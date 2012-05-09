namespace ExpansionDownloader.Service
{
    using ExpansionDownloader.impl;

    /// <summary>
    /// The downloader service parameters.
    /// </summary>
    public static class DownloaderServiceParameters
    {
        #region Constants and Fields

        /// <summary>
        /// The flags parameter.
        /// </summary>
        public const string Flags = "flags";

        /// <summary>
        /// The messenger parameter.
        /// </summary>
        public const string Messenger = DownloaderServiceExtras.MessageHandler;

        #endregion
    }
}