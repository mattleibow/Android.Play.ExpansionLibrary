using Java.Lang;

namespace ExpansionDownloader.impl
{
    public class CustomNotificationFactory
    {
        public static DownloadNotification.ICustomNotification createCustomNotification()
        {
            // return new V11CustomNotification();

            try
            {
                Class.ForName("android.app.Notification$Builder");
                return new V11CustomNotification();
            }
            catch (ClassNotFoundException e)
            {
                return new V3CustomNotification();
            }
        }
    }
}