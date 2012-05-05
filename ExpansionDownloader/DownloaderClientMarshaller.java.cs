using System;
using Android.App;
using Android.Content;
using Android.OS;
using ExpansionDownloader.impl;
using Object = Java.Lang.Object;

namespace ExpansionDownloader
{
    public class DownloaderClientMarshaller
    {
        public const int MSG_ONDOWNLOADSTATE_CHANGED = 10;
        public const int MSG_ONDOWNLOADPROGRESS = 11;
        public const int MSG_ONSERVICECONNECTED = 12;

        public const string PARAM_NEW_STATE = "newState";
        public const string PARAM_PROGRESS = "progress";
        public const string PARAM_MESSENGER = DownloaderServiceExtras.MessageHandler;

        /**
     * Returns a proxy that will marshal calls to IDownloaderClient methods
     * 
     * @param msg
     * @return
     */

        public static IDownloaderClient CreateProxy(Messenger msg)
        {
            return new Proxy(msg);
        }

        /**
     * Returns a stub object that, when connected, will listen for marshaled
     * {@link IDownloaderClient} methods and translate them into calls to the supplied
     * interface.
     * 
     * @param itf An implementation of IDownloaderClient that will be called
     *            when remote method calls are unmarshaled.
     * @param downloaderService The class for your implementation of {@link
     * impl.DownloaderService}.
     * @return The {@link IStub} that allows you to connect to the service such that
     * your {@link IDownloaderClient} receives status updates.
     */

        public static IStub CreateStub(IDownloaderClient itf, Type downloaderService)
        {
            return new Stub(itf, downloaderService);
        }

        /**
     * Starts the download if necessary. This function starts a flow that does `
     * many things. 1) Checks to see if the APK version has been checked and
     * the metadata database updated 2) If the APK version does not match,
     * checks the new LVL status to see if a new download is required 3) If the
     * APK version does match, then checks to see if the download(s) have been
     * completed 4) If the downloads have been completed, returns
     * DownloadServiceRequirement.NoDownloadRequired The idea is that this can be called during the
     * startup of an application to quickly ascertain if the application needs
     * to wait to hear about any updated APK expansion files. Note that this does
     * mean that the application MUST be run for the first time with a network
     * connection, even if Market delivers all of the files.
     * 
     * @param context Your application Context.
     * @param notificationClient A PendingIntent to start the Activity in your application
     * that shows the download progress and which will also start the application when download
     * completes.
     * @param serviceClass the class of your {@link imp.DownloaderService} implementation
     * @return whether the service was started and the reason for starting the service.
     * Either {@link #DownloadServiceRequirement.NoDownloadRequired}, {@link #DownloadServiceRequirement.LvlCheckRequired}, or {@link
     * #DownloadServiceRequirement.DownloadRequired}.
     * @throws NameNotFoundException
     */

        public static DownloadServiceRequirement StartDownloadServiceIfRequired(Context context, PendingIntent notificationClient, Type serviceClass)
        {
            return DownloaderService.StartDownloadServiceIfRequired(context, notificationClient, serviceClass);
        }

        /**
         * This version assumes that the intent contains the pending intent as a parameter. This
         * is used for responding to alarms.
         * <p>The pending intent must be in an extra with the key {@link 
         * impl.DownloaderService#PendingIntent}.
         * 
         * @param context
         * @param notificationClient
         * @param serviceClass the class of the service to start
         * @return
         * @throws NameNotFoundException
         */

        public static DownloadServiceRequirement StartDownloadServiceIfRequired(Context context, Intent notificationClient, Type serviceClass)
        {
            return DownloaderService.StartDownloadServiceIfRequired(context, notificationClient, serviceClass);
        }

        #region Nested type: Proxy

        private class Proxy : IDownloaderClient
        {
            private readonly Messenger mServiceMessenger;

            public Proxy(Messenger msg)
            {
                mServiceMessenger = msg;
            }

            #region IDownloaderClient Members

            public void OnDownloadStateChanged(DownloaderClientState newState)
            {
                var p = new Bundle(1);
                p.PutInt(PARAM_NEW_STATE, (int) newState);
                send(MSG_ONDOWNLOADSTATE_CHANGED, p);
            }

            public void OnDownloadProgress(DownloadProgressInfo progress)
            {
                var p = new Bundle(1);
                p.PutString(PARAM_PROGRESS, progress.ToString());
                send(MSG_ONDOWNLOADPROGRESS, p);
            }

            public void OnServiceConnected(Messenger m)
            {
                /**
             * This is never called through the proxy.
             */
            }

            #endregion

            private void send(int method, Bundle p)
            {
                Message m = Message.Obtain(null, method);
                m.Data = (p);
                try
                {
                    mServiceMessenger.Send(m);
                }
                catch (RemoteException e)
                {
                    e.PrintStackTrace();
                }
            }
        }

        #endregion

        #region Nested type: Stub

        private class Stub : IStub
        {
            private readonly IServiceConnection mConnection;
            private readonly Type mDownloaderServiceClass;
            private readonly IDownloaderClient mItf;
            private readonly Messenger mMessenger;
            private bool mBound;
            private Context mContext;
            private Messenger mServiceMessenger;
            /**
         * Target we publish for clients to send messages to IncomingHandler.
         */

            public Stub(IDownloaderClient itf, Type downloaderService)
            {
                var handler = new Handler(msg =>
                                              {
                                                  switch (msg.What)
                                                  {
                                                      case MSG_ONDOWNLOADPROGRESS:
                                                          Bundle bun = msg.Data;
                                                          if (null != mContext)
                                                          {
                                                              bun.SetClassLoader(mContext.ClassLoader);
                                                              var dpi = new DownloadProgressInfo(msg.Data.GetString(PARAM_PROGRESS));
                                                              mItf.OnDownloadProgress(dpi);
                                                          }
                                                          break;
                                                      case MSG_ONDOWNLOADSTATE_CHANGED:
                                                          mItf.OnDownloadStateChanged((DownloaderClientState) msg.Data.GetInt(PARAM_NEW_STATE));
                                                          break;
                                                      case MSG_ONSERVICECONNECTED:
                                                          mItf.OnServiceConnected((Messenger) msg.Data.GetParcelable(PARAM_MESSENGER));
                                                          break;
                                                  }
                                              });
                mMessenger = new Messenger(handler);
                mConnection = new ServiceConnection(this);
                mItf = itf;
                mDownloaderServiceClass = downloaderService;
            }

            /**
         * Class for interacting with the main interface of the service.
         */

            #region IStub Members

            public void Connect(Context c)
            {
                mContext = c;
                var bindIntent = new Intent(c, mDownloaderServiceClass);
                bindIntent.PutExtra(PARAM_MESSENGER, mMessenger);
                var bound = c.BindService(bindIntent, mConnection, Bind.DebugUnbind);
                if (!bound)
                {
                    System.Diagnostics.Debug.WriteLine("LVLDL Service Unbound");
                }
            }

            public void Disconnect(Context c)
            {
                if (mBound)
                {
                    c.UnbindService(mConnection);
                    mBound = false;
                }
                mContext = null;
            }

            public Messenger GetMessenger()
            {
                return mMessenger;
            }

            #endregion

            #region Nested type: ServiceConnection

            private class ServiceConnection : Object, IServiceConnection
            {
                private readonly Stub _stub;

                public ServiceConnection(Stub stub)
                {
                    _stub = stub;
                }

                #region IServiceConnection Members

                public void OnServiceConnected(ComponentName className, IBinder service)
                {
                    System.Diagnostics.Debug.WriteLine("ServiceConnection.OnServiceConnected");

                    // This is called when the connection with the service has been
                    // established, giving us the object we can use to
                    // interact with the service. We are communicating with the
                    // service using a Messenger, so here we get a client-side
                    // representation of that from the raw IBinder object.
                    _stub.mServiceMessenger = new Messenger(service);
                    _stub.mItf.OnServiceConnected(_stub.mServiceMessenger);
                    _stub.mBound = true;
                }

                public void OnServiceDisconnected(ComponentName className)
                {
                    System.Diagnostics.Debug.WriteLine("ServiceConnection.OnServiceDisconnected");
                    
                    // This is called when the connection with the service has been
                    // unexpectedly disconnected -- that is, its process crashed.
                    _stub.mServiceMessenger = null;
                    _stub.mBound = false;
                }

                #endregion
            }

            #endregion
        }

        #endregion
    }
}