using Android.OS;

namespace ExpansionDownloader
{
    public interface IDownloaderClient
    {
        /**
     * Called internally by the stub when the service is bound to the client.
     * <p>
     * Critical implementation detail. In onServiceConnected we create the
     * remote service and marshaler. This is how we pass the client information
     * back to the service so the client can be properly notified of changes. We
     * must do this every time we reconnect to the service.
     * <p>That is, when you receive this callback, you should call
     * {@link DownloaderServiceMarshaller#CreateProxy} to instantiate a member
     * instance of {@link IDownloaderService}, then call {@link
     * IDownloaderService#OnClientUpdated} with the Messenger retrieved from your
     * {@link IStub} proxy object.
     * 
     * @param m the service Messenger. This Messenger is used to call the
     *            service API from the client.
     */
        void OnServiceConnected(Messenger m);

        /**
     * Called when the download state changes. Depending on the state, there may
     * be user requests. The service is free to change the download state in the
     * middle of a user request, so the client should be able to handle this.
     * <p>The Downloader Library includes a collection of string resources that correspond
     * to each of the states, which you can use to provide users a useful message based
     * on the state provided in this callback. To fetch the appropriate string for a state,
     * call {@link Helpers#getDownloaderStringResourceIDFromState}.
     * <p>
     * What this means to the developer: The application has gotten a message
     * that the download has paused due to lack of WiFi. The developer should
     * then show UI asking the user if they want to enable downloading over
     * cellular connections with appropriate warnings. If the application
     * suddenly starts downloading, the application should revert to showing the
     * progress again, rather than leaving up the download over cellular UI up.
     * 
     * @param newState one of the STATE_* values defined in DownloaderClientState
     */
        void OnDownloadStateChanged(DownloaderClientState newState);

        /**
     * Shows the download progress. This is intended to be used to fill out a
     * client UI. This progress should only be shown in a few states such as
     * Downloading.
     * 
     * @param progress the DownloadProgressInfo object containing the current
     *            progress of all downloads.
     */
        void OnDownloadProgress(DownloadProgressInfo progress);
    }
}