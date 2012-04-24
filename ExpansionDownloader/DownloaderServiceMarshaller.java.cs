using Android.Content;
using Android.OS;
using ExpansionDownloader.impl;

namespace ExpansionDownloader
{
    public class DownloaderServiceMarshaller
    {
        public const int MSG_REQUEST_ABORT_DOWNLOAD = 1;
        public const int MSG_REQUEST_PAUSE_DOWNLOAD = 2;
        public const int MSG_SET_DOWNLOAD_FLAGS = 3;
        public const int MSG_REQUEST_CONTINUE_DOWNLOAD = 4;
        public const int MSG_REQUEST_DOWNLOAD_STATE = 5;
        public const int MSG_REQUEST_CLIENT_UPDATE = 6;

        public const string PARAMS_FLAGS = "flags";
        public const string PARAM_MESSENGER = DownloaderService.EXTRA_MESSAGE_HANDLER;

        /**
     * Returns a proxy that will marshall calls to IDownloaderService methods
     * 
     * @param ctx
     * @return
     */

        public static IDownloaderService CreateProxy(Messenger msg)
        {
            return new Proxy(msg);
        }

        /**
     * Returns a stub object that, when connected, will listen for marshalled
     * IDownloaderService methods and translate them into calls to the supplied
     * interface.
     * 
     * @param itf An implementation of IDownloaderService that will be called
     *            when remote method calls are unmarshalled.
     * @return
     */

        public static IStub CreateStub(IDownloaderService itf)
        {
            return new Stub(itf);
        }

        #region Nested type: Proxy

        private class Proxy : IDownloaderService
        {
            private readonly Messenger mMsg;

            public Proxy(Messenger msg)
            {
                mMsg = msg;
            }

            #region IDownloaderService Members

            public void requestAbortDownload()
            {
                send(MSG_REQUEST_ABORT_DOWNLOAD, new Bundle());
            }

            public void requestPauseDownload()
            {
                send(MSG_REQUEST_PAUSE_DOWNLOAD, new Bundle());
            }

            public void setDownloadFlags(int flags)
            {
                var p = new Bundle();
                p.PutInt(PARAMS_FLAGS, flags);
                send(MSG_SET_DOWNLOAD_FLAGS, p);
            }

            public void requestContinueDownload()
            {
                send(MSG_REQUEST_CONTINUE_DOWNLOAD, new Bundle());
            }

            public void requestDownloadStatus()
            {
                send(MSG_REQUEST_DOWNLOAD_STATE, new Bundle());
            }

            public void onClientUpdated(Messenger clientMessenger)
            {
                var bundle = new Bundle(1);
                bundle.PutParcelable(PARAM_MESSENGER, clientMessenger);
                send(MSG_REQUEST_CLIENT_UPDATE, bundle);
            }

            #endregion

            private void send(int method, Bundle p)
            {
                Message m = Message.Obtain(null, method);
                m.Data = p;
                try
                {
                    mMsg.Send(m);
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
            private readonly IDownloaderService mItf;

            private readonly Messenger mMessenger;

            public Stub(IDownloaderService itf)
            {
                var handler = new Handler(msg =>
                                              {
                                                  switch (msg.What)
                                                  {
                                                      case MSG_REQUEST_ABORT_DOWNLOAD:
                                                          mItf.requestAbortDownload();
                                                          break;
                                                      case MSG_REQUEST_CONTINUE_DOWNLOAD:
                                                          mItf.requestContinueDownload();
                                                          break;
                                                      case MSG_REQUEST_PAUSE_DOWNLOAD:
                                                          mItf.requestPauseDownload();
                                                          break;
                                                      case MSG_SET_DOWNLOAD_FLAGS:
                                                          mItf.setDownloadFlags(msg.Data.GetInt(PARAMS_FLAGS));
                                                          break;
                                                      case MSG_REQUEST_DOWNLOAD_STATE:
                                                          mItf.requestDownloadStatus();
                                                          break;
                                                      case MSG_REQUEST_CLIENT_UPDATE:
                                                          mItf.onClientUpdated((Messenger) msg.Data.GetParcelable(PARAM_MESSENGER));
                                                          break;
                                                  }
                                              });
                mMessenger = new Messenger(handler);
                mItf = itf;
            }

            #region IStub Members

            public Messenger getMessenger()
            {
                return mMessenger;
            }

            public void connect(Context c)
            {
            }

            public void disconnect(Context c)
            {
            }

            #endregion
        }

        #endregion
    }
}