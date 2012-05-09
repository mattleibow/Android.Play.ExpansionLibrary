namespace ExpansionDownloader.Client
{
    using Android.OS;

    using ExpansionDownloader.Service;

    /// <summary>
    /// This interface should be implemented by the client activity for the
    /// downloader. It is used to pass status from the service to the client.
    /// </summary>
    public interface IDownloaderClient
    {
        #region Public Methods and Operators

        /// <summary>
        /// Shows the download progress. This is intended to be used to fill 
        /// out a client UI. 
        /// This progress should only be shown in a few states such as
        /// <see cref="DownloaderClientState.Downloading"/>.
        /// </summary>
        /// <param name="progress">
        /// the DownloadProgressInfo object containing the current progress of 
        /// all downloads.
        /// </param>
        void OnDownloadProgress(DownloadProgressInfo progress);

        /// <summary>
        /// <para>
        /// Called when the download state changes. Depending on the state, 
        /// there may be user requests. The service is free to change the 
        /// download state in the middle of a user request, so the client 
        /// should be able to handle this.
        /// </para>
        /// <para>
        /// The Downloader Library includes a collection of string resources 
        /// that correspond to each of the states, which you can use to provide 
        /// users a useful message based on the state provided in this callback.
        /// To fetch the appropriate string for a state, call 
        /// <see cref="Helpers.GetDownloaderStringFromState"/>.
        /// </para>
        /// <para>
        /// What this means to the developer: 
        /// The application has gotten a message that the download has paused 
        /// due to lack of WiFi. 
        /// The developer should then show UI asking the user if they want to 
        /// enable downloading over cellular connections with appropriate 
        /// warnings. If the application suddenly starts downloading, the 
        /// application should revert to showing the progress again, rather 
        /// than leaving up the download over cellular UI up.
        /// </para>
        /// </summary>
        /// <param name="newState">
        /// The new state of the current download.
        /// </param>
        void OnDownloadStateChanged(DownloaderClientState newState);

        /// <summary>
        /// <para>
        /// Called internally by the stub when the service is bound to the 
        /// client.
        /// </para>
        /// <para>
        /// Critical implementation detail. In onServiceConnected we create the
        /// remote service and marshaler. This is how we pass the client 
        /// information back to the service so the client can be properly 
        /// notified of changes. 
        /// This must be done every time we reconnect to the service.
        /// </para>
        /// <para>
        /// That is, when you receive this callback, you should call
        /// <see cref="DownloaderServiceMarshaller.CreateProxy"/> to 
        /// instantiate a member instance of <see cref="IDownloaderService"/>, 
        /// then call <see cref="IDownloaderService.OnClientUpdated"/> with the 
        /// Messenger retrieved from your 
        /// <see cref="IDownloaderServiceConnection"/> proxy object.
        /// </para>
        /// </summary>
        /// <param name="m">
        /// the service Messenger. This Messenger is used to call the service 
        /// API from the client.
        /// </param>
        void OnServiceConnected(Messenger m);

        #endregion
    }
}