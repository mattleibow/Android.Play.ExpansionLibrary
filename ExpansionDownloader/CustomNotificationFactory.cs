namespace ExpansionDownloader
{
    /// <summary>
    /// The custom notification factory.
    /// </summary>
    public static class CustomNotificationFactory
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets Notification.
        /// </summary>
        public static DownloadNotification.ICustomNotification Notification { get; set; }

        #endregion
    }
}