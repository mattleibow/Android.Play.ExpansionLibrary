namespace ExpansionDownloader.Service
{
    /// <summary>
    /// The downloader sevice actions.
    /// </summary>
    public static class DownloaderServiceActions
    {
        #region Constants and Fields

        /// <summary>
        /// The intent that gets sent when deleting the notification of a completed download
        /// </summary>
        public const string ActionHide = "android.intent.action.DOWNLOAD_HIDE";

        /// <summary>
        /// The intent that gets sent when clicking an incomplete/failed download
        /// </summary>
        public const string ActionList = "android.intent.action.DOWNLOAD_LIST";

        /// <summary>
        /// The intent that gets sent when clicking a successful download
        /// </summary>
        public const string ActionOpen = "android.intent.action.DOWNLOAD_OPEN";

        /// <summary>
        /// The intent that gets sent when the service must wake up for a retry.
        /// </summary>
        public const string ActionRetry = "android.intent.action.DOWNLOAD_WAKEUP";

        /// <summary>
        /// Broadcast intent action sent by the download manager when a download completes.
        /// </summary>
        public const string DownloadComplete = "lvldownloader.intent.action.DOWNLOAD_COMPLETE";

        /// <summary>
        /// Broadcast intent action sent by the download manager when download status changes.
        /// </summary>
        public const string DownloadStatus = "lvldownloader.intent.action.DOWNLOAD_STATUS";

        /// <summary>
        /// The downloads changed.
        /// </summary>
        public const string DownloadsChanged = "downloadsChanged";

        #endregion
    }
}