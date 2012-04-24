using Android.Content;
using Android.Content.PM;

namespace ExpansionDownloader.Sample
{
    [BroadcastReceiver]
    public class SampleAlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                DownloaderClientMarshaller.startDownloadServiceIfRequired(context, intent, typeof (SampleDownloaderService));
            }
            catch (PackageManager.NameNotFoundException e)
            {
                e.PrintStackTrace();
            }
        }
    }
}