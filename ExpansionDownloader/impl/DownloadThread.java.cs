using System;
using System.IO;
using System.Net;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Java.IO;
using Java.Lang;
using Java.Net;
using LicenseVerificationLibrary;
using Exception = Java.Lang.Exception;
using File = Java.IO.File;
using FileNotFoundException = Java.IO.FileNotFoundException;
using IOException = Java.IO.IOException;
using Process = Android.OS.Process;

namespace ExpansionDownloader.impl
{
    public class DownloadThread
    {
        private readonly Context mContext;
        private readonly DownloadsDB mDB;
        private readonly DownloadInfo mInfo;
        private readonly DownloadNotification mNotification;
        private readonly DownloaderService mService;

        public DownloadThread(DownloadInfo info, DownloaderService service, DownloadNotification notification)
        {
            mContext = service;
            mInfo = info;
            mService = service;
            mNotification = notification;
            mDB = DownloadsDB.getDB(service);
        }

        /**
     * Returns the default user agent
     */

        private string userAgent()
        {
            return Constants.DEFAULT_USER_AGENT;
        }

        /**
     * State for the entire run() method.
     */

        private static bool isLocalHost(string url)
        {
            if (url == null)
            {
                return false;
            }

            try
            {
                string host = new Uri(url).Host;
                if (!string.IsNullOrEmpty(host))
                {
                    // TODO: InetAddress.isLoopbackAddress should be used to check
                    // for localhost. However no public factory methods exist which
                    // can be used without triggering DNS lookup if host is not localhost.
                    if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                        host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                        host.Equals("[::1]", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (IllegalArgumentException iex)
            {
                // Ignore (URI.create)
            }

            return false;
        }

        /**
     * Executes the download in a separate thread
     */

        public void run()
        {
            Process.SetThreadPriority(ThreadPriority.Background);

            var state = new State(mInfo, mService);
            PowerManager.WakeLock wakeLock = null;
            int finalStatus = DownloaderService.STATUS_UNKNOWN_ERROR;

            try
            {
                var pm = mContext.GetSystemService(Context.PowerService).JavaCast<PowerManager>();
                wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, Constants.TAG);
                wakeLock.Acquire();

                if (Constants.LOGV)
                {
                    Log.Verbose(Constants.TAG, "initiating download for " + mInfo.mFileName);
                    Log.Verbose(Constants.TAG, "  at " + mInfo.mUri);
                }

                bool finished = false;
                while (!finished)
                {
                    if (Constants.LOGV)
                    {
                        Log.Verbose(Constants.TAG, "initiating download for " + mInfo.mFileName);
                        Log.Verbose(Constants.TAG, "  at " + mInfo.mUri);
                    }

                    var request = (HttpWebRequest) WebRequest.Create(new Uri(state.mRequestUri));
                    request.Proxy = WebRequest.DefaultWebProxy;
                    request.UserAgent = userAgent();
                    request.Timeout = TimeSpan.FromMinutes(1).Milliseconds;
                    request.ReadWriteTimeout = TimeSpan.FromMinutes(1).Milliseconds;
                    request.AllowAutoRedirect = false; // todo

                    try
                    {
                        executeDownload(state, request);
                        finished = true;
                    }
                    catch (RetryDownload ex)
                    {
                        // fall through
                    }
                    finally
                    {
                        request.Abort();
                    }
                }

                if (Constants.LOGV)
                {
                    Log.Verbose(Constants.TAG, "download completed for " + mInfo.mFileName);
                    Log.Verbose(Constants.TAG, "  at " + mInfo.mUri);
                }
                finalizeDestinationFile(state);
                finalStatus = DownloaderService.STATUS_SUCCESS;
            }
            catch (StopRequest error)
            {
                // remove the cause before printing, in case it contains PII
                Log.Warn(Constants.TAG, "Aborting request for download " + mInfo.mFileName + ": " + error.Message);
                error.PrintStackTrace();
                finalStatus = error.mFinalStatus;
                // fall through to finally block
            }
            catch (Throwable ex)
            {
                //sometimes the socket code throws unchecked exceptions
                Log.Warn(Constants.TAG, "Exception for " + mInfo.mFileName + ": " + ex);
                finalStatus = DownloaderService.STATUS_UNKNOWN_ERROR;
                // falls through to the code that reports an error
            }
            finally
            {
                if (wakeLock != null)
                {
                    wakeLock.Release();
                }
                cleanupDestination(state, finalStatus);
                notifyDownloadCompleted(finalStatus, state.mCountRetry, state.mRetryAfter, state.mRedirectCount, state.mGotData, state.mFilename);
            }
        }

        /**
     * Fully execute a single download request - setup and send the request, handle the response,
     * and transfer the data to the destination file.
     */

        private void executeDownload(State state, HttpWebRequest request)
        {
            var innerState = new InnerState();
            var data = new byte[Constants.BUFFER_SIZE];

            checkPausedOrCanceled(state);

            setupDestinationFile(state, innerState);
            addRequestHeaders(innerState, request);

            // check just before sending the request to avoid using an invalid connection at all
            checkConnectivity(state);

            mNotification.onDownloadStateChanged(DownloaderClientState.STATE_CONNECTING);
            HttpWebResponse response = sendRequest(state, request);
            handleExceptionalStatus(state, innerState, response);

            if (Constants.LOGV)
            {
                Log.Verbose(Constants.TAG, "received response for " + mInfo.mUri);
            }

            processResponseHeaders(state, innerState, response);
            Stream entityStream = openResponseEntity(state, response);
            mNotification.onDownloadStateChanged(DownloaderClientState.STATE_DOWNLOADING);
            transferData(state, innerState, data, entityStream);
        }

        /**
     * Check if current connectivity is valid for this request.
     */

        private void checkConnectivity(State state)
        {
            switch (mService.getNetworkAvailabilityState(mDB))
            {
                case DownloaderService.NETWORK_OK:
                    return;
                case DownloaderService.NETWORK_NO_CONNECTION:
                    throw new StopRequest(DownloaderService.STATUS_WAITING_FOR_NETWORK, "waiting for network to return");
                case DownloaderService.NETWORK_TYPE_DISALLOWED_BY_REQUESTOR:
                    throw new StopRequest(DownloaderService.STATUS_QUEUED_FOR_WIFI, "waiting for wifi or for download over cellular to be authorized");
                case DownloaderService.NETWORK_CANNOT_USE_ROAMING:
                    throw new StopRequest(DownloaderService.STATUS_WAITING_FOR_NETWORK, "roaming is not allowed");
            }
        }

        /**
     * Transfer as much data as possible from the HTTP response to the destination file.
     * @param data buffer to use to read data
     * @param entityStream stream for reading the HTTP response entity
     */

        private void transferData(State state, InnerState innerState, byte[] data, Stream entityStream)
        {
            for (;;)
            {
                int bytesRead = readFromResponse(state, innerState, data, entityStream);
                if (bytesRead == -1)
                {
                    // success, end of stream already reached
                    handleEndOfStream(state, innerState);
                    return;
                }

                state.mGotData = true;
                writeDataToDestination(state, data, bytesRead);
                innerState.mBytesSoFar += bytesRead;
                innerState.mBytesThisSession += bytesRead;
                reportProgress(state, innerState);

                checkPausedOrCanceled(state);
            }
        }

        /**
     * Called after a successful completion to take any necessary action on the downloaded file.
     */

        private void finalizeDestinationFile(State state)
        {
            syncDestination(state);
            string tempFilename = state.mFilename;
            string finalFilename = Helpers.generateSaveFileName(mService, mInfo.mFileName);
            if (state.mFilename != (finalFilename))
            {
                var startFile = new File(tempFilename);
                var destFile = new File(finalFilename);
                if (mInfo.mTotalBytes != -1 && mInfo.mCurrentBytes == mInfo.mTotalBytes)
                {
                    if (!startFile.RenameTo(destFile))
                    {
                        throw new StopRequest(DownloaderService.STATUS_FILE_ERROR, "unable to finalize destination file");
                    }
                }
                else
                {
                    throw new StopRequest(DownloaderService.STATUS_FILE_DELIVERED_INCORRECTLY,
                                          "file delivered with incorrect size. probably due to network not browser configured");
                }
            }
        }

        /**
     * Called just before the thread finishes, regardless of status, to take any necessary action on
     * the downloaded file.
     */

        private void cleanupDestination(State state, int finalStatus)
        {
            closeDestination(state);
            if (state.mFilename != null && DownloaderService.isStatusError(finalStatus))
            {
                new File(state.mFilename).Delete();
                state.mFilename = null;
            }
        }

        /**
     * Sync the destination file to storage.
     */

        private void syncDestination(State state)
        {
            FileOutputStream downloadedFileStream = null;
            try
            {
                downloadedFileStream = new FileOutputStream(state.mFilename, true);
                downloadedFileStream.FD.Sync();
            }
            catch (FileNotFoundException ex)
            {
                Log.Warn(Constants.TAG, "file " + state.mFilename + " not found: " + ex);
            }
            catch (SyncFailedException ex)
            {
                Log.Warn(Constants.TAG, "file " + state.mFilename + " sync failed: " + ex);
            }
            catch (IOException ex)
            {
                Log.Warn(Constants.TAG, "IOException trying to sync " + state.mFilename + ": " + ex);
            }
            catch (RuntimeException ex)
            {
                Log.Warn(Constants.TAG, "exception while syncing file: ", ex);
            }
            finally
            {
                if (downloadedFileStream != null)
                {
                    try
                    {
                        downloadedFileStream.Close();
                    }
                    catch (IOException ex)
                    {
                        Log.Warn(Constants.TAG, "IOException while closing synced file: ", ex);
                    }
                    catch (RuntimeException ex)
                    {
                        Log.Warn(Constants.TAG, "exception while closing file: ", ex);
                    }
                }
            }
        }

        /**
     * Close the destination output stream.
     */

        private void closeDestination(State state)
        {
            try
            {
                // close the file
                if (state.mStream != null)
                {
                    state.mStream.Close();
                    state.mStream = null;
                }
            }
            catch (IOException ex)
            {
                if (Constants.LOGV)
                {
                    Log.Verbose(Constants.TAG, "exception when closing the file after download : " + ex);
                }
                // nothing can really be done if the file can't be closed
            }
        }

        /**
     * Check if the download has been paused or canceled, stopping the request appropriately if it
     * has been.
     */

        private void checkPausedOrCanceled(State state)
        {
            if (mService.getControl() == DownloaderService.CONTROL_PAUSED)
            {
                int status = mService.getStatus();
                switch (status)
                {
                    case DownloaderService.STATUS_PAUSED_BY_APP:
                        throw new StopRequest(mService.getStatus(), "download paused");
                }
            }
        }

        /**
     * Report download progress through the database if necessary.
     */

        private void reportProgress(State state, InnerState innerState)
        {
            long now = PolicyExtensions.GetCurrentMilliseconds();
            if (innerState.mBytesSoFar - innerState.mBytesNotified > Constants.MIN_PROGRESS_STEP &&
                now - innerState.mTimeLastNotification > Constants.MIN_PROGRESS_TIME)
            {
                // we store progress updates to the database here
                mInfo.mCurrentBytes = innerState.mBytesSoFar;
                mDB.updateDownloadCurrentBytes(mInfo);

                innerState.mBytesNotified = innerState.mBytesSoFar;
                innerState.mTimeLastNotification = now;

                long totalBytesSoFar = innerState.mBytesThisSession + mService.mBytesSoFar;

                if (Constants.LOGVV)
                {
                    Log.Verbose(Constants.TAG, "downloaded " + mInfo.mCurrentBytes + " out of " + mInfo.mTotalBytes);
                    Log.Verbose(Constants.TAG, "     total " + totalBytesSoFar + " out of " + mService.mTotalLength);
                }

                mService.notifyUpdateBytes(totalBytesSoFar);
            }
        }

        /**
     * Write a data buffer to the destination file.
     * @param data buffer containing the data to write
     * @param bytesRead how many bytes to write from the buffer
     */

        private void writeDataToDestination(State state, byte[] data, int bytesRead)
        {
            bool finished = false;

            while (!finished)
            {
                try
                {
                    if (state.mStream == null)
                    {
                        state.mStream = new FileOutputStream(state.mFilename, true);
                    }

                    state.mStream.Write(data, 0, bytesRead);

                    // we close after every write - todo: this may be too inefficient
                    closeDestination(state);

                    finished = true;
                }
                catch (IOException ex)
                {
                    if (!Helpers.isExternalMediaMounted())
                    {
                        throw new StopRequest(DownloaderService.STATUS_DEVICE_NOT_FOUND_ERROR, "external media not mounted while writing destination file");
                    }

                    long availableBytes = Helpers.getAvailableBytes(Helpers.getFilesystemRoot(state.mFilename));

                    if (availableBytes < bytesRead)
                    {
                        throw new StopRequest(DownloaderService.STATUS_INSUFFICIENT_SPACE_ERROR, "insufficient space while writing destination file", ex);
                    }

                    throw new StopRequest(DownloaderService.STATUS_FILE_ERROR, "while writing destination file: " + ex, ex);
                }
            }
        }

        /**
     * Called when we've reached the end of the HTTP response stream, to update the database and
     * check for consistency.
     */

        private void handleEndOfStream(State state, InnerState innerState)
        {
            mInfo.mCurrentBytes = innerState.mBytesSoFar;
            // this should always be set from the market
            //    	if ( innerState.mHeaderContentLength == null ) {
            //    		mInfo.mTotalBytes = innerState.mBytesSoFar;
            //    	}
            mDB.updateDownload(mInfo);

            bool lengthMismatched = innerState.mHeaderContentLength != null &&
                                    innerState.mBytesSoFar != int.Parse(innerState.mHeaderContentLength);
            if (lengthMismatched)
            {
                if (cannotResume(innerState))
                {
                    throw new StopRequest(DownloaderService.STATUS_CANNOT_RESUME, "mismatched content length");
                }
                else
                {
                    throw new StopRequest(getFinalStatusForHttpError(state), "closed socket before end of file");
                }
            }
        }

        private static bool cannotResume(InnerState innerState)
        {
            return innerState.mBytesSoFar > 0 && innerState.mHeaderETag == null;
        }

        /**
     * Read some data from the HTTP response stream, handling I/O errors.
     * @param data buffer to use to read data
     * @param entityStream stream for reading the HTTP response entity
     * @return the number of bytes actually read or -1 if the end of the stream has been reached
     */

        private int readFromResponse(State state, InnerState innerState, byte[] data, Stream entityStream)
        {
            try
            {
                return entityStream.Read(data, 0, data.Length);
            }
            catch (IOException ex)
            {
                logNetworkState();
                mInfo.mCurrentBytes = innerState.mBytesSoFar;
                mDB.updateDownload(mInfo);
                if (cannotResume(innerState))
                {
                    throw new StopRequest(DownloaderService.STATUS_CANNOT_RESUME,
                                          "while reading response: " + ex + ", can't resume interrupted download with no ETag", ex);
                }
                else
                {
                    throw new StopRequest(getFinalStatusForHttpError(state), "while reading response: " + ex, ex);
                }
            }
        }

        /**
     * Open a stream for the HTTP response entity, handling I/O errors.
     * @return an InputStream to read the response entity
     */

        private Stream openResponseEntity(State state, HttpWebResponse response)
        {
            try
            {
                return response.GetResponseStream();
            }
            catch (IOException ex)
            {
                logNetworkState();
                throw new StopRequest(getFinalStatusForHttpError(state), "while getting entity: " + ex, ex);
            }
        }

        private void logNetworkState()
        {
            if (Constants.LOGX)
            {
                Log.Info(Constants.TAG, "Net " + (mService.getNetworkAvailabilityState(mDB) == DownloaderService.NETWORK_OK ? "Up" : "Down"));
            }
        }

        /**
     * Read HTTP response headers and take appropriate action, including setting up the destination
     * file and updating the database.
     */

        private void processResponseHeaders(State state, InnerState innerState, HttpWebResponse response)
        {
            if (innerState.mContinuingDownload)
            {
                // ignore response headers on resume requests
                return;
            }

            readResponseHeaders(state, innerState, response);

            try
            {
                state.mFilename = mService.generateSaveFile(mInfo.mFileName, mInfo.mTotalBytes);
            }
            catch (DownloaderService.GenerateSaveFileError exc)
            {
                throw new StopRequest(exc.mStatus, exc.Message);
            }
            try
            {
                state.mStream = new FileOutputStream(state.mFilename);
            }
            catch (FileNotFoundException exc)
            {
                // make sure the directory exists
                var pathFile = new File(Helpers.getSaveFilePath(mService));
                try
                {
                    if (pathFile.Mkdirs())
                    {
                        state.mStream = new FileOutputStream(state.mFilename);
                    }
                }
                catch (Exception ex)
                {
                    throw new StopRequest(DownloaderService.STATUS_FILE_ERROR, "while opening destination file: " + exc, exc);
                }
            }
            if (Constants.LOGV)
            {
                Log.Verbose(Constants.TAG, "writing " + mInfo.mUri + " to " + state.mFilename);
            }

            updateDatabaseFromHeaders(state, innerState);
            // check connectivity again now that we know the total size
            checkConnectivity(state);
        }

        /**
     * Update necessary database fields based on values of HTTP response headers that have been
     * read.
     */

        private void updateDatabaseFromHeaders(State state, InnerState innerState)
        {
            mInfo.mETag = innerState.mHeaderETag;
            mDB.updateDownload(mInfo);
        }

        /**
     * Read headers from the HTTP response and store them into local state.
     */

        private void readResponseHeaders(State state, InnerState innerState, HttpWebResponse response)
        {
            string header = response.GetResponseHeader("Content-Disposition");
            if (header != null)
            {
                innerState.mHeaderContentDisposition = header;
            }
            header = response.GetResponseHeader("Content-Location");
            if (header != null)
            {
                innerState.mHeaderContentLocation = header;
            }
            header = response.GetResponseHeader("ETag");
            if (header != null)
            {
                innerState.mHeaderETag = header;
            }
            string headerTransferEncoding = response.GetResponseHeader("Transfer-Encoding");
            header = response.GetResponseHeader("Content-Type");
            if (header != null && header != "application/vnd.android.obb")
            {
                throw new StopRequest(DownloaderService.STATUS_FILE_DELIVERED_INCORRECTLY, "file delivered with incorrect Mime type");
            }

            if (!string.IsNullOrEmpty(headerTransferEncoding))
            {
                header = response.GetResponseHeader("Content-Length");
                if (header != null)
                {
                    innerState.mHeaderContentLength = header;
                    // this is always set from Market
                    long contentLength = long.Parse(innerState.mHeaderContentLength);
                    if (contentLength != -1 && contentLength != mInfo.mTotalBytes)
                    {
                        // we're most likely on a bad wifi connection -- we should probably
                        // also look at the mime type --- but the size mismatch is enough
                        // to tell us that something is wrong here
                        Log.Error(Constants.TAG, "Incorrect file size delivered.");
                    }
                }
            }
            else
            {
                // Ignore content-length with transfer-encoding - 2616 4.4 3
                if (Constants.LOGVV)
                {
                    Log.Verbose(Constants.TAG, "ignoring content-length because of xfer-encoding");
                }
            }

            if (Constants.LOGVV)
            {
                Log.Verbose(Constants.TAG, "Content-Disposition: " + innerState.mHeaderContentDisposition);
                Log.Verbose(Constants.TAG, "Content-Length: " + innerState.mHeaderContentLength);
                Log.Verbose(Constants.TAG, "Content-Location: " + innerState.mHeaderContentLocation);
                Log.Verbose(Constants.TAG, "ETag: " + innerState.mHeaderETag);
                Log.Verbose(Constants.TAG, "Transfer-Encoding: " + headerTransferEncoding);
            }

            bool noSizeInfo = innerState.mHeaderContentLength == null &&
                              (headerTransferEncoding == null || !"chunked".Equals(headerTransferEncoding, StringComparison.OrdinalIgnoreCase));
            if (noSizeInfo)
            {
                throw new StopRequest(DownloaderService.STATUS_HTTP_DATA_ERROR, "can't know size of download, giving up");
            }
        }

        /**
     * Check the HTTP response status and handle anything unusual (e.g. not 200/206).
     */

        private void handleExceptionalStatus(State state, InnerState innerState, HttpWebResponse response)
        {
            HttpStatusCode statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.ServiceUnavailable &&
                mInfo.mNumFailed < Constants.MAX_RETRIES)
            {
                handleServiceUnavailable(state, response);
            }
            if (statusCode == HttpStatusCode.Moved ||
                statusCode == HttpStatusCode.Found ||
                statusCode == HttpStatusCode.SeeOther ||
                statusCode == HttpStatusCode.TemporaryRedirect)
            {
                handleRedirect(state, response, statusCode);
            }

            HttpStatusCode expectedStatus = innerState.mContinuingDownload
                                                ? HttpStatusCode.PartialContent
                                                : HttpStatusCode.OK;
            if (statusCode != expectedStatus)
            {
                handleOtherStatus(state, innerState, statusCode);
            }
            else
            {
                // no longer redirected
                state.mRedirectCount = 0;
            }
        }

        /**
     * Handle a status that we don't know how to deal with properly.
     */

        private void handleOtherStatus(State state, InnerState innerState, HttpStatusCode statusCode)
        {
            int finalStatus;
            if (DownloaderService.isStatusError((int) statusCode))
            {
                finalStatus = (int) statusCode;
            }
            else if ((int) statusCode >= 300 && (int) statusCode < 400)
            {
                finalStatus = DownloaderService.STATUS_UNHANDLED_REDIRECT;
            }
            else if (innerState.mContinuingDownload && (int) statusCode == DownloaderService.STATUS_SUCCESS)
            {
                finalStatus = DownloaderService.STATUS_CANNOT_RESUME;
            }
            else
            {
                finalStatus = DownloaderService.STATUS_UNHANDLED_HTTP_CODE;
            }
            throw new StopRequest(finalStatus, "http error " + statusCode);
        }

        /**
     * Handle a 3xx redirect status.
     */

        private void handleRedirect(State state, HttpWebResponse response, HttpStatusCode statusCode)
        {
            if (Constants.LOGVV)
            {
                Log.Verbose(Constants.TAG, "got HTTP redirect " + statusCode);
            }
            if (state.mRedirectCount >= Constants.MAX_REDIRECTS)
            {
                throw new StopRequest(DownloaderService.STATUS_TOO_MANY_REDIRECTS, "too many redirects");
            }
            string header = response.GetResponseHeader("Location");
            if (header == null)
            {
                return;
            }
            if (Constants.LOGVV)
            {
                Log.Verbose(Constants.TAG, "Location :" + header);
            }

            string newUri;
            try
            {
                newUri = new URI(mInfo.mUri).Resolve(new URI(header)).ToString();
            }
            catch (URISyntaxException ex)
            {
                if (Constants.LOGV)
                {
                    Log.Debug(Constants.TAG, "Couldn't resolve redirect URI " + header + " for " + mInfo.mUri);
                }
                throw new StopRequest(DownloaderService.STATUS_HTTP_DATA_ERROR, "Couldn't resolve redirect URI");
            }
            ++state.mRedirectCount;
            state.mRequestUri = newUri;
            if ((int) statusCode == 301 || (int) statusCode == 303)
            {
                // use the new URI for all future requests (should a retry/resume be necessary)
                state.mNewUri = newUri;
            }
            throw new RetryDownload();
        }

        /// <summary>
        ///   Add headers for this download to the HTTP request to allow for resume.
        /// </summary>
        private void addRequestHeaders(InnerState innerState, HttpWebRequest request)
        {
            if (innerState.mContinuingDownload)
            {
                if (innerState.mHeaderETag != null)
                {
                    request.Headers.Add("If-Match", innerState.mHeaderETag);
                }
                request.Headers.Add("Range", "bytes=" + innerState.mBytesSoFar + "-");
            }
        }

        /**
     * Handle a 503 Service Unavailable status by processing the Retry-After header.
     */

        private void handleServiceUnavailable(State state, HttpWebResponse response)
        {
            if (Constants.LOGVV)
            {
                Log.Verbose(Constants.TAG, "got HTTP response code 503");
            }
            state.mCountRetry = true;
            string header = response.GetResponseHeader("Retry-After");
            if (header != null)
            {
                try
                {
                    if (Constants.LOGVV)
                    {
                        Log.Verbose(Constants.TAG, "Retry-After :" + header);
                    }
                    state.mRetryAfter = int.Parse(header);
                    if (state.mRetryAfter < 0)
                    {
                        state.mRetryAfter = 0;
                    }
                    else
                    {
                        if (state.mRetryAfter < Constants.MIN_RETRY_AFTER)
                        {
                            state.mRetryAfter = Constants.MIN_RETRY_AFTER;
                        }
                        else if (state.mRetryAfter > Constants.MAX_RETRY_AFTER)
                        {
                            state.mRetryAfter = Constants.MAX_RETRY_AFTER;
                        }
                        state.mRetryAfter += Helpers.sRandom.NextInt(Constants.MIN_RETRY_AFTER + 1);
                        state.mRetryAfter *= 1000;
                    }
                }
                catch (NumberFormatException ex)
                {
                    // ignored - retryAfter stays 0 in this case.
                }
            }
            throw new StopRequest(DownloaderService.STATUS_WAITING_TO_RETRY, "got 503 Service Unavailable, will retry later");
        }

        /// <summary>
        ///   Send the request to the server, handling any I/O exceptions.
        /// </summary>
        private HttpWebResponse sendRequest(State state, HttpWebRequest request)
        {
            try
            {
                return (HttpWebResponse) request.GetResponse();
            }
            catch (IllegalArgumentException ex)
            {
                throw new StopRequest(DownloaderService.STATUS_HTTP_DATA_ERROR, "while trying to execute request: " + ex, ex);
            }
            catch (IOException ex)
            {
                logNetworkState();
                throw new StopRequest(getFinalStatusForHttpError(state), "while trying to execute request: " + ex, ex);
            }
        }

        private int getFinalStatusForHttpError(State state)
        {
            if (mService.getNetworkAvailabilityState(mDB) != DownloaderService.NETWORK_OK)
            {
                return DownloaderService.STATUS_WAITING_FOR_NETWORK;
            }
            else if (mInfo.mNumFailed < Constants.MAX_RETRIES)
            {
                state.mCountRetry = true;
                return DownloaderService.STATUS_WAITING_TO_RETRY;
            }
            else
            {
                Log.Warn(Constants.TAG, "reached max retries for " + mInfo.mNumFailed);
                return DownloaderService.STATUS_HTTP_DATA_ERROR;
            }
        }

        /**
     * Prepare the destination file to receive data.  If the file already exists, we'll set up
     * appropriately for resumption.
     */

        private void setupDestinationFile(State state, InnerState innerState)
        {
            if (state.mFilename != null)
            {
                // only true if we've already run a thread for this download
                if (!Helpers.isFilenameValid(state.mFilename))
                {
                    // this should never happen
                    throw new StopRequest(DownloaderService.STATUS_FILE_ERROR, "found invalid internal destination filename");
                }
                // We're resuming a download that got interrupted
                var f = new File(state.mFilename);
                if (f.Exists())
                {
                    long fileLength = f.Length();
                    if (fileLength == 0)
                    {
                        // The download hadn't actually started, we can restart from scratch
                        f.Delete();
                        state.mFilename = null;
                    }
                    else if (mInfo.mETag == null)
                    {
                        // This should've been caught upon failure
                        f.Delete();
                        throw new StopRequest(DownloaderService.STATUS_CANNOT_RESUME, "Trying to resume a download that can't be resumed");
                    }
                    else
                    {
                        // All right, we'll be able to resume this download
                        try
                        {
                            state.mStream = new FileOutputStream(state.mFilename, true);
                        }
                        catch (FileNotFoundException exc)
                        {
                            throw new StopRequest(DownloaderService.STATUS_FILE_ERROR, "while opening destination for resuming: " + exc, exc);
                        }
                        innerState.mBytesSoFar = (int) fileLength;
                        if (mInfo.mTotalBytes != -1)
                        {
                            innerState.mHeaderContentLength = mInfo.mTotalBytes.ToString();
                        }
                        innerState.mHeaderETag = mInfo.mETag;
                        innerState.mContinuingDownload = true;
                    }
                }
            }

            if (state.mStream != null)
            {
                closeDestination(state);
            }
        }

        /**
     * Stores information about the completed download, and notifies the initiating application.
     */

        private void notifyDownloadCompleted(int status, bool countRetry, int retryAfter, int redirectCount, bool gotData, string filename)
        {
            updateDownloadDatabase(status, countRetry, retryAfter, redirectCount, gotData, filename);
            if (DownloaderService.isStatusCompleted(status))
            {
                // TBD: send status update?
            }
        }

        private void updateDownloadDatabase(int status, bool countRetry, int retryAfter, int redirectCount, bool gotData, string filename)
        {
            mInfo.mStatus = status;
            mInfo.mRetryAfter = retryAfter;
            mInfo.mRedirectCount = redirectCount;
            mInfo.mLastMod = PolicyExtensions.GetCurrentMilliseconds();
            if (!countRetry)
            {
                mInfo.mNumFailed = 0;
            }
            else if (gotData)
            {
                mInfo.mNumFailed = 1;
            }
            else
            {
                mInfo.mNumFailed++;
            }
            mDB.updateDownload(mInfo);
        }

        #region Nested type: InnerState

        private class InnerState
        {
            public int mBytesNotified;
            public int mBytesSoFar;
            public int mBytesThisSession;
            public bool mContinuingDownload;
            public string mHeaderContentDisposition;
            public string mHeaderContentLength;
            public string mHeaderContentLocation;
            public string mHeaderETag;
            public long mTimeLastNotification;
        }

        #endregion

        #region Nested type: RetryDownload

        private class RetryDownload : Throwable
        {
            private static long serialVersionUID = 6196036036517540229L;
        }

        #endregion

        #region Nested type: State

        private class State
        {
            public bool mCountRetry;
            public string mFilename;
            public bool mGotData;
            public string mNewUri;
            public int mRedirectCount;
            public string mRequestUri;
            public int mRetryAfter;
            public FileOutputStream mStream;

            public State(DownloadInfo info, DownloaderService service)
            {
                mRedirectCount = info.mRedirectCount;
                mRequestUri = info.mUri;
                mFilename = service.generateTempSaveFileName(info.mFileName);
            }
        }

        #endregion

        #region Nested type: StopRequest

        private class StopRequest : Throwable
        {
            private static long serialVersionUID = 6338592678988347973L;
            public readonly int mFinalStatus;

            public StopRequest(int finalStatus, string message)
                : base(message)
            {
                mFinalStatus = finalStatus;
            }

            public StopRequest(int finalStatus, string message, Throwable throwable)
                : base(message, throwable)
            {
                mFinalStatus = finalStatus;
            }
        }

        #endregion
    }
}