namespace ExpansionDownloader.Service
{
    using Android.Content;
    using Android.OS;

    using ExpansionDownloader.Client;
    using ExpansionDownloader.impl;

    /// <summary>
    /// Used by the client activity to proxy requests to the DownloaderService.
    /// </summary>
    /// <remarks>
    /// Most importantly, you must call <see cref="CreateProxy"/> during the 
    /// <see cref="IDownloaderClient.OnServiceConnected"/> callback in your 
    /// activity in order to instantiate an <see cref="IDownloaderService"/>
    /// object that you can then use to issue commands to the
    /// <see cref="DownloaderService"/> (such as to pause and resume downloads).
    /// </remarks>
    public class DownloaderServiceMarshaller
    {
        /// <summary>
        /// Returns a proxy that will marshall calls to IDownloaderService methods
        /// </summary>
        /// <param name="messenger">
        /// The messenger.
        /// </param>
        /// <returns>
        /// A proxy that will marshall calls to IDownloaderService methods
        /// </returns>
        public static IDownloaderService CreateProxy(Messenger messenger)
        {
            return new Proxy(messenger);
        }

        /// <summary>
        /// Returns a stub object that, when connected, will listen for 
        /// marshalled IDownloaderService methods and translate them into calls 
        /// to the supplied interface.
        /// </summary>
        /// <param name="itf">
        /// An implementation of IDownloaderService that will be called when
        /// remote method calls are unmarshalled.
        /// </param>
        /// <returns>
        /// A stub that will listen for marshalled IDownloaderService methods.
        /// </returns>
        public static IDownloaderServiceConnection CreateStub(IDownloaderService itf)
        {
            return new DownloaderServiceConnection(itf);
        }

        #region Nested type: Proxy

        private class Proxy : IDownloaderService
        {
            private readonly Messenger messenger;

            public Proxy(Messenger msg)
            {
                this.messenger = msg;
            }

            #region IDownloaderService Members

            public void RequestAbortDownload()
            {
                this.Send(DownloaderServiceMessages.RequestAbortDownload, new Bundle());
            }

            public void RequestPauseDownload()
            {
                this.Send(DownloaderServiceMessages.RequestPauseDownload, new Bundle());
            }

            public void SetDownloadFlags(DownloaderServiceFlags flags)
            {
                var p = new Bundle();
                p.PutInt(DownloaderServiceParameters.Flags, (int)flags);
                this.Send(DownloaderServiceMessages.SetDownloadFlags, p);
            }

            public void RequestContinueDownload()
            {
                this.Send(DownloaderServiceMessages.RequestContinueDownload, new Bundle());
            }

            public void RequestDownloadStatus()
            {
                this.Send(DownloaderServiceMessages.RequestDownloadState, new Bundle());
            }

            public void OnClientUpdated(Messenger clientMessenger)
            {
                var bundle = new Bundle(1);
                bundle.PutParcelable(DownloaderServiceParameters.Messenger, clientMessenger);
                this.Send(DownloaderServiceMessages.RequestClientUpdate, bundle);
            }

            #endregion

            private void Send(DownloaderServiceMessages method, Bundle p)
            {
                Message m = Message.Obtain(null, (int)method);
                m.Data = p;
                try
                {
                    this.messenger.Send(m);
                }
                catch (RemoteException e)
                {
                    e.PrintStackTrace();
                }
            }
        }

        #endregion

        #region Nested type: DownloaderServiceConnection

        private class DownloaderServiceConnection : IDownloaderServiceConnection
        {
            private readonly IDownloaderService downloaderService;

            private readonly Messenger messenger;

            public DownloaderServiceConnection(IDownloaderService downloaderService)
            {
                var handler = new Handler(this.SendMessage);
                this.messenger = new Messenger(handler);
                this.downloaderService = downloaderService;
            }

            private void SendMessage(Message message)
            {
                switch ((DownloaderServiceMessages)message.What)
                {
                    case DownloaderServiceMessages.RequestAbortDownload:
                        this.downloaderService.RequestAbortDownload();
                        break;
                    case DownloaderServiceMessages.RequestContinueDownload:
                        this.downloaderService.RequestContinueDownload();
                        break;
                    case DownloaderServiceMessages.RequestPauseDownload:
                        this.downloaderService.RequestPauseDownload();
                        break;
                    case DownloaderServiceMessages.SetDownloadFlags:
                        var flags = (DownloaderServiceFlags)message.Data.GetInt(DownloaderServiceParameters.Flags);
                        this.downloaderService.SetDownloadFlags(flags);
                        break;
                    case DownloaderServiceMessages.RequestDownloadState:
                        this.downloaderService.RequestDownloadStatus();
                        break;
                    case DownloaderServiceMessages.RequestClientUpdate:
                        var m = (Messenger)message.Data.GetParcelable(DownloaderServiceParameters.Messenger);
                        this.downloaderService.OnClientUpdated(m);
                        break;
                }
            }

            #region IDownloaderServiceConnection Members

            public Messenger GetMessenger()
            {
                return this.messenger;
            }

            public void Connect(Context context)
            {
            }

            public void Disconnect(Context context)
            {
            }

            #endregion
        }

        #endregion
    }
}