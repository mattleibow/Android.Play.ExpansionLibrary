using System;
using Android.App;
using Android.Content;
using Android.OS;
using ExpansionDownloader.impl;
using Object = Java.Lang.Object;

namespace ExpansionDownloader
{
    using ExpansionDownloader.Client;

    using Java.Lang;

    /**
 * This class binds the service API to your application client.  It contains the IDownloaderClient proxy,
 * which is used to call functions in your client as well as the Stub, which is used to call functions
 * in the client implementation of IDownloaderClient.
 * 
 * <p>The IPC is implemented using an Android Messenger and a service Binder.  The connect method
 * should be called whenever the client wants to bind to the service.  It opens up a service connection
 * that ends up calling the onServiceConnected client API that passes the service messenger
 * in.  If the client wants to be notified by the service, it is responsible for then passing its
 * messenger to the service in a separate call.
 *
 * <p>Critical methods are {@link #startDownloadServiceIfRequired} and {@link #CreateStub}.
 *
 * <p>When your application first starts, you should first check whether your app's expansion files are
 * already on the device. If not, you should then call {@link #startDownloadServiceIfRequired}, which
 * starts your {@link impl.DownloaderService} to download the expansion files if necessary. The method
 * returns a value indicating whether download is required or not.
 *
 * <p>If a download is required, {@link #startDownloadServiceIfRequired} begins the download through
 * the specified service and you should then call {@link #CreateStub} to instantiate a member {@link
 * IStub} object that you need in order to receive calls through your {@link IDownloaderClient}
 * interface.
 */
    public class DownloaderClientMarshaller
    {
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
        /// The class of your <see cref="DownloaderService" /> implementation.
        /// </param>
        /// <returns>
        /// Whether the service was started and the reason for starting the 
        /// service.
        /// Either <see cref="DownloadServiceRequirement.NoDownloadRequired" />,
        /// <see cref="DownloadServiceRequirement.LvlCheckRequired" />, or 
        /// <see cref="DownloadServiceRequirement.DownloadRequired" />
        /// </returns>
        public static DownloadServiceRequirement StartDownloadServiceIfRequired(Context context, PendingIntent notificationClient, Type serviceType)
        {
            return DownloaderService.StartDownloadServiceIfRequired(context, notificationClient, serviceType);
        }

        /// <summary>
        /// This version assumes that the intent contains the pending intent as
        /// a parameter. This is used for responding to alarms.
        /// The pending intent must be in an extra with the key 
        /// <see cref="DownloaderService#PendingIntent" />.
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
        /// Either <see cref="DownloadServiceRequirement.NoDownloadRequired" />,
        /// <see cref="DownloadServiceRequirement.LvlCheckRequired" />, or 
        /// <see cref="DownloadServiceRequirement.DownloadRequired" />
        /// </returns>
        public static DownloadServiceRequirement StartDownloadServiceIfRequired(Context context, Intent notificationClient, Type serviceType)
        {
            return DownloaderService.StartDownloadServiceIfRequired(context, notificationClient, serviceType);
        }

        #region Nested type: Proxy

        private class Proxy : IDownloaderClient
        {
            private readonly Messenger serviceMessenger;

            public Proxy(Messenger messenger)
            {
                this.serviceMessenger = messenger;
            }

            #region IDownloaderClient Members

            public void OnDownloadStateChanged(DownloaderClientState newState)
            {
                var p = new Bundle(1);
                p.PutInt(DownloaderClientMessageParameters.NewState, (int)newState);
                this.SendMessage(DownloaderClientMessages.DownloadStateChanged, p);
            }

            public void OnDownloadProgress(DownloadProgressInfo progress)
            {
                var p = new Bundle(1);
                p.PutString(DownloaderClientMessageParameters.Progress, progress.ToString());
                this.SendMessage(DownloaderClientMessages.DownloadProgress, p);
            }

            public void OnServiceConnected(Messenger m)
            {
                // This is never called through the proxy.
            }

            #endregion

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
        }

        #endregion

        #region Nested type: DownloaderServiceConnection

        private class DownloaderServiceConnection : IDownloaderServiceConnection
        {
            private readonly IServiceConnection serviceConnection;
            private readonly Type serviceTypeType;
            private readonly IDownloaderClient clientType;

            /// <summary>
            /// Target we publish for clients to send messages to 
            /// IncomingHandler.
            /// </summary>
            private readonly Messenger messenger;
            private bool isBound;
            private ClassLoader classLoader;

            public DownloaderServiceConnection(IDownloaderClient clientType, Type serviceType)
            {
                this.messenger = new Messenger(new Handler(this.SendMessage));
                this.serviceConnection = new ServiceConnection(this);
                this.clientType = clientType;
                this.serviceTypeType = serviceType;
            }

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
            
            #region IDownloaderServiceConnection Members

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

            public void Disconnect(Context context)
            {
                if (this.isBound)
                {
                    context.UnbindService(this.serviceConnection);
                    this.isBound = false;
                }

                this.classLoader = null;
            }

            public Messenger GetMessenger()
            {
                return this.messenger;
            }

            #endregion

            #region Nested type: ServiceConnection

            /// <summary>
            /// Class for interacting with the main interface of the service.
            /// </summary>
            private class ServiceConnection : Object, IServiceConnection
            {
                private readonly DownloaderServiceConnection _downloaderServiceConnection;

                public ServiceConnection(DownloaderServiceConnection _downloaderServiceConnection)
                {
                    this._downloaderServiceConnection = _downloaderServiceConnection;
                }

                #region IServiceConnection Members

                /// <summary>
                /// This is called when the connection with the service has 
                /// been established, giving us the object we can use to
                /// interact with the service. 
                /// We are communicating with the service using a Messenger, 
                /// so here we get a client-side representation of that from 
                /// the raw IBinder object.
                /// </summary>
                /// <param name="className"></param>
                /// <param name="service"></param>
                public void OnServiceConnected(ComponentName className, IBinder service)
                {
                    this._downloaderServiceConnection.clientType.OnServiceConnected(new Messenger(service));
                    this._downloaderServiceConnection.isBound = true;
                }

                /// <summary>
                /// This is called when the connection with the service has 
                /// been unexpectedly disconnected (its process crashed).
                /// </summary>
                /// <param name="className"></param>
                public void OnServiceDisconnected(ComponentName className)
                {
                    this._downloaderServiceConnection.isBound = false;
                }

                #endregion
            }

            #endregion
        }

        #endregion
    }
}