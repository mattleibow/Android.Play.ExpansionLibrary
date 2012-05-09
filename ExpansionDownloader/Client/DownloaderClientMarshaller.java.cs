namespace ExpansionDownloader.Client
{
    using System;

    using Android.App;
    using Android.Content;
    using Android.OS;

    using ExpansionDownloader.impl;

    using Java.Lang;

    /// <summary>
    /// This class binds the service API to your application client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It contains the <see cref="IDownloaderClient"/> proxy, which is used to 
    /// call functions in your client as well as the Stub, which is used to 
    /// call functions in the client implementation of 
    /// <see cref="IDownloaderClient"/>.
    /// </para>
    /// <para>
    /// The IPC is implemented using an Android Messenger and a service Binder.
    /// The connect method should be called whenever the client wants to bind 
    /// to the service.  
    /// It opens up a service connection that ends up calling the 
    /// <see cref="IDownloaderClient.OnServiceConnected"/> client API that 
    /// passes the service messenger in.
    /// If the client wants to be notified by the service, it is responsible 
    /// for then passing its messenger to the service in a separate call.
    /// </para>
    /// <para>
    /// Critical methods are 
    /// <see cref="StartDownloadServiceIfRequired(Android.Content.Context,Android.Content.Intent,System.Type)"/> 
    /// and <see cref="CreateStub"/>.
    /// </para>
    /// <para>
    /// When your application first starts, you should first check whether your
    /// app's expansion files are already on the device. If not, you should 
    /// then call 
    /// <see cref="StartDownloadServiceIfRequired(Android.Content.Context,Android.Content.Intent,System.Type)"/>,
    /// which starts your <see cref="DownloaderService"/> to download the 
    /// expansion files if necessary. 
    /// The method returns a value indicating whether download is required or 
    /// not.
    /// </para>
    /// <para>
    /// If a download is required, 
    /// <see cref="StartDownloadServiceIfRequired(Android.Content.Context,Android.Content.Intent,System.Type)"/> 
    /// begins the download through the specified service and you should then 
    /// call <see cref="CreateStub"/> to instantiate a member
    /// <see cref="IDownloaderServiceConnection"/> object that you need in 
    /// order to receive calls through your <see cref="IDownloaderClient"/> 
    /// interface.
    /// </para>
    /// </remarks>
    public static class DownloaderClientMarshaller
    {
        #region Public Methods and Operators

        /// <summary>
        /// Returns a proxy that will marshal calls to IDownloaderClient 
        /// methods.
        /// </summary>
        /// <param name="msg">
        /// The messenger.
        /// </param>
        /// <returns>
        /// A proxy that will marshal calls to IDownloaderClient methods.
        /// </returns>
        public static IDownloaderClient CreateProxy(Messenger msg)
        {
            return new Proxy(msg);
        }

        /// <summary>
        /// Returns a stub object that, when connected, will listen for 
        /// marshaled <see cref="IDownloaderClient"/> methods and translate 
        /// them into calls to the supplied interface.
        /// </summary>
        /// <param name="itf">
        /// An implementation of IDownloaderClient that will be called when 
        /// remote method calls are unmarshaled.
        /// </param>
        /// <param name="downloaderService">
        /// The class for your implementation of<see cref="DownloaderService"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IDownloaderServiceConnection"/> that allows you to connect to the service 
        /// such that your <see cref="IDownloaderClient"/> receives status updates.
        /// </returns>
        public static IDownloaderServiceConnection CreateStub(IDownloaderClient itf, Type downloaderService)
        {
            return new DownloaderServiceConnection(itf, downloaderService);
        }

        /// <summary>
        /// Starts the download if necessary. 
        /// </summary>
        /// <remarks>
        /// This function starts a flow that 
        /// does many things:
        ///   1) Checks to see if the APK version has been checked and the 
        ///      metadata database updated 
        ///   2) If the APK version does not match, checks the new LVL status 
        ///      to see if a new download is required 
        ///   3) If the APK version does match, then checks to see if the 
        ///      download(s) have been completed
        ///   4) If the downloads have been completed, returns 
        ///      <see cref="DownloadServiceRequirement.NoDownloadRequired"/> 
        /// The idea is that this can be called during the startup of an 
        /// application to quickly ascertain if the application needs to wait 
        /// to hear about any updated APK expansion files. 
        /// This does mean that the application MUST be run with a network 
        /// connection for the first time, even if Market delivers all of the 
        /// files.
        /// </remarks>
        /// <param name="context">
        /// Your application Context.
        /// </param>
        /// <param name="notificationClient">
        /// A PendingIntent to start the Activity in your application that
        /// shows the download progress and which will also start the 
        /// application when downloadcompletes.
        /// </param>
        /// <param name="serviceType">
        /// The class of your <see cref="DownloaderService"/> implementation.
        /// </param>
        /// <returns>
        /// Whether the service was started and the reason for starting the 
        /// service.
        /// Either <see cref="DownloadServiceRequirement.NoDownloadRequired"/>,
        /// <see cref="DownloadServiceRequirement.LvlCheckRequired"/>, or 
        /// <see cref="DownloadServiceRequirement.DownloadRequired"/>
        /// </returns>
        public static DownloadServiceRequirement StartDownloadServiceIfRequired(
            Context context, PendingIntent notificationClient, Type serviceType)
        {
            return DownloaderService.StartDownloadServiceIfRequired(context, notificationClient, serviceType);
        }

        /// <summary>
        /// This version assumes that the intent contains the pending intent as
        /// a parameter. This is used for responding to alarms.
        /// The pending intent must be in an extra with the key 
        /// <see cref="DownloaderService#PendingIntent"/>.
        /// </summary>
        /// <param name="context">
        /// Your application Context.
        /// </param>
        /// <param name="notificationClient">
        /// A PendingIntent to start the Activity in your application that
        /// shows the download progress and which will also start the 
        /// application when downloadcompletes.
        /// </param>
        /// <param name="serviceType">
        /// The type of the service to start.
        /// </param>
        /// <returns>
        /// Whether the service was started and the reason for starting the 
        /// service.
        /// Either <see cref="DownloadServiceRequirement.NoDownloadRequired"/>,
        /// <see cref="DownloadServiceRequirement.LvlCheckRequired"/>, or 
        /// <see cref="DownloadServiceRequirement.DownloadRequired"/>
        /// </returns>
        public static DownloadServiceRequirement StartDownloadServiceIfRequired(
            Context context, Intent notificationClient, Type serviceType)
        {
            return DownloaderService.StartDownloadServiceIfRequired(context, notificationClient, serviceType);
        }

        #endregion

        /// <summary>
        /// The downloader service connection.
        /// </summary>
        private class DownloaderServiceConnection : IDownloaderServiceConnection
        {
            #region Constants and Fields

            /// <summary>
            /// The client type.
            /// </summary>
            private readonly IDownloaderClient clientType;

            /// <summary>
            /// Target we publish for clients to send messages to 
            /// IncomingHandler.
            /// </summary>
            private readonly Messenger messenger;

            /// <summary>
            /// The service connection.
            /// </summary>
            private readonly IServiceConnection serviceConnection;

            /// <summary>
            /// The service type type.
            /// </summary>
            private readonly Type serviceTypeType;

            /// <summary>
            /// The class loader.
            /// </summary>
            private ClassLoader classLoader;

            /// <summary>
            /// The is bound.
            /// </summary>
            private bool isBound;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="DownloaderServiceConnection"/> class.
            /// </summary>
            /// <param name="clientType">
            /// The client type.
            /// </param>
            /// <param name="serviceType">
            /// The service type.
            /// </param>
            public DownloaderServiceConnection(IDownloaderClient clientType, Type serviceType)
            {
                this.messenger = new Messenger(new Handler(this.SendMessage));
                this.serviceConnection = new ServiceConnection(this);
                this.clientType = clientType;
                this.serviceTypeType = serviceType;
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The connect.
            /// </summary>
            /// <param name="context">
            /// The context.
            /// </param>
            public void Connect(Context context)
            {
                this.classLoader = context.ClassLoader;
                var bindIntent = new Intent(context, this.serviceTypeType);
                bindIntent.PutExtra(DownloaderClientMessageParameters.Messenger, this.messenger);
                var bound = context.BindService(bindIntent, this.serviceConnection, Bind.DebugUnbind);
                if (!bound)
                {
                    System.Diagnostics.Debug.WriteLine("LVLDL Service Unbound");
                }
            }

            /// <summary>
            /// The disconnect.
            /// </summary>
            /// <param name="context">
            /// The context.
            /// </param>
            public void Disconnect(Context context)
            {
                if (this.isBound)
                {
                    context.UnbindService(this.serviceConnection);
                    this.isBound = false;
                }

                this.classLoader = null;
            }

            /// <summary>
            /// Returns a messenger.
            /// </summary>
            /// <returns>
            /// The messenger
            /// </returns>
            public Messenger GetMessenger()
            {
                return this.messenger;
            }

            #endregion

            #region Methods

            /// <summary>
            /// The send message.
            /// </summary>
            /// <param name="msg">
            /// The msg.
            /// </param>
            private void SendMessage(Message msg)
            {
                switch ((DownloaderClientMessages)msg.What)
                {
                    case DownloaderClientMessages.DownloadProgress:
                        if (this.classLoader != null)
                        {
                            Bundle bun = msg.Data;
                            bun.SetClassLoader(this.classLoader);
                            var progress = msg.Data.GetString(DownloaderClientMessageParameters.Progress);
                            var info = new DownloadProgressInfo(progress);
                            this.clientType.OnDownloadProgress(info);
                        }

                        break;
                    case DownloaderClientMessages.DownloadStateChanged:
                        var state = (DownloaderClientState)msg.Data.GetInt(DownloaderClientMessageParameters.NewState);
                        this.clientType.OnDownloadStateChanged(state);
                        break;
                    case DownloaderClientMessages.ServiceConnected:
                        var m = (Messenger)msg.Data.GetParcelable(DownloaderClientMessageParameters.Messenger);
                        this.clientType.OnServiceConnected(m);
                        break;
                }
            }

            #endregion

            /// <summary>
            /// Class for interacting with the main interface of the service.
            /// </summary>
            private class ServiceConnection : Java.Lang.Object, IServiceConnection
            {
                #region Constants and Fields

                /// <summary>
                /// The connection.
                /// </summary>
                private readonly DownloaderServiceConnection connection;

                #endregion

                #region Constructors and Destructors

                /// <summary>
                /// Initializes a new instance of the <see cref="ServiceConnection"/> class.
                /// </summary>
                /// <param name="connection">
                /// The _downloader service connection.
                /// </param>
                public ServiceConnection(DownloaderServiceConnection connection)
                {
                    this.connection = connection;
                }

                #endregion

                #region Public Methods and Operators

                /// <summary>
                /// This is called when the connection with the service has 
                /// been established, giving us the object we can use to
                /// interact with the service. 
                /// We are communicating with the service using a Messenger, 
                /// so here we get a client-side representation of that from 
                /// the raw IBinder object.
                /// </summary>
                /// <param name="className">
                /// </param>
                /// <param name="service">
                /// </param>
                public void OnServiceConnected(ComponentName className, IBinder service)
                {
                    this.connection.clientType.OnServiceConnected(new Messenger(service));
                    this.connection.isBound = true;
                }

                /// <summary>
                /// This is called when the connection with the service has 
                /// been unexpectedly disconnected (its process crashed).
                /// </summary>
                /// <param name="className">
                /// </param>
                public void OnServiceDisconnected(ComponentName className)
                {
                    this.connection.isBound = false;
                }

                #endregion
            }
        }

        /// <summary>
        /// The proxy.
        /// </summary>
        private class Proxy : IDownloaderClient
        {
            #region Constants and Fields

            /// <summary>
            /// The service messenger.
            /// </summary>
            private readonly Messenger serviceMessenger;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="Proxy"/> class.
            /// </summary>
            /// <param name="messenger">
            /// The messenger.
            /// </param>
            public Proxy(Messenger messenger)
            {
                this.serviceMessenger = messenger;
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The on download progress.
            /// </summary>
            /// <param name="progress">
            /// The progress.
            /// </param>
            public void OnDownloadProgress(DownloadProgressInfo progress)
            {
                var p = new Bundle(1);
                p.PutString(DownloaderClientMessageParameters.Progress, progress.ToString());
                this.SendMessage(DownloaderClientMessages.DownloadProgress, p);
            }

            /// <summary>
            /// The on download state changed.
            /// </summary>
            /// <param name="newState">
            /// The new state.
            /// </param>
            public void OnDownloadStateChanged(DownloaderClientState newState)
            {
                var p = new Bundle(1);
                p.PutInt(DownloaderClientMessageParameters.NewState, (int)newState);
                this.SendMessage(DownloaderClientMessages.DownloadStateChanged, p);
            }

            /// <summary>
            /// The on service connected.
            /// </summary>
            /// <param name="m">
            /// The m.
            /// </param>
            public void OnServiceConnected(Messenger m)
            {
                // This is never called through the proxy.
            }

            #endregion

            #region Methods

            /// <summary>
            /// The send message.
            /// </summary>
            /// <param name="clientMessage">
            /// The client message.
            /// </param>
            /// <param name="data">
            /// The data.
            /// </param>
            private void SendMessage(DownloaderClientMessages clientMessage, Bundle data)
            {
                Message m = Message.Obtain(null, (int)clientMessage);
                m.Data = data;
                try
                {
                    this.serviceMessenger.Send(m);
                }
                catch (RemoteException e)
                {
                    e.PrintStackTrace();
                }
            }

            #endregion
        }
    }
}