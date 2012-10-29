// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientMarshaller.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   This class binds the service API to your application client.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader.Client
{
    using System;

    using Android.Content;
    using Android.OS;

    using ExpansionDownloader.Core;
    using ExpansionDownloader.Core.Client;

    using Java.Lang;

    using Debug = System.Diagnostics.Debug;
    using Object = Java.Lang.Object;

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
    /// <see cref="DownloaderService.StartDownloadServiceIfRequired(Android.Content.Context,Android.Content.Intent,System.Type)"/> 
    /// and <see cref="CreateStub"/>.
    /// </para>
    /// <para>
    /// When your application first starts, you should first check whether your
    /// app's expansion files are already on the device. If not, you should 
    /// then call 
    /// <see cref="DownloaderService.StartDownloadServiceIfRequired(Android.Content.Context,Android.Content.Intent,System.Type)"/>,
    /// which starts your <see cref="DownloaderService"/> to download the 
    /// expansion files if necessary. 
    /// The method returns a value indicating whether download is required or 
    /// not.
    /// </para>
    /// <para>
    /// If a download is required, 
    /// <see cref="DownloaderService.StartDownloadServiceIfRequired(Android.Content.Context,Android.Content.Intent,System.Type)"/> 
    /// begins the download through the specified service and you should then 
    /// call <see cref="CreateStub"/> to instantiate a member
    /// <see cref="IDownloaderServiceConnection"/> object that you need in 
    /// order to receive calls through your <see cref="IDownloaderClient"/> 
    /// interface.
    /// </para>
    /// </remarks>
    public static class ClientMarshaller
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
        /// The class for your implementation of<see cref="ExpansionDownloader.Service.DownloaderService"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IDownloaderServiceConnection"/> that allows you to connect to the service 
        /// such that your <see cref="IDownloaderClient"/> receives status updates.
        /// </returns>
        public static IDownloaderServiceConnection CreateStub(IDownloaderClient itf, Type downloaderService)
        {
            return new DownloaderServiceConnection(itf, downloaderService);
        }

        #endregion

        /// <summary>
        /// The downloader service connection.
        /// </summary>
        private class DownloaderServiceConnection : IDownloaderServiceConnection
        {
            #region Fields

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
                bindIntent.PutExtra(ClientMessageParameters.Messenger, this.messenger);
                bool bound = context.BindService(bindIntent, this.serviceConnection, Bind.DebugUnbind);
                if (!bound)
                {
                    Debug.WriteLine("LVLDL Service Unbound");
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
                switch ((ClientMessages)msg.What)
                {
                    case ClientMessages.DownloadProgress:
                        if (this.classLoader != null)
                        {
                            Bundle bun = msg.Data;
                            bun.SetClassLoader(this.classLoader);
                            string progress = msg.Data.GetString(ClientMessageParameters.Progress);
                            var info = new DownloadProgressInfo(progress);
                            this.clientType.OnDownloadProgress(info);
                        }

                        break;
                    case ClientMessages.DownloadStateChanged:
                        var state = (DownloaderState)msg.Data.GetInt(ClientMessageParameters.NewState);
                        this.clientType.OnDownloadStateChanged(state);
                        break;
                    case ClientMessages.ServiceConnected:
                        var m = (Messenger)msg.Data.GetParcelable(ClientMessageParameters.Messenger);
                        this.clientType.OnServiceConnected(m);
                        break;
                }
            }

            #endregion

            /// <summary>
            /// Class for interacting with the main interface of the service.
            /// </summary>
            private class ServiceConnection : Object, IServiceConnection
            {
                #region Fields

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
            #region Fields

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
                using (var p = new Bundle(1))
                {
                    p.PutString(ClientMessageParameters.Progress, progress.ToString());
                    this.SendMessage(ClientMessages.DownloadProgress, p);
                }
            }

            /// <summary>
            /// The on download state changed.
            /// </summary>
            /// <param name="newState">
            /// The new state.
            /// </param>
            public void OnDownloadStateChanged(DownloaderState newState)
            {
                using (var p = new Bundle(1))
                {
                    p.PutInt(ClientMessageParameters.NewState, (int)newState);
                    this.SendMessage(ClientMessages.DownloadStateChanged, p);
                }
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
            private void SendMessage(ClientMessages clientMessage, Bundle data)
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