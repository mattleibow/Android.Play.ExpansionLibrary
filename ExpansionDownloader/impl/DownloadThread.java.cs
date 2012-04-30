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
using Exception = System.Exception;
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

        /// <summary>
        /// Returns the default user agent
        /// </summary>
        private static string UserAgent()
        {
            return DownloaderService.DefaultUserAgent;
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
            catch (IllegalArgumentException)
            {
                // Ignore (URI.create)
            }

            return false;
        }

        /// <summary>
        /// Executes the download in a separate thread
        /// </summary>
        public void Run()
        {
            Process.SetThreadPriority(ThreadPriority.Background);

            var state = new State(mInfo, mService);
            PowerManager.WakeLock wakeLock = null;
            int finalStatus = DownloadStatus.UnknownError;

            try
            {
                var pm = mContext.GetSystemService(Context.PowerService).JavaCast<PowerManager>();
                wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, DownloaderService.TAG);
                wakeLock.Acquire();

                System.Diagnostics.Debug.WriteLine("DownloadThread : initiating download for " + mInfo.FileName);
                System.Diagnostics.Debug.WriteLine("DownloadThread :   at " + mInfo.Uri);

                bool finished = false;
                while (!finished)
                {
                    System.Diagnostics.Debug.WriteLine("DownloadThread : initiating download for " + mInfo.FileName);
                    System.Diagnostics.Debug.WriteLine("DownloadThread :   at " + mInfo.Uri);

                    var requestUri = new Uri(state.mRequestUri);
                    var minute = (int) TimeSpan.FromMinutes(1).TotalMilliseconds;
                    var request = new HttpWebRequest(requestUri)
                                      {
                                          Proxy = WebRequest.DefaultWebProxy,
                                          UserAgent = UserAgent(),
                                          Timeout = minute,
                                          ReadWriteTimeout = minute,
                                          AllowAutoRedirect = false // todo
                                      };

                    try
                    {
                        ExecuteDownload(state, request);
                        finished = true;
                    }
                    catch (RetryDownloadException)
                    {
                        // fall through
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        throw;
                    }
                    finally
                    {
                        request.Abort();
                    }
                }

                System.Diagnostics.Debug.WriteLine("DownloadThread : download completed for " + mInfo.FileName);
                System.Diagnostics.Debug.WriteLine("DownloadThread :   at " + mInfo.Uri);

                FinalizeDestinationFile(state);
                finalStatus = DownloadStatus.Success;
            }
            catch (StopRequestException error)
            {
                // remove the cause before printing, in case it contains PII
                Log.Warn(DownloaderService.TAG, "Aborting request for download " + mInfo.FileName + ": " + error.Message);
                System.Diagnostics.Debug.WriteLine(error.StackTrace);
                finalStatus = error.mFinalStatus;
                // fall through to finally block
            }
            catch (Exception ex)
            {
                //sometimes the socket code throws unchecked exceptions
                Log.Warn(DownloaderService.TAG, "Exception for " + mInfo.FileName + ": " + ex);
                finalStatus = DownloadStatus.UnknownError;
                // falls through to the code that reports an error
            }
            finally
            {
                if (wakeLock != null)
                {
                    wakeLock.Release();
                }
                CleanupDestination(state, finalStatus);
                NotifyDownloadCompleted(finalStatus, state.mCountRetry, state.mRetryAfter, state.mRedirectCount, state.mGotData, state.mFilename);
            }
        }

        /// <summary>
        /// Fully execute a single download request - setup and send the request,
        /// handle the response, and transfer the data to the destination file.
        /// </summary>
        private void ExecuteDownload(State state, HttpWebRequest request)
        {
            var innerState = new InnerState();
            var data = new byte[DownloaderService.BufferSize];

            CheckPausedOrCanceled(state);

            SetupDestinationFile(state, innerState);
            AddRequestHeaders(innerState, request);

            // check just before sending the request to avoid using an invalid connection at all
            CheckConnectivity(state);

            mNotification.OnDownloadStateChanged(DownloaderClientState.Connecting);
            HttpWebResponse response = SendRequest(state, request);
            HandleExceptionalStatus(state, innerState, response);

            System.Diagnostics.Debug.WriteLine("DownloadThread : received response for {0}", mInfo.Uri);

            ProcessResponseHeaders(state, innerState, response);
            Stream entityStream = OpenResponseEntity(state, response);
            mNotification.OnDownloadStateChanged(DownloaderClientState.Downloading);
            TransferData(state, innerState, data, entityStream);
        }

        /// <summary>
        /// Check if current connectivity is valid for this request.
        /// </summary>
        private void CheckConnectivity(State state)
        {
            switch (mService.GetNetworkAvailabilityState(mDB))
            {
                case NetworkConstants.NETWORK_OK:
                    return;
                case NetworkConstants.NETWORK_NO_CONNECTION:
                    throw new StopRequestException(DownloadStatus.WaitingForNetwork, "waiting for network to return");
                case NetworkConstants.NETWORK_TYPE_DISALLOWED_BY_REQUESTOR:
                    throw new StopRequestException(DownloadStatus.QueuedForWifi, "waiting for wifi or for download over cellular to be authorized");
                case NetworkConstants.NETWORK_CANNOT_USE_ROAMING:
                    throw new StopRequestException(DownloadStatus.WaitingForNetwork, "roaming is not allowed");
            }
        }

        /// <summary>
        /// Transfer as much data as possible from the HTTP response to the destination file.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="innerState"></param>
        /// <param name="data">buffer to use to read data</param>
        /// <param name="entityStream">stream for reading the HTTP response entity</param>
        private void TransferData(State state, InnerState innerState, byte[] data, Stream entityStream)
        {
            int bytesRead;

            do
            {
                bytesRead = ReadFromResponse(state, innerState, data, entityStream);

                if (bytesRead != 0)
                {
                    state.mGotData = true;
                    WriteDataToDestination(state, data, bytesRead);
                    innerState.mBytesSoFar += bytesRead;
                    innerState.mBytesThisSession += bytesRead;
                    ReportProgress(state, innerState);

                    CheckPausedOrCanceled(state);
                }
            } while (bytesRead != 0);

            // success, end of stream already reached
            HandleEndOfStream(state, innerState);
        }

        /// <summary>
        /// Called after a successful completion to take any necessary action on the downloaded file.
        /// </summary>
        private void FinalizeDestinationFile(State state)
        {
            SyncDestination(state);
            string tempFilename = state.mFilename;
            string finalFilename = Helpers.GenerateSaveFileName(mService, mInfo.FileName);
            if (state.mFilename != (finalFilename))
            {
                var startFile = new File(tempFilename);
                var destFile = new File(finalFilename);
                if (mInfo.TotalBytes != -1 && mInfo.CurrentBytes == mInfo.TotalBytes)
                {
                    if (!startFile.RenameTo(destFile))
                    {
                        throw new StopRequestException(DownloadStatus.FileError,
                                                       "unable to finalize destination file");
                    }
                }
                else
                {
                    throw new StopRequestException(DownloadStatus.FileDeliveredIncorrectly,
                                                   "file delivered with incorrect size. probably due to network not browser configured");
                }
            }
        }

        /// <summary>
        /// Called just before the thread finishes, regardless of status, to take any
        /// necessary action on the downloaded file.
        /// </summary>
        private static void CleanupDestination(State state, int finalStatus)
        {
            CloseDestination(state);
            if (state.mFilename != null &&
                DownloaderService.isStatusError(finalStatus) &&
                System.IO.File.Exists(state.mFilename))
            {
                System.IO.File.Delete(state.mFilename);
                state.mFilename = null;
            }
        }

        /// <summary>
        /// Sync the destination file to storage.
        /// </summary>
        private static void SyncDestination(State state)
        {
            FileOutputStream downloadedFileStream = null;
            try
            {
                downloadedFileStream = new FileOutputStream(state.mFilename, true);
                downloadedFileStream.FD.Sync();
            }
            catch (FileNotFoundException ex)
            {
                Log.Warn(DownloaderService.TAG, "file " + state.mFilename + " not found: " + ex);
            }
            catch (SyncFailedException ex)
            {
                Log.Warn(DownloaderService.TAG, "file " + state.mFilename + " sync failed: " + ex);
            }
            catch (IOException ex)
            {
                Log.Warn(DownloaderService.TAG, "IOException trying to sync " + state.mFilename + ": " + ex);
            }
            catch (RuntimeException ex)
            {
                Log.Warn(DownloaderService.TAG, "exception while syncing file: ", ex);
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
                        Log.Warn(DownloaderService.TAG, "IOException while closing synced file: ", ex);
                    }
                    catch (RuntimeException ex)
                    {
                        Log.Warn(DownloaderService.TAG, "exception while closing file: ", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Close the destination output stream.
        /// </summary>
        private static void CloseDestination(State state)
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DownloadThread : exception when closing the file after download : " + ex);
                // nothing can really be done if the file can't be closed
            }
        }

        /// <summary>
        /// Check if the download has been paused or canceled, stopping the request appropriately if it has been.
        /// </summary>
        private void CheckPausedOrCanceled(State state)
        {
            if (mService.getControl() == DownloaderService.CONTROL_PAUSED)
            {
                int status = mService.getStatus();
                if (status == DownloadStatus.PausedByApp)
                {
                    throw new StopRequestException(status, "download paused");
                }
            }
        }

        /// <summary>
        /// Report download progress through the database if necessary.
        /// </summary>
        private void ReportProgress(State state, InnerState innerState)
        {
            long now = PolicyExtensions.GetCurrentMilliseconds();
            if (innerState.mBytesSoFar - innerState.mBytesNotified > DownloaderService.MinimumProgressStep &&
                now - innerState.mTimeLastNotification > DownloaderService.MinimumProgressTime)
            {
                // we store progress updates to the database here
                mInfo.CurrentBytes = innerState.mBytesSoFar;
                mDB.updateDownloadCurrentBytes(mInfo);

                innerState.mBytesNotified = innerState.mBytesSoFar;
                innerState.mTimeLastNotification = now;

                long totalBytesSoFar = innerState.mBytesThisSession + mService.mBytesSoFar;

                System.Diagnostics.Debug.WriteLine("DownloadThread : downloaded {0} out of {1}", mInfo.CurrentBytes, mInfo.TotalBytes);
                System.Diagnostics.Debug.WriteLine("DownloadThread :      total {0} out of {1}", totalBytesSoFar, mService.mTotalLength);

                mService.NotifyUpdateBytes(totalBytesSoFar);
            }
        }

        /// <summary>
        /// Write a data buffer to the destination file.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="data">data buffer containing the data to write</param>
        /// <param name="bytesRead">bytesRead how many bytes to write from the buffer</param>
        private static void WriteDataToDestination(State state, byte[] data, int bytesRead)
        {
            bool finished = false;

            while (!finished)
            {
                try
                {
                    if (state.mStream == null)
                    {
                        state.mStream = new FileStream(state.mFilename, FileMode.Append);
                    }

                    state.mStream.Write(data, 0, bytesRead);

                    // we close after every write - todo: this may be too inefficient
                    CloseDestination(state);

                    finished = true;
                }
                catch (IOException ex)
                {
                    if (!Helpers.IsExternalMediaMounted())
                    {
                        throw new StopRequestException(DownloadStatus.DeviceNotFoundError,
                                                       "external media not mounted while writing destination file");
                    }

                    long availableBytes = Helpers.GetAvailableBytes(Helpers.GetFileSystemRoot(state.mFilename));

                    if (availableBytes < bytesRead)
                    {
                        throw new StopRequestException(DownloadStatus.InsufficientSpaceError,
                                                       "insufficient space while writing destination file",
                                                       ex);
                    }

                    throw new StopRequestException(DownloadStatus.FileError,
                                                   "while writing destination file: " + ex.Message,
                                                   ex);
                }
            }
        }

        /// <summary>
        /// Called when we've reached the end of the HTTP response stream, to update the database and
        /// check for consistency.
        /// </summary>
        private void HandleEndOfStream(State state, InnerState innerState)
        {
            System.Diagnostics.Debug.WriteLine("HandleEndOfStream");

            mInfo.CurrentBytes = innerState.mBytesSoFar;

            //// this should always be set from the market
            //if (innerState.mHeaderContentLength == null)
            //{
            //    mInfo.TotalBytes = innerState.mBytesSoFar;
            //}
            mDB.updateDownload(mInfo);

            bool lengthMismatched = innerState.mHeaderContentLength != null &&
                                    innerState.mBytesSoFar != int.Parse(innerState.mHeaderContentLength);
            if (lengthMismatched)
            {
                string message;
                int finalStatus;
                if (CannotResume(innerState))
                {
                    finalStatus = DownloadStatus.CannotResume;
                    message = "mismatched content length";
                }
                else
                {
                    finalStatus = GetFinalStatusForHttpError(state);
                    message = "closed socket before end of file";
                }

                throw new StopRequestException(finalStatus, message);
            }
        }

        private static bool CannotResume(InnerState innerState)
        {
            return innerState.mBytesSoFar > 0 && innerState.mHeaderETag == null;
        }

        /// <summary>
        /// Read some data from the HTTP response stream, handling I/O errors.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="innerState"></param>
        /// <param name="data">data buffer to use to read data</param>
        /// <param name="entityStream">entityStream stream for reading the HTTP response entity</param>
        /// <returns>the number of bytes actually read or -1 if the end of the stream has been reached</returns>
        private int ReadFromResponse(State state, InnerState innerState, byte[] data, Stream entityStream)
        {
            try
            {
                return entityStream.Read(data, 0, data.Length);
            }
            catch (IOException ex)
            {
                LogNetworkState();
                mInfo.CurrentBytes = innerState.mBytesSoFar;
                mDB.updateDownload(mInfo);

                string message;
                int finalStatus;
                if (CannotResume(innerState))
                {
                    finalStatus = DownloadStatus.CannotResume;
                    message = string.Format("while reading response: {0}, can't resume interrupted download with no ETag", ex);
                }
                else
                {
                    finalStatus = GetFinalStatusForHttpError(state);
                    message = string.Format("while reading response: {0}", ex);
                }

                throw new StopRequestException(finalStatus, message, ex);
            }
        }

        /// <summary>
        /// Open a stream for the HTTP response entity, handling I/O errors.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="response"></param>
        /// <returns>an InputStream to read the response entity</returns>
        private Stream OpenResponseEntity(State state, HttpWebResponse response)
        {
            try
            {
                return response.GetResponseStream();
            }
            catch (IOException ex)
            {
                LogNetworkState();
                throw new StopRequestException(GetFinalStatusForHttpError(state),
                                               string.Format("while getting entity: {0}", ex),
                                               ex);
            }
        }

        private void LogNetworkState()
        {
            var network = mService.GetNetworkAvailabilityState(mDB) == NetworkConstants.NETWORK_OK ? "Up" : "Down";
            System.Diagnostics.Debug.WriteLine(string.Format("Network is {0}.", network));
        }

        /// <summary>
        /// Read HTTP response headers and take appropriate action, including setting up the destination
        /// file and updating the database.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="innerState"></param>
        /// <param name="response"></param>
        private void ProcessResponseHeaders(State state, InnerState innerState, HttpWebResponse response)
        {
            if (!innerState.mContinuingDownload)
            {
                ReadResponseHeaders(state, innerState, response);

                try
                {
                    state.mFilename = mService.GenerateSaveFile(mInfo.FileName, mInfo.TotalBytes);
                }
                catch (DownloaderService.GenerateSaveFileError exc)
                {
                    throw new StopRequestException(exc.mStatus, exc.Message);
                }

                try
                {
                    if (!System.IO.File.Exists(state.mFilename))
                    {
                        // make sure the directory exists
                        var path = Helpers.GetSaveFilePath(mService);

                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        state.mStream = new FileStream(state.mFilename, FileMode.Create);
                    }
                    else
                    {
                        state.mStream = new FileStream(state.mFilename, FileMode.Open);
                    }
                }
                catch (Exception ex)
                {
                    throw new StopRequestException(DownloadStatus.FileError,
                                                   string.Format("while opening destination file: {0}", ex),
                                                   ex);
                }

                System.Diagnostics.Debug.WriteLine("DownloadThread : writing {0} to {1}", mInfo.Uri, state.mFilename);

                UpdateDatabaseFromHeaders(state, innerState);

                // check connectivity again now that we know the total size
                CheckConnectivity(state);
            }
        }

        /// <summary>
        /// Update necessary database fields based on values of HTTP response headers that have been read.
        /// </summary>
        private void UpdateDatabaseFromHeaders(State state, InnerState innerState)
        {
            mInfo.ETag = innerState.mHeaderETag;
            mDB.updateDownload(mInfo);
        }

        /// <summary>
        ///  Read headers from the HTTP response and store them into local state.
        /// </summary>
        private void ReadResponseHeaders(State state, InnerState innerState, HttpWebResponse response)
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
            header = response.GetResponseHeader("Content-Type");
            if (header != null && header != "application/vnd.android.obb")
            {
                throw new StopRequestException(DownloadStatus.FileDeliveredIncorrectly, "file delivered with incorrect Mime type");
            }

            string headerTransferEncoding = response.GetResponseHeader("Transfer-Encoding");
            //if (!string.IsNullOrEmpty(headerTransferEncoding))
            //{
            header = response.GetResponseHeader("Content-Length");
            if (header != null)
            {
                innerState.mHeaderContentLength = header;
                // this is always set from Market
                long contentLength = long.Parse(innerState.mHeaderContentLength);
                if (contentLength != -1 && contentLength != mInfo.TotalBytes)
                {
                    // we're most likely on a bad wifi connection -- we should probably
                    // also look at the mime type --- but the size mismatch is enough
                    // to tell us that something is wrong here
                    Log.Error(DownloaderService.TAG, "Incorrect file size delivered.");
                }
            }
            //}
            //else
            //{
            //    // Ignore content-length with transfer-encoding - 2616 4.4 3
            //    System.Diagnostics.Debug.WriteLine("DownloadThread : ignoring content-length because of xfer-encoding");
            //}

            System.Diagnostics.Debug.WriteLine("DownloadThread : Content-Disposition: " + innerState.mHeaderContentDisposition);
            System.Diagnostics.Debug.WriteLine("DownloadThread : Content-Length: " + innerState.mHeaderContentLength);
            System.Diagnostics.Debug.WriteLine("DownloadThread : Content-Location: " + innerState.mHeaderContentLocation);
            System.Diagnostics.Debug.WriteLine("DownloadThread : ETag: " + innerState.mHeaderETag);
            System.Diagnostics.Debug.WriteLine("DownloadThread : Transfer-Encoding: " + headerTransferEncoding);

            bool noSizeInfo = innerState.mHeaderContentLength == null &&
                              (headerTransferEncoding == null || !"chunked".Equals(headerTransferEncoding, StringComparison.OrdinalIgnoreCase));
            if (noSizeInfo)
            {
                throw new StopRequestException(DownloadStatus.HttpDataError, "can't know size of download, giving up");
            }
        }

        /// <summary>
        /// Check the HTTP response status and handle anything unusual (e.g. not 200/206).
        /// </summary>
        private void HandleExceptionalStatus(State state, InnerState innerState, HttpWebResponse response)
        {
            HttpStatusCode statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.ServiceUnavailable && mInfo.FailedCount < DownloaderService.MaximumRetries)
            {
                HandleServiceUnavailable(state, response);
            }
            switch (statusCode)
            {
                case HttpStatusCode.TemporaryRedirect:
                case HttpStatusCode.SeeOther:
                case HttpStatusCode.Found:
                case HttpStatusCode.Moved:
                    HandleRedirect(state, response, statusCode);
                    break;
            }

            HttpStatusCode expectedStatus = innerState.mContinuingDownload
                                                ? HttpStatusCode.PartialContent
                                                : HttpStatusCode.OK;
            if (statusCode != expectedStatus)
            {
                HandleOtherStatus(state, innerState, statusCode);
            }
            else
            {
                // no longer redirected
                state.mRedirectCount = 0;
            }
        }


        /// <summary>
        /// Handle a status that we don't know how to deal with properly.
        /// </summary>
        private static void HandleOtherStatus(State state, InnerState innerState, HttpStatusCode statusCode)
        {
            int finalStatus;
            if (DownloaderService.isStatusError((int) statusCode))
            {
                finalStatus = (int) statusCode;
            }
            else if ((int) statusCode >= 300 && (int) statusCode < 400)
            {
                finalStatus = DownloadStatus.UnhandledRedirect;
            }
            else if (innerState.mContinuingDownload && (int) statusCode == DownloadStatus.Success)
            {
                finalStatus = DownloadStatus.CannotResume;
            }
            else
            {
                finalStatus = DownloadStatus.UnhandledHttpCode;
            }

            throw new StopRequestException(finalStatus, "http error " + statusCode);
        }

        /// <summary>
        /// Handle a 3xx redirect status.
        /// </summary>
        private void HandleRedirect(State state, HttpWebResponse response, HttpStatusCode statusCode)
        {
            System.Diagnostics.Debug.WriteLine("got HTTP redirect " + statusCode);

            if (state.mRedirectCount >= DownloaderService.MAX_REDIRECTS)
            {
                throw new StopRequestException(DownloadStatus.TooManyRedirects, "too many redirects");
            }
            string header = response.GetResponseHeader("Location");
            if (header == null)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine("Redirecting to " + header);

            string newUri;
            try
            {
                newUri = new URI(mInfo.Uri).Resolve(new URI(header)).ToString();
            }
            catch (URISyntaxException)
            {
                System.Diagnostics.Debug.WriteLine("Couldn't resolve redirect URI {0} for {1}", header, mInfo.Uri);
                throw new StopRequestException(DownloadStatus.HttpDataError, "Couldn't resolve redirect URI");
            }

            ++state.mRedirectCount;
            state.mRequestUri = newUri;
            if ((int) statusCode == 301 || (int) statusCode == 303)
            {
                // use the new URI for all future requests (should a retry/resume be necessary)
                state.mNewUri = newUri;
            }

            throw new RetryDownloadException();
        }

        /// <summary>
        ///   Add headers for this download to the HTTP request to allow for resume.
        /// </summary>
        private static void AddRequestHeaders(InnerState innerState, HttpWebRequest request)
        {
            if (innerState.mContinuingDownload)
            {
                if (innerState.mHeaderETag != null)
                {
                    request.Headers.Add("If-Match", innerState.mHeaderETag);
                }
                request.AddRange(innerState.mBytesSoFar);
            }

            // request.SendChunked = true;
        }

        /// <summary>
        ///   Handle a 503 Service Unavailable status by processing the Retry-After header.
        /// </summary>
        private static void HandleServiceUnavailable(State state, HttpWebResponse response)
        {
            System.Diagnostics.Debug.WriteLine("DownloadThread : got HTTP response code 503");
            state.mCountRetry = true;
            string header = response.GetResponseHeader("Retry-After");
            System.Diagnostics.Debug.WriteLine("DownloadThread : Retry-After :" + header);

            if (!string.IsNullOrWhiteSpace(header) && int.TryParse(header, out state.mRetryAfter))
            {
                if (state.mRetryAfter < 0)
                {
                    state.mRetryAfter = 0;
                }
                else
                {
                    if (state.mRetryAfter < DownloaderService.MinimumRetryAfter)
                    {
                        state.mRetryAfter = DownloaderService.MinimumRetryAfter;
                    }
                    else if (state.mRetryAfter > DownloaderService.MAX_RETRY_AFTER)
                    {
                        state.mRetryAfter = DownloaderService.MAX_RETRY_AFTER;
                    }
                    state.mRetryAfter += Helpers.Random.Next(DownloaderService.MinimumRetryAfter + 1);
                    state.mRetryAfter *= 1000;
                }
            }

            throw new StopRequestException(DownloadStatus.WaitingToRetry, "got 503 Service Unavailable, will retry later");
        }

        /// <summary>
        ///   Send the request to the server, handling any I/O exceptions.
        /// </summary>
        private HttpWebResponse SendRequest(State state, HttpWebRequest request)
        {
            try
            {
                return (HttpWebResponse) request.GetResponse();
            }
            catch (IllegalArgumentException ex)
            {
                throw new StopRequestException(DownloadStatus.HttpDataError, "while trying to execute request: " + ex.Message, ex);
            }
            catch (IOException ex)
            {
                LogNetworkState();
                throw new StopRequestException(GetFinalStatusForHttpError(state), "while trying to execute request: " + ex.Message, ex);
            }
        }

        private int GetFinalStatusForHttpError(State state)
        {
            if (mService.GetNetworkAvailabilityState(mDB) != NetworkConstants.NETWORK_OK)
            {
                return DownloadStatus.WaitingForNetwork;
            }
            else if (mInfo.FailedCount < DownloaderService.MaximumRetries)
            {
                state.mCountRetry = true;
                return DownloadStatus.WaitingToRetry;
            }
            else
            {
                Log.Warn(DownloaderService.TAG, "reached max retries for " + mInfo.FailedCount);
                return DownloadStatus.HttpDataError;
            }
        }

        /**
     * Prepare the destination file to receive data.  If the file already exists, we'll set up
     * appropriately for resumption.
     */

        private void SetupDestinationFile(State state, InnerState innerState)
        {
            if (state.mFilename != null)
            {
                // only true if we've already run a thread for this download
                if (!Helpers.IsFilenameValid(state.mFilename))
                {
                    // this should never happen
                    throw new StopRequestException(DownloadStatus.FileError, "found invalid internal destination filename");
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
                    else if (mInfo.ETag == null)
                    {
                        // This should've been caught upon failure
                        f.Delete();
                        throw new StopRequestException(DownloadStatus.CannotResume, "Trying to resume a download that can't be resumed");
                    }
                    else
                    {
                        // All right, we'll be able to resume this download
                        try
                        {
                            state.mStream = new FileStream(state.mFilename, FileMode.Append);
                        }
                        catch (FileNotFoundException exc)
                        {
                            throw new StopRequestException(DownloadStatus.FileError, "while opening destination for resuming: " + exc, exc);
                        }
                        innerState.mBytesSoFar = (int) fileLength;
                        if (mInfo.TotalBytes != -1)
                        {
                            innerState.mHeaderContentLength = mInfo.TotalBytes.ToString();
                        }
                        innerState.mHeaderETag = mInfo.ETag;
                        innerState.mContinuingDownload = true;
                    }
                }
            }

            if (state.mStream != null)
            {
                CloseDestination(state);
            }
        }

        /**
     * Stores information about the completed download, and notifies the initiating application.
     */

        private void NotifyDownloadCompleted(int status, bool countRetry, int retryAfter, int redirectCount, bool gotData, string filename)
        {
            System.Diagnostics.Debug.WriteLine("NotifyDownloadCompleted");
            UpdateDownloadDatabase(status, countRetry, retryAfter, redirectCount, gotData, filename);
            if (DownloaderService.isStatusCompleted(status))
            {
                // TBD: send status update?
            }
        }

        private void UpdateDownloadDatabase(int status, bool countRetry, int retryAfter, int redirectCount, bool gotData, string filename)
        {
            System.Diagnostics.Debug.WriteLine("UpdateDownloadDatabase");
            mInfo.Status = status;
            mInfo.RetryAfter = retryAfter;
            mInfo.RedirectCount = redirectCount;
            mInfo.LastModified = PolicyExtensions.GetCurrentMilliseconds();
            if (!countRetry)
            {
                mInfo.FailedCount = 0;
            }
            else if (gotData)
            {
                mInfo.FailedCount = 1;
            }
            else
            {
                mInfo.FailedCount++;
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

        #region Nested type: RetryDownloadException

        private class RetryDownloadException : Exception
        {
            public RetryDownloadException()
                : base("Retrying download...")
            {
                System.Diagnostics.Debug.WriteLine(Message);
            }
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
            public FileStream mStream;

            public State(DownloadInfo info, DownloaderService service)
            {
                mRedirectCount = info.RedirectCount;
                mRequestUri = info.Uri;
                mFilename = service.GenerateTempSaveFileName(info.FileName);
            }
        }

        #endregion

        #region Nested type: StopRequestException

        private class StopRequestException : Exception
        {
            public readonly int mFinalStatus;

            public StopRequestException(int finalStatus, string message)
                : this(finalStatus, message, null)
            {
            }

            public StopRequestException(int finalStatus, string message, Exception throwable)
                : base(message, throwable)
            {
                System.Diagnostics.Debug.WriteLine(message);

                mFinalStatus = finalStatus;
            }
        }

        #endregion
    }
}
