namespace ExpansionDownloader
{
    using System.Diagnostics;

    using Android.App;
    using Android.Content;
    using Android.Net;
    using Android.Runtime;
    using Android.Telephony;

    using Java.Lang;

    internal class SystemFacade
    {
        private readonly Context context;
        private readonly NotificationManager notificationManager;

        public SystemFacade(Context context)
        {
            this.context = context;
            this.notificationManager = this.context.GetSystemService(Context.NotificationService).JavaCast<NotificationManager>();
        }

        public ConnectivityType? GetActiveNetworkType()
        {
            var connectivity =
                this.context.GetSystemService(Context.ConnectivityService).JavaCast<ConnectivityManager>();
            if (connectivity == null)
            {
                Debug.WriteLine("LVLDL couldn't get connectivity manager");
                return null;
            }

            NetworkInfo activeInfo = connectivity.ActiveNetworkInfo;
            if (activeInfo == null)
            {
                Debug.WriteLine("LVLDL network is not available");
                return null;
            }

            return activeInfo.Type;
        }

        public bool IsNetworkRoaming()
        {
            var connectivity = this.context.GetSystemService(Context.ConnectivityService).JavaCast<ConnectivityManager>();
            if (connectivity == null)
            {
                Debug.WriteLine("LVLDL couldn't get connectivity manager");
                return false;
            }

            NetworkInfo info = connectivity.ActiveNetworkInfo;
            bool isMobile = info != null && info.Type == ConnectivityType.Mobile;
            var tm = this.context.GetSystemService(Context.TelephonyService).JavaCast<TelephonyManager>();
            if (tm == null)
            {
                Debug.WriteLine("LVLDL couldn't get telephony manager");
                return false;
            }

            bool isRoaming = isMobile && tm.IsNetworkRoaming;
            if (isRoaming)
            {
                Debug.WriteLine("LVLDL network is roaming");
            }

            return isRoaming;
        }

        public long GetMaxBytesOverMobile()
        {
            return int.MaxValue;
        }

        public long GetRecommendedMaxBytesOverMobile()
        {
            return 2097152L;
        }

        public void SendBroadcast(Intent intent)
        {
            this.context.SendBroadcast(intent);
        }

        public bool UserOwnsPackage(int uid, string packageName)
        {
            return this.context.PackageManager.GetApplicationInfo(packageName, 0).Uid == uid;
        }

        public void PostNotification(long id, Notification notification)
        {
            // TODO: The system notification manager takes ints, not longs, as IDs,
            // but the download manager uses IDs take straight from the database,
            // which are longs. This will have to be dealt with at some point.
            this.notificationManager.Notify((int)id, notification);
        }

        public void CancelNotification(long id)
        {
            this.notificationManager.Cancel((int) id);
        }

        public void CancelAllNotifications()
        {
            this.notificationManager.CancelAll();
        }

        public void StartThread(Thread thread)
        {
            thread.Start();
        }
    }
}