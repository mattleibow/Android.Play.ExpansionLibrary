// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Helpers.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The helpers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader.Service
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    using Android.Content;
    using Android.OS;

    using Debug = System.Diagnostics.Debug;
    using Environment = System.Environment;

    /// <summary>
    /// The helpers.
    /// </summary>
    public static class Helpers
    {
        #region Static Fields

        /// <summary>
        /// The random.
        /// </summary>
        public static readonly Random Random;

        /// <summary>
        /// The content disposition pattern.
        /// </summary>
        private static readonly string ContentDispositionPattern; // to parse content-disposition headers

        /// <summary>
        /// The megabytes.
        /// </summary>
        private static readonly float Megabytes;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes static members of the <see cref="Helpers"/> class.
        /// </summary>
        static Helpers()
        {
            Megabytes = 1024.0F * 1024.0F;
            Random = new Random(Environment.TickCount);
            ContentDispositionPattern = @"attachment;\s*filename\s*=\s*""([^\""]*)\""";
            ExpansionPath = string.Format("{0}Android{0}obb{0}", Path.DirectorySeparatorChar);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the expansion files root path.
        /// </summary>
        public static string ExpansionPath { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the externl media is mounted.
        /// </summary>
        public static bool IsExternalMediaMounted
        {
            get
            {
                bool storageMissing = Android.OS.Environment.ExternalStorageState != Android.OS.Environment.MediaMounted;
                if (storageMissing)
                {
                    // No SD card found.
                    Debug.WriteLine("no external storage");
                }

                return !storageMissing;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Delete the given file from device
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        public static void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("File '{0}' couldn't be deleted. ({1})", path, ex.Message);
            }
        }

        /// <summary>
        /// Helper function to ascertain the existence of a file and return true/false appropriately.
        /// </summary>
        /// <param name="c">
        /// the app/activity/service context
        /// </param>
        /// <param name="fileName">
        /// the name (sans path) of the file to query
        /// </param>
        /// <param name="fileSize">
        /// the size that the file must match
        /// </param>
        /// <param name="deleteFileOnMismatch">
        /// if the file sizes do not match, delete the file
        /// </param>
        /// <returns>
        /// true if it does exist, false otherwise
        /// </returns>
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
        /// Returns the filename (where the file should be saved) from info about a download
        /// </summary>
        /// <param name="c">
        /// The c.
        /// </param>
        /// <param name="fileName">
        /// The file Name.
        /// </param>
        /// <returns>
        /// The generate save file name.
        /// </returns>
        public static string GenerateSaveFileName(Context c, string fileName)
        {
            return Path.Combine(GetSaveFilePath(c), fileName);
        }

        /// <summary>
        /// Get the number of bytes available on the filesystem rooted at the given File.
        /// </summary>
        /// <param name="root">
        /// The root.
        /// </param>
        /// <returns>
        /// The get available bytes.
        /// </returns>
        public static long GetAvailableBytes(string root)
        {
            var stat = new StatFs(root);

            // put a bit of margin (in case creating the file grows the system by a few blocks)
            long availableBlocks = (long)stat.AvailableBlocks - 4;
            return stat.BlockSize * availableBlocks;
        }

        /// <summary>
        /// Gets the download progress percent based on the current and total bytes.
        /// </summary>
        /// <param name="overallProgress">
        /// The overall progress.
        /// </param>
        /// <param name="overallTotal">
        /// The overall total.
        /// </param>
        /// <returns>
        /// The download progress percent.
        /// </returns>
        public static string GetDownloadProgressPercent(long overallProgress, long overallTotal)
        {
            if (overallTotal == 0)
            {
                Debug.WriteLine("Notification called when total is zero");
                return string.Empty;
            }

            return string.Format("{0}%", overallProgress * 100 / overallTotal);
        }

        /// <summary>
        /// Showing progress in MB here. 
        /// It would be nice to choose the unit (KB, MB, GB) based on total 
        /// file size, but given what we know about the expected ranges of file
        /// sizes for APK expansion files, it's probably not necessary.
        /// </summary>
        /// <param name="overallProgress">
        /// The overall Progress.
        /// </param>
        /// <param name="overallTotal">
        /// The overall Total.
        /// </param>
        /// <returns>
        /// The get download progress string.
        /// </returns>
        public static string GetDownloadProgressString(long overallProgress, long overallTotal)
        {
            if (overallTotal == 0)
            {
                Debug.WriteLine("Notification called when total is zero");
                return string.Empty;
            }

            return string.Format("{0:0.00} MB / {1:0.00} MB", overallProgress / Megabytes, overallTotal / Megabytes);
        }

        /// <summary>
        /// Adds a percentile to GetDownloadProgressString.
        /// </summary>
        /// <param name="overallProgress">
        /// The overall Progress.
        /// </param>
        /// <param name="overallTotal">
        /// The overall Total.
        /// </param>
        /// <returns>
        /// The get download progress string notification.
        /// </returns>
        public static string GetDownloadProgressStringNotification(long overallProgress, long overallTotal)
        {
            if (overallTotal == 0)
            {
                Debug.WriteLine("Notification called when total is zero");
                return string.Empty;
            }

            return string.Format(
                "{0} ({1})", 
                GetDownloadProgressString(overallProgress, overallTotal), 
                GetDownloadProgressPercent(overallProgress, overallTotal));
        }

	    /// <summary>
	    /// Converts download states that are returned by the
	    /// <see cref="IDownloaderClient.OnDownloadStateChanged"/> callback into usable strings.
	    /// This is useful if using the state strings built into the library to display user messages.
	    /// </summary>
	    /// <param name="context">
	    /// The context to use when reading resources.
	    /// </param>
	    /// <param name="state">
	    /// A string representing one of the STATE_* constant values from <see cref="IDownloaderClient"/>.
	    /// </param>
	    /// <returns>
	    /// A string id that corresponds to the state message.
	    /// </returns>
	    public static string GetDownloaderStringFromState(Context context, DownloaderState state)
	    {
		    return context.GetString(GetDownloaderStringFromState(state));
	    }

	    /// <summary>
        /// Converts download states that are returned by the
        /// <see cref="IDownloaderClient.OnDownloadStateChanged"/> callback into usable string IDs.
        /// This is useful if using the state strings built into the library to display user messages.
        /// </summary>
        /// <param name="state">
        /// One of the STATE_* constants from <see cref="IDownloaderClient"/>.
        /// </param>
        /// <returns>
        /// A string ID that corresponds to the state message.
        /// </returns>
        public static int GetDownloaderStringFromState(DownloaderState state)
        {
            switch (state)
            {
                case DownloaderState.Idle:
                    return Resource.String.state_idle;
                case DownloaderState.FetchingUrl:
					return Resource.String.state_fetching_url;
                case DownloaderState.Connecting:
					return Resource.String.state_connecting;
                case DownloaderState.Downloading:
					return Resource.String.state_downloading;
                case DownloaderState.Completed:
					return Resource.String.state_completed;
                case DownloaderState.PausedNetworkUnavailable:
					return Resource.String.state_paused_network_unavailable;
                case DownloaderState.PausedByRequest:
					return Resource.String.state_paused_by_request;
                case DownloaderState.PausedWifiDisabled:
                case DownloaderState.PausedWifiDisabledNeedCellularPermission:
					return Resource.String.state_paused_wifi_disabled;
                case DownloaderState.PausedNeedWifi:
                case DownloaderState.PausedNeedCellularPermission:
					return Resource.String.state_paused_wifi_unavailable;
                case DownloaderState.PausedRoaming:
					return Resource.String.state_paused_roaming;
                case DownloaderState.PausedNetworkSetupFailure:
					return Resource.String.state_paused_network_setup_failure;
                case DownloaderState.PausedSdCardUnavailable:
					return Resource.String.state_paused_sdcard_unavailable;
                case DownloaderState.FailedUnlicensed:
					return Resource.String.state_failed_unlicensed;
                case DownloaderState.FailedFetchingUrl:
					return Resource.String.state_failed_fetching_url;
                case DownloaderState.FailedSdCardFull:
					return Resource.String.state_failed_sdcard_full;
                case DownloaderState.Failed:
					return Resource.String.state_failed;
                case DownloaderState.FailedCanceled:
					return Resource.String.state_failed_cancelled;
                default:
					return Resource.String.state_unknown;
            }
        }

        /// <summary>
        /// Returns the file name (without full path) for an Expansion APK file
        /// from the given context.
        /// </summary>
        /// <param name="c">
        /// the context
        /// </param>
        /// <param name="mainFile">
        /// true for main file, false for patch file
        /// </param>
        /// <param name="versionCode">
        /// the version of the file
        /// </param>
        /// <returns>
        /// the file name of the expansion file
        /// </returns>
        public static string GetExpansionApkFileName(Context c, bool mainFile, int versionCode)
        {
            return string.Format("{0}.{1}.{2}.obb", mainFile ? "main" : "patch", versionCode, c.PackageName);
        }

        /// <summary>
        /// Get the root of the filesystem containing the given path
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// The get file system root.
        /// </returns>
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

        /// <summary>
        /// Returns the path where the expansion files would be saved.
        /// </summary>
        /// <param name="c">
        /// The c.
        /// </param>
        /// <returns>
        /// The path where the files would be saved.
        /// </returns>
        public static string GetSaveFilePath(Context c)
        {
            string root = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            return root + ExpansionPath + c.PackageName;
        }

        /// <summary>
        /// Returns a string representation of the specified speed (KB/s).
        /// </summary>
        /// <param name="bytesPerMillisecond">
        /// The bytes per millisecond.
        /// </param>
        /// <returns>
        /// Returns a string (KB/s)
        /// </returns>
        public static string GetSpeedString(float bytesPerMillisecond)
        {
            return string.Format("{0:0.0}", bytesPerMillisecond * 1000 / 1024);
        }

        /// <summary>
        /// Returns the time remaining (HH:MM \ MM:SS).
        /// </summary>
        /// <param name="duration">
        /// The duration in milliseconds.
        /// </param>
        /// <returns>
        /// The time remaining string.
        /// </returns>
        public static string GetTimeRemaining(long duration)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(duration);
            return timeSpan.ToString(timeSpan.Hours > 0 ? "hh\\:mm" : "mm\\:ss");
        }

        /// <summary>
        /// Checks whether the filename looks legitimate.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <returns>
        /// The is filename valid.
        /// </returns>
        public static bool IsFilenameValid(string filename)
        {
            filename = Regex.Replace(filename, "/+", "/"); // normalize leading slashes
            return filename.StartsWith(Android.OS.Environment.DownloadCacheDirectory.ToString())
                   || filename.StartsWith(Android.OS.Environment.ExternalStorageDirectory.ToString());
        }

        /// <summary>
        /// Parse the Content-Disposition HTTP Header. The format of the header is
        /// defined here: http://www.w3.org/Protocols/rfc2616/rfc2616-sec19.html This
        /// header provides a filename for content that is going to be downloaded to
        /// the file system. We only support the attachment type.
        /// </summary>
        /// <param name="contentDisposition">
        /// The content Disposition.
        /// </param>
        /// <returns>
        /// The parse content disposition.
        /// </returns>
        public static string ParseContentDisposition(string contentDisposition)
        {
            try
            {
                Match m = Regex.Match(contentDisposition, ContentDispositionPattern);
                if (m.Success)
                {
                    return m.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                // This function is defined as returning null when it can't parse the header
                Debug.WriteLine("can't parse the content disposition header {0}", ex.Message);
            }

            return null;
        }

        #endregion
    }
}