namespace ExpansionDownloader.impl
{
    using ExpansionDownloader.Service;

    /// <summary>
    /// The metadata columns.
    /// </summary>
    public class MetadataTable
    {
        #region Public Properties

        /// <summary>
        /// The apk version.
        /// </summary>
        public int ApkVersion { get; set; }

        /// <summary>
        /// The download status.
        /// </summary>
        public DownloadStatus DownloadStatus { get; set; }

        /// <summary>
        /// The flags.
        /// </summary>
        public DownloaderServiceFlags Flags { get; set; }

        #endregion
    }
}