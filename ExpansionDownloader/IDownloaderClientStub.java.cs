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
    /// <see cref="DownloaderClientMarshaller.CreateStub" /> in your activity 
    /// when the downloader service starts. 
    /// Then, call <see cref="Connect"/> during your activity's
    /// <see cref="Android.App.Activity.OnResume"/> and call 
    /// <see cref="IDownloaderServiceConnection.Disconnect"/> during onStop().
    /// Then during the <see cref="IDownloaderClient.OnServiceConnected"/> 
    /// callback, you should call <see cref="IDownloaderServiceConnection.GetMessenger"/> to pass the stub's
    /// Messenger object to <see cref="IDownloaderService.OnClientUpdated"/>.
    /// </remarks>
    public interface IDownloaderServiceConnection
    {
        Messenger GetMessenger();
        void Connect(Context context);
        void Disconnect(Context context);
    }
}