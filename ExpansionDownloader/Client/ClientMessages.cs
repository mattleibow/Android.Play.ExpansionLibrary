namespace ExpansionDownloader.Client
{
    /// <summary>
    /// The downloader client messages.
    /// </summary>
    public enum ClientMessages
    {
        /// <summary>
        /// The download state changed.
        /// </summary>
        DownloadStateChanged = 10, 

        /// <summary>
        /// The download progress.
        /// </summary>
        DownloadProgress = 11, 

        /// <summary>
        /// The service connected.
        /// </summary>
        ServiceConnected = 12
    }
}