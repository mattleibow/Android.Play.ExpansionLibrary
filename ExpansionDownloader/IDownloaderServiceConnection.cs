namespace ExpansionDownloader
{
    using Android.Content;
    using Android.OS;

    using ExpansionDownloader.Client;
    using ExpansionDownloader.Service;

    /// <summary>
    /// This is the interface that is used to connect/disconnect from the 
    /// downloader service.
    /// </summary>
    /// <remarks>
    /// You should get a proxy object that implements this interface by calling
    /// <see cref="ClientMarshaller.CreateStub"/> in your activity 
    /// when the downloader service starts. 
    /// Then, call <see cref="Connect"/> during your activity's
    /// <see cref="Android.App.Activity.OnResume"/> and call 
    /// <see cref="IDownloaderServiceConnection.Disconnect"/> during onStop().
    /// Then during the <see cref="IDownloaderClient.OnServiceConnected"/> 
    /// callback, you should call 
    /// <see cref="IDownloaderServiceConnection.GetMessenger"/> to pass the 
    /// stub's Messenger object to <see cref="IDownloaderService.OnClientUpdated"/>.
    /// </remarks>
    public interface IDownloaderServiceConnection
    {
        #region Public Methods and Operators

        /// <summary>
        /// The connect.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        void Connect(Context context);

        /// <summary>
        /// The disconnect.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        void Disconnect(Context context);

        /// <summary>
        /// The get messenger.
        /// </summary>
        /// <returns>
        /// </returns>
        Messenger GetMessenger();

        #endregion
    }
}