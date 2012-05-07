using System;
using System.IO;
using System.Text.RegularExpressions;
using Android.Content;
using Android.OS;
using ExpansionDownloader.impl;

namespace ExpansionDownloader
{
    using ExpansionDownloader.Client;

    public static class Helpers
    {
        public static readonly Random Random;
        private static readonly string ContentDispositionPattern; // to parse content-disposition headers
        private static readonly float Megabytes;

        static Helpers()
        {
            Megabytes = 1024.0F*1024.0F;
            Random = new Random(System.Environment.TickCount);
            ContentDispositionPattern = @"attachment;\s*filename\s*=\s*""([^\""]*)\""";
        }

        /// <summary>
        /// Parse the Content-Disposition HTTP Header. The format of the header is
        /// defined here: http://www.w3.org/Protocols/rfc2616/rfc2616-sec19.html This
        /// header provides a filename for content that is going to be downloaded to
        /// the file system. We only support the attachment type.
        /// </summary>
        private static string ParseContentDisposition(string contentDisposition)
        {
            try
            {
                var m = Regex.Match(contentDisposition, ContentDispositionPattern);
                if (m.Success)
                {
                    return m.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                // This function is defined as returning null when it can't parse the header
                System.Diagnostics.Debug.WriteLine("can't parse the content disposition header {0}", ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Get the root of the filesystem containing the given path
        /// </summary>
        public static string GetFileSystemRoot(string path)
        {
            string cache = Android.OS.Environment.DownloadCacheDirectory.Path;
            if (path.StartsWith(cache))
            {
                return cache;
            }
            string external = Android.OS.Environment.ExternalStorageDirectory.Path;
            if (path.StartsWith(external))
            {
                return external;
            }
            throw new ArgumentException("Cannot determine filesystem root for " + path);
        }

        public static bool IsExternalMediaMounted()
        {
            var storageMissing = Android.OS.Environment.ExternalStorageState != Android.OS.Environment.MediaMounted;
            if (storageMissing) // No SD card found.
            {
                System.Diagnostics.Debug.WriteLine("no external storage");
            }

            return !storageMissing;
        }

        /// <summary>
        /// Get the number of bytes available on the filesystem rooted at the given File.
        /// </summary>
        public static long GetAvailableBytes(string root)
        {
            var stat = new StatFs(root);
            // put a bit of margin (in case creating the file grows the system by a few blocks)
            long availableBlocks = (long) stat.AvailableBlocks - 4;
            return stat.BlockSize*availableBlocks;
        }

        /// <summary>
        /// Checks whether the filename looks legitimate.
        /// </summary>
        public static bool IsFilenameValid(string filename)
        {
            filename = Regex.Replace(filename, "/+", "/"); // normalize leading slashes
            return filename.StartsWith(Android.OS.Environment.DownloadCacheDirectory.ToString()) ||
                   filename.StartsWith(Android.OS.Environment.ExternalStorageDirectory.ToString());
        }

        /// <summary>
        /// Delete the given file from device
        /// </summary>
        private static void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("File '{0}' couldn't be deleted. ({1})", path, ex.Message);
            }
        }

        /// <summary>
        /// Showing progress in MB here. It would be nice to choose the unit (KB, MB,
        /// GB) based on total file size, but given what we know about the expected
        /// ranges of file sizes for APK expansion files, it's probably not necessary.
        /// </summary>
        public static string GetDownloadProgressString(long overallProgress, long overallTotal)
        {
            if (overallTotal == 0)
            {
                System.Diagnostics.Debug.WriteLine("Notification called when total is zero");
                return string.Empty;
            }

            return string.Format("{0:0.00} MB / {1:0.00} MB", overallProgress/Megabytes, overallTotal/Megabytes);
        }

        /// <summary>
        /// Adds a percentile to GetDownloadProgressString.
        /// </summary>
        public static string GetDownloadProgressStringNotification(long overallProgress, long overallTotal)
        {
            if (overallTotal == 0)
            {
                System.Diagnostics.Debug.WriteLine("Notification called when total is zero");
                return string.Empty;
            }

            return string.Format("{0} ({1})",
                                 GetDownloadProgressString(overallProgress, overallTotal),
                                 GetDownloadProgressPercent(overallProgress, overallTotal));
        }

        public static string GetDownloadProgressPercent(long overallProgress, long overallTotal)
        {
            if (overallTotal == 0)
            {
                System.Diagnostics.Debug.WriteLine("Notification called when total is zero");
                return string.Empty;
            }
            return string.Format("{0}%", overallProgress*100/overallTotal);
        }

        public static string GetSpeedString(float bytesPerMillisecond)
        {
            return string.Format("{0:00}", bytesPerMillisecond*1000/1024);
        }

        public static string GetTimeRemaining(long durationInMilliseconds)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(durationInMilliseconds);
            return timeSpan.ToString(timeSpan.Hours > 0 ? "hh\\:mm" : "mm\\:ss");
        }

        /// <summary>
        /// Returns the file name (without full path) for an Expansion APK file from the given context.
        /// </summary>
        /// <param name="c">the context</param>
        /// <param name="mainFile">true for main file, false for patch file</param>
        /// <param name="versionCode">the version of the file</param>
        /// <returns>the file name of the expansion file</returns>
        public static string GetExpansionApkFileName(Context c, bool mainFile, int versionCode)
        {
            return string.Format("{0}.{1}.{2}.obb", mainFile ? "main" : "patch", versionCode, c.PackageName);
        }

        /// <summary>
        /// Returns the filename (where the file should be saved) from info about a download
        /// </summary>
        public static string GenerateSaveFileName(Context c, string fileName)
        {
            return Path.Combine(GetSaveFilePath(c), fileName);
        }

        public static string GetSaveFilePath(Context c)
        {
            var root = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            return root + DownloaderService.ExpansionPath + c.PackageName;
        }

        /// <summary>
        /// Helper function to ascertain the existence of a file and return true/false appropriately.
        /// </summary>
        /// <param name="c">the app/activity/service context</param>
        /// <param name="fileName">the name (sans path) of the file to query</param>
        /// <param name="fileSize">the size that the file must match</param>
        /// <param name="deleteFileOnMismatch">if the file sizes do not match, delete the file</param>
        /// <returns>true if it does exist, false otherwise</returns>
        public static bool DoesFileExist(Context c, string fileName, long fileSize, bool deleteFileOnMismatch)
        {
            // the file may have been delivered by Market - let's make sure it's the size we expect
            var fileForNewFile = new FileInfo(GenerateSaveFileName(c, fileName));
            if (fileForNewFile.Exists)
            {
                if (fileForNewFile.Length == fileSize)
                {
                    return true;
                }
                if (deleteFileOnMismatch)
                {
                    // delete the file - we won't be able to resume because we cannot
                    // confirm the integrity of the file
                    fileForNewFile.Delete();
                }
            }
            return false;
        }

        /// <summary>
        /// Converts download states that are returned by the
        /// <see cref="IDownloaderClient.OnDownloadStateChanged"/> callback into usable strings.
        /// This is useful if using the state strings built into the library to display user messages.
        /// </summary>
        /// <param name="state">One of the STATE_* constants from <see cref="IDownloaderClient"/>.</param>
        /// <returns>resource ID for the corresponding string.</returns>
        public static string GetDownloaderStringFromState(DownloaderClientState state)
        {
            switch (state)
            {
                case DownloaderClientState.Idle:
                    return "Waiting for download to start";
                case DownloaderClientState.FetchingUrl:
                    return "Looking for resources to download";
                case DownloaderClientState.Connecting:
                    return "Connecting to the download server";
                case DownloaderClientState.Downloading:
                    return "Downloading resources";
                case DownloaderClientState.Completed:
                    return "Download finished";
                case DownloaderClientState.PausedNetworkUnavailable:
                    return "Download paused because no network is available";
                case DownloaderClientState.PausedByRequest:
                    return "Download paused";
                case DownloaderClientState.PausedWifiDisabledNeedCellularPermission:
                    return "Download paused because wifi is disabled";
                case DownloaderClientState.PausedNeedCellularPermission:
                    return "Download paused because wifi is unavailable";
                case DownloaderClientState.PausedRoaming:
                    return "Download paused because you are roaming";
                case DownloaderClientState.PausedNetworkSetupFailure:
                    return "Download paused. Test a website in browser";
                case DownloaderClientState.PausedSdCardUnavailable:
                    return "Download paused because the external storage is unavailable";
                case DownloaderClientState.FailedUnlicensed:
                    return "Download failed because you may not have purchased this app";
                case DownloaderClientState.FailedFetchingUrl:
                    return "Download failed because the resources could not be found";
                case DownloaderClientState.FailedSdCardFull:
                    return "Download failed because the external storage is full";
                case DownloaderClientState.Failed:
                    return "Download failed";
                case DownloaderClientState.FailedCanceled:
                    return "Download cancelled";
                default:
                    return "Starting...";
            }
        }
    }
}
