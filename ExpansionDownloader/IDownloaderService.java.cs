using Android.OS;

namespace ExpansionDownloader
{
    public interface IDownloaderService
    {
        /**
     * Request that the service abort the current download. The service should
     * respond by changing the state to {@link IDownloaderClient.STATE_ABORTED}.
     */
        void requestAbortDownload();

        /**
     * Request that the service pause the current download. The service should
     * respond by changing the state to
     * {@link IDownloaderClient.STATE_PAUSED_BY_REQUEST}.
     */
        void requestPauseDownload();

        /**
     * Request that the service continue a paused download, when in any paused
     * or failed state, including
     * {@link IDownloaderClient.STATE_PAUSED_BY_REQUEST}.
     */
        void requestContinueDownload();

        /**
     * Set the flags for this download (e.g.
     * {@link DownloaderService.FLAGS_DOWNLOAD_OVER_CELLULAR}).
     * 
     * @param flags
     */
        void setDownloadFlags(int flags);

        /**
     * Requests that the download status be sent to the client.
     */
        void requestDownloadStatus();

        /**
     * Call this when you get {@link
     * IDownloaderClient.onServiceConnected(Messenger m)} from the
     * DownloaderClient to register the client with the service. It will
     * automatically send the current status to the client.
     * 
     * @param clientMessenger
     */
        void onClientUpdated(Messenger clientMessenger);
    }

    public static class IDownloaderServiceConsts
    {
        /**
     * Set this flag in response to the
     * IDownloaderClient.STATE_PAUSED_NEED_CELLULAR_PERMISSION state and then
     * call RequestContinueDownload to resume a download
     */
        public static int FLAGS_DOWNLOAD_OVER_CELLULAR = 1;
    }
}