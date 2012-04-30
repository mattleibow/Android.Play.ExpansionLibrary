using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Java.Lang;

namespace ExpansionDownloader.impl
{
    public abstract class CustomIntentService : Service
    {
        private static string LOG_TAG = "CancellableIntentService";
        private static int WHAT_MESSAGE = -10;
        private readonly string mName;
        private bool mRedelivery;
        private volatile ServiceHandler mServiceHandler;
        private volatile Looper mServiceLooper;

        public CustomIntentService(string paramString)
        {
            mName = paramString;
        }

        public override IBinder OnBind(Intent paramIntent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            var localHandlerThread = new HandlerThread("IntentService[" + mName + "]");
            localHandlerThread.Start();
            mServiceLooper = localHandlerThread.Looper;
            mServiceHandler = new ServiceHandler(mServiceLooper, this);
        }

        public override void OnDestroy()
        {
            Thread localThread = mServiceLooper.Thread;
            if ((localThread != null) && (localThread.IsAlive))
            {
                localThread.Interrupt();
            }
            mServiceLooper.Quit();
            Log.Debug(LOG_TAG, "onDestroy");
        }

        protected abstract void OnHandleIntent(Intent paramIntent);
        protected abstract bool shouldStop();

        public override void OnStart(Intent paramIntent, int startId)
        {
            if (!mServiceHandler.HasMessages(WHAT_MESSAGE))
            {
                Message localMessage = mServiceHandler.ObtainMessage();
                localMessage.Arg1 = startId;
                localMessage.Obj = paramIntent;
                localMessage.What = WHAT_MESSAGE;
                mServiceHandler.SendMessage(localMessage);
            }
        }

        public override StartCommandResult OnStartCommand(Intent paramIntent, StartCommandFlags flags, int startId)
        {
            OnStart(paramIntent, startId);
            return mRedelivery ? StartCommandResult.RedeliverIntent : StartCommandResult.NotSticky;
        }

        public void setIntentRedelivery(bool enabled)
        {
            mRedelivery = enabled;
        }

        #region Nested type: ServiceHandler

        private class ServiceHandler : Handler
        {
            private readonly CustomIntentService _cis;

            public ServiceHandler(Looper looper, CustomIntentService cis)
                : base(looper)
            {
                _cis = cis;
            }

            public override void HandleMessage(Message paramMessage)
            {
                _cis.OnHandleIntent((Intent) paramMessage.Obj);
                if (_cis.shouldStop())
                {
                    Log.Debug(LOG_TAG, "stopSelf");
                    _cis.StopSelf(paramMessage.Arg1);
                    Log.Debug(LOG_TAG, "afterStopSelf");
                }
            }
        }

        #endregion
    }
}