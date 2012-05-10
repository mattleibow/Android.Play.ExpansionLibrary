namespace ExpansionDownloader
{
    /// <summary>
    /// The custom notification factory.
    /// </summary>
    public static class CustomNotificationFactory
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the maximum size, in bytes, of downloads that may go 
        /// over a mobile connection; or null if there's no limit
        /// </summary>
        public static long? MaxBytesOverMobile { get; set; }

        /// <summary>
        /// Gets or sets Notification.
        /// </summary>
        public static DownloadNotification.ICustomNotification Notification { get; set; }

        /// <summary>
        /// Gets or sets a recommended maximum size, in bytes, of downloads 
        /// that may go over a mobile connection; or null if there's no 
        /// recommended limit.
        /// The user will have the option to bypass this limit.
        /// </summary>
        public static long? RecommendedMaxBytesOverMobile { get; set; }

        #endregion
    }
}