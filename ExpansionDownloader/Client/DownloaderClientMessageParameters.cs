namespace ExpansionDownloader.Client
{
    using ExpansionDownloader.impl;

    /// <summary>
    /// The downloader client message parameters.
    /// </summary>
    public static class DownloaderClientMessageParameters
    {
        #region Constants and Fields

        /// <summary>
        /// The messenger.
        /// </summary>
        public const string Messenger = DownloaderServiceExtras.MessageHandler;

        /// <summary>
        /// The new state.
        /// </summary>
        public const string NewState = "newState";

        /// <summary>
        /// The progress.
        /// </summary>
        public const string Progress = "progress";

        #endregion
    }
}