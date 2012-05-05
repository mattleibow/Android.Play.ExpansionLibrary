namespace ExpansionDownloader.impl
{
    public class DownloaderServiceExtras
    {
        /// <summary>
        /// The extra file name.
        /// </summary>
        public const string FileName = "downloadId";

        /// <summary>
        /// For intents used to notify the user that a download exceeds a size
        /// threshold, if this extra is true, WiFi is required for this download
        /// size; otherwise, it is only recommended.
        /// </summary>
        public const string IsWifiRequired = "isWifiRequired";

        /// <summary>
        /// The extra message handler.
        /// </summary>
        public const string MessageHandler = "EMH";

        /// <summary>
        /// The extra package name.
        /// </summary>
        public const string PackageName = "EPN";

        /// <summary>
        /// The extra pending intent.
        /// </summary>
        public const string PendingIntent = "EPI";
    }
}