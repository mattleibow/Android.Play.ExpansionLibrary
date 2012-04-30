using Android.OS;
using ExpansionDownloader.impl;

namespace ExpansionDownloader
{
    public interface IDownloaderService
    {
        /// <summary>
        /// Request that the service abort the current download. The service 
        /// should respond by changing the state to IDownloaderService.STATE_ABORTED.
        /// </summary>
        void RequestAbortDownload();

        /// <summary>
        /// Request that the service pause the current download. The service
        /// should respond by changing the state to 
        /// <see cref="DownloaderClientState.PausedByRequest"/>.
        /// </summary>
        void RequestPauseDownload();

        /// <summary>
        /// Request that the service continue a paused download, when in any
        /// paused or failed state, including
        /// <see cref="DownloaderClientState.PausedByRequest"/>.
        /// </summary>
        void RequestContinueDownload();

        /// <summary>
        /// Set the flags for this download (e.g. 
        /// <see cref="DownloaderServiceFlags.FlagsDownloadOverCellular"/>).
        /// </summary>
        void SetDownloadFlags(DownloaderServiceFlags flags);

        /// <summary>
        /// Requests that the download status be sent to the client.
        /// </summary>
        void RequestDownloadStatus();

        /// <summary>
        /// Call this when you get {@link
        /// IDownloaderClient.onServiceConnected(Messenger m)} from the
        /// DownloaderClient to register the client with the service. It will
        /// automatically send the current status to the client.
        /// </summary>
        void OnClientUpdated(Messenger clientMessenger);
    }
}
