namespace ExpansionDownloader.Service
{
    /// <summary>
    /// The downloader service messages.
    /// </summary>
    public enum DownloaderServiceMessages
    {
        /// <summary>
        /// A request to abort a download.
        /// </summary>
        RequestAbortDownload = 1, 

        /// <summary>
        /// A request to pause a download.
        /// </summary>
        RequestPauseDownload = 2, 

        /// <summary>
        /// Update the download flags.
        /// </summary>
        SetDownloadFlags = 3, 

        /// <summary>
        /// A request to continue a download.
        /// </summary>
        RequestContinueDownload = 4, 

        /// <summary>
        /// A request for the download state.
        /// </summary>
        RequestDownloadState = 5, 

        /// <summary>
        /// A request to update the client.
        /// </summary>
        RequestClientUpdate = 6
    }
}