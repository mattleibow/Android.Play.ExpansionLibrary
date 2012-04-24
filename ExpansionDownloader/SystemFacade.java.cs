using Android.App;
using Android.Content;
using Android.Net;
using Android.Runtime;
using Android.Telephony;
using Android.Util;
using Java.Lang;

namespace ExpansionDownloader
{
    internal class SystemFacade
    {
        private readonly Context mContext;
        private readonly NotificationManager mNotificationManager;

        public SystemFacade(Context context)
        {
            mContext = context;
            mNotificationManager = mContext.GetSystemService(Context.NotificationService).JavaCast<NotificationManager>();
        }

        public int getActiveNetworkType()
        {
            var connectivity = mContext.GetSystemService(Context.ConnectivityService).JavaCast<ConnectivityManager>();
            if (connectivity == null)
            {
                Log.Warn(Constants.TAG, "couldn't get connectivity manager");
                return -1;
            }

            NetworkInfo activeInfo = connectivity.ActiveNetworkInfo;
            if (activeInfo == null)
            {
                if (Constants.LOGVV)
                {
                    Log.Verbose(Constants.TAG, "network is not available");
                }
                return -1;
            }
            return (int) activeInfo.Type;
        }

        public bool isNetworkRoaming()
        {
            var connectivity = mContext.GetSystemService(Context.ConnectivityService).JavaCast<ConnectivityManager>();
            if (connectivity == null)
            {
                Log.Warn(Constants.TAG, "couldn't get connectivity manager");
                return false;
            }

            NetworkInfo info = connectivity.ActiveNetworkInfo;
            bool isMobile = (info != null && info.Type == (int) ConnectivityType.Mobile);
            var tm = mContext.GetSystemService(Context.TelephonyService).JavaCast<TelephonyManager>();
            if (null == tm)
            {
                Log.Warn(Constants.TAG, "couldn't get telephony manager");
                return false;
            }
            bool isRoaming = isMobile && tm.IsNetworkRoaming;
            if (Constants.LOGVV && isRoaming)
            {
                Log.Verbose(Constants.TAG, "network is roaming");
            }
            return isRoaming;
        }

        public long getMaxBytesOverMobile()
        {
            return int.MaxValue;
        }

        public long getRecommendedMaxBytesOverMobile()
        {
            return 2097152L;
        }

        public void sendBroadcast(Intent intent)
        {
            mContext.SendBroadcast(intent);
        }

        public bool userOwnsPackage(int uid, string packageName)
        {
            return mContext.PackageManager.GetApplicationInfo(packageName, 0).Uid == uid;
        }

        public void postNotification(long id, Notification notification)
        {
            /**
         * TODO: The system notification manager takes ints, not longs, as IDs,
         * but the download manager uses IDs take straight from the database,
         * which are longs. This will have to be dealt with at some point.
         */
            mNotificationManager.Notify((int) id, notification);
        }

        public void cancelNotification(long id)
        {
            mNotificationManager.Cancel((int) id);
        }

        public void cancelAllNotifications()
        {
            mNotificationManager.CancelAll();
        }

        public void startThread(Thread thread)
        {
            thread.Start();
        }
    }
}