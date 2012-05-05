namespace ExpansionDownloader.impl
{
    public static class CustomNotificationFactory
    {
        public static DownloadNotification.ICustomNotification Notification { get; set; }

        /// <summary>
        /// Returns maximum size, in bytes, of downloads that may go over a mobile connection; or null if
        /// there's no limit
        /// </summary>
        public static long? MaxBytesOverMobile { get; set; }

        /// <summary>
        /// Gets or sets a recommended maximum size, in bytes, of downloads that may go over a mobile
        /// connection; or null if there's no recommended limit.  The user will have the option to bypass
        /// this limit.
        /// </summary>
        public static long? RecommendedMaxBytesOverMobile { get; set; }
    }
}