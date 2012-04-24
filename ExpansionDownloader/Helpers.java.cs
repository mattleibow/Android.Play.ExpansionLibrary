using System;
using Android.Content;
using Android.OS;
using Android.Util;
using Java.IO;
using Java.Lang;
using Java.Util.Regex;
using Environment = System.Environment;
using Exception = System.Exception;
using Pattern = Java.Util.Regex.Pattern;
using Random = Java.Util.Random;
using String = Java.Lang.String;

namespace ExpansionDownloader
{
    public class Helpers
    {
        public static Random sRandom = new Random(Environment.TickCount);

        /** Regex used to parse content-disposition headers */
        private static readonly Pattern CONTENT_DISPOSITION_PATTERN = Pattern.Compile("attachment;\\s*filename\\s*=\\s*\"([^\"]*)\"");

        private Helpers()
        {
        }

        /*
     * Parse the Content-Disposition HTTP Header. The format of the header is
     * defined here: http://www.w3.org/Protocols/rfc2616/rfc2616-sec19.html This
     * header provides a filename for content that is going to be downloaded to
     * the file system. We only support the attachment type.
     */

        private static string parseContentDisposition(string contentDisposition)
        {
            try
            {
                Matcher m = CONTENT_DISPOSITION_PATTERN.Matcher(contentDisposition);
                if (m.Find())
                {
                    return m.Group(1);
                }
            }
            catch (IllegalStateException)
            {
                // This function is defined as returning null when it can't parse
                // the header
            }
            return null;
        }

        /**
     * @return the root of the filesystem containing the given path
     */

        public static File getFilesystemRoot(string path)
        {
            File cache = Android.OS.Environment.DownloadCacheDirectory;
            if (path.StartsWith(cache.Path))
            {
                return cache;
            }
            File external = Android.OS.Environment.ExternalStorageDirectory;
            if (path.StartsWith(external.Path))
            {
                return external;
            }
            throw new IllegalArgumentException("Cannot determine filesystem root for " + path);
        }

        public static bool isExternalMediaMounted()
        {
            if (Android.OS.Environment.ExternalStorageState != Android.OS.Environment.MediaMounted)
            {
                // No SD card found.
                if (Constants.LOGVV)
                {
                    Log.Debug(Constants.TAG, "no external storage");
                }
                return false;
            }
            return true;
        }

        /**
     * @return the number of bytes available on the filesystem rooted at the
     *         given File
     */

        public static long getAvailableBytes(File root)
        {
            var stat = new StatFs(root.Path);
            // put a bit of margin (in case creating the file grows the system by a
            // few blocks)
            long availableBlocks = (long) stat.AvailableBlocks - 4;
            return stat.BlockSize*availableBlocks;
        }

        /**
     * Checks whether the filename looks legitimate
     */

        public static bool isFilenameValid(string filename)
        {
            filename = new String(filename).ReplaceFirst("/+", "/"); // normalize leading slashes
            return filename.StartsWith(Android.OS.Environment.DownloadCacheDirectory.ToString()) ||
                   filename.StartsWith(Android.OS.Environment.ExternalStorageDirectory.ToString());
        }

        /*
     * Delete the given file from device
     */
        /* package */

        private static void deleteFile(string path)
        {
            try
            {
                var file = new File(path);
                file.Delete();
            }
            catch (Exception e)
            {
                Log.Warn(Constants.TAG, "file: '" + path + "' couldn't be deleted", e);
            }
        }

        /**
     * Showing progress in MB here. It would be nice to choose the unit (KB, MB,
     * GB) based on total file size, but given what we know about the expected
     * ranges of file sizes for APK expansion files, it's probably not necessary.
     * 
     * @param overallProgress
     * @param overallTotal
     * @return
     */

        public static string getDownloadProgressString(long overallProgress, long overallTotal)
        {
            if (overallTotal == 0)
            {
                if (Constants.LOGVV)
                {
                    Log.Error(Constants.TAG, "Notification called when total is zero");
                }
                return string.Empty;
            }
            return string.Format("{0:00} MB / {1:00} MB",
                                 overallProgress/(1024.0f*1024.0f),
                                 overallTotal/(1024.0f*1024.0f));
        }

        /**
     * Adds a percentile to getDownloadProgressString.
     * 
     * @param overallProgress
     * @param overallTotal
     * @return
     */

        public static string getDownloadProgressStringNotification(long overallProgress, long overallTotal)
        {
            if (overallTotal == 0)
            {
                if (Constants.LOGVV)
                {
                    Log.Error(Constants.TAG, "Notification called when total is zero");
                }
                return "";
            }
            return getDownloadProgressString(overallProgress, overallTotal) + " (" +
                   getDownloadProgressPercent(overallProgress, overallTotal) + ")";
        }

        public static string getDownloadProgressPercent(long overallProgress, long overallTotal)
        {
            if (overallTotal == 0)
            {
                if (Constants.LOGVV)
                {
                    Log.Error(Constants.TAG, "Notification called when total is zero");
                }
                return string.Empty;
            }
            return (overallProgress*100/overallTotal) + "%";
        }

        public static string getSpeedString(float bytesPerMillisecond)
        {
            return string.Format("{0:00}", bytesPerMillisecond*1000/1024);
        }

        public static string getTimeRemaining(long durationInMilliseconds)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(durationInMilliseconds);
            return timeSpan.ToString(timeSpan.Hours > 0 ? "HH:mm" : "mm:ss");
        }

        /**
     * Returns the file name (without full path) for an Expansion APK file from
     * the given context.
     * 
     * @param c the context
     * @param mainFile true for main file, false for patch file
     * @param versionCode the version of the file
     * @return string the file name of the expansion file
     */

        public static string getExpansionAPKFileName(Context c, bool mainFile, int versionCode)
        {
            return (mainFile ? "main." : "patch.") + versionCode + "." + c.PackageName + ".obb";
        }

        /**
     * Returns the filename (where the file should be saved) from info about a
     * download
     */

        public static string generateSaveFileName(Context c, string fileName)
        {
            string path = getSaveFilePath(c) + File.Separator + fileName;
            return path;
        }

        public static string getSaveFilePath(Context c)
        {
            File root = Android.OS.Environment.ExternalStorageDirectory;
            string path = root + Constants.EXP_PATH + c.PackageName;
            return path;
        }

        /**
     * Helper function to ascertain the existence of a file and return
     * true/false appropriately
     * 
     * @param c the app/activity/service context
     * @param fileName the name (sans path) of the file to query
     * @param fileSize the size that the file must match
     * @param deleteFileOnMismatch if the file sizes do not match, delete the
     *            file
     * @return true if it does exist, false otherwise
     */

        public static bool doesFileExist(Context c, string fileName, long fileSize, bool deleteFileOnMismatch)
        {
            // the file may have been delivered by Market --- let's make sure
            // it's the size we expect
            var fileForNewFile = new File(generateSaveFileName(c, fileName));
            if (fileForNewFile.Exists())
            {
                if (fileForNewFile.Length() == fileSize)
                {
                    return true;
                }
                if (deleteFileOnMismatch)
                {
                    // delete the file --- we won't be able to resume
                    // because we cannot confirm the integrity of the file
                    fileForNewFile.Delete();
                }
            }
            return false;
        }

        /**
     * Converts download states that are returned by the {@link 
     * IDownloaderClient#onDownloadStateChanged} callback into usable strings.
     * This is useful if using the state strings built into the library to display user messages.
     * @param state One of the STATE_* constants from {@link IDownloaderClient}.
     * @return string resource ID for the corresponding string.
     */

        public static string getDownloaderStringResourceIDFromState(DownloaderClientState state)
        {
            switch (state)
            {
                case DownloaderClientState.STATE_IDLE:
                    return "Waiting for download to start";
                case DownloaderClientState.STATE_FETCHING_URL:
                    return "Looking for resources to download";
                case DownloaderClientState.STATE_CONNECTING:
                    return "Connecting to the download server";
                case DownloaderClientState.STATE_DOWNLOADING:
                    return "Downloading resources";
                case DownloaderClientState.STATE_COMPLETED:
                    return "Download finished";
                case DownloaderClientState.STATE_PAUSED_NETWORK_UNAVAILABLE:
                    return "Download paused because no network is available";
                case DownloaderClientState.STATE_PAUSED_BY_REQUEST:
                    return "Download paused";
                case DownloaderClientState.STATE_PAUSED_WIFI_DISABLED_NEED_CELLULAR_PERMISSION:
                    return "Download paused because wifi is disabled";
                case DownloaderClientState.STATE_PAUSED_NEED_CELLULAR_PERMISSION:
                    return "Download paused because wifi is unavailable";
                case DownloaderClientState.STATE_PAUSED_ROAMING:
                    return "Download paused because you are roaming";
                case DownloaderClientState.STATE_PAUSED_NETWORK_SETUP_FAILURE:
                    return "Download paused. Test a website in browser";
                case DownloaderClientState.STATE_PAUSED_SDCARD_UNAVAILABLE:
                    return "Download paused because the external storage is unavailable";
                case DownloaderClientState.STATE_FAILED_UNLICENSED:
                    return "Download failed because you may not have purchased this app";
                case DownloaderClientState.STATE_FAILED_FETCHING_URL:
                    return "Download failed because the resources could not be found";
                case DownloaderClientState.STATE_FAILED_SDCARD_FULL:
                    return "Download failed because the external storage is full";
                case DownloaderClientState.STATE_FAILED:
                    return "Download failed";
                case DownloaderClientState.STATE_FAILED_CANCELED:
                    return "Download cancelled";
                default:
                    return "Starting...";
            }
        }
    }
}