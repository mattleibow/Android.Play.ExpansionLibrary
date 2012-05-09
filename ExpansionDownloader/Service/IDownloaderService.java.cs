namespace ExpansionDownloader.Service
{
    using Android.OS;

    using ExpansionDownloader.Client;

    /// <summary>
    /// Represents a service that will perform downloads.
    /// </summary>
    public interface IDownloaderService
    {
        #region Public Methods and Operators

        /// <summary>
        /// Call this when you get <see cref="IDownloaderClient.OnServiceConnected"/> 
        /// from the downloader client to register it with the service. 
        /// It will automatically send the current status to the client.
        /// </summary>
        /// <param name="clientMessenger">
        /// The client Messenger.
        /// </param>
        void OnClientUpdated(Messenger clientMessenger);

        /// <summary>
        /// Request that the service abort the current download. The service 
        /// should respond by changing the state to 
        /// <see cref="DownloaderClientState.FailedCanceled"/>.
        /// </summary>
        void RequestAbortDownload();

        /// <summary>
        /// Request that the service continue a paused download, when in any
        /// paused or failed state, including
        /// <see cref="DownloaderClientState.PausedByRequest"/>.
        /// </summary>
        void RequestContinueDownload();

        /// <summary>
        /// Requests that the download status be sent to the client.
        /// </summary>
        void RequestDownloadStatus();

        /// <summary>
        /// Request that the service pause the current download. The service
        /// should respond by changing the state to 
        /// <see cref="DownloaderClientState.PausedByRequest"/>.
        /// </summary>
        void RequestPauseDownload();

        /// <summary>
        /// Set the flags for this download (e.g. 
        /// <see cref="DownloaderServiceFlags.FlagsDownloadOverCellular"/>).
        /// </summary>
        /// <param name="flags">
        /// The new flags to use.
        /// </param>
        void SetDownloadFlags(DownloaderServiceFlags flags);

        #endregion
    }
}