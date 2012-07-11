namespace SuperSimpleSpansions
{
    using Android.Content;

    using ExpansionDownloader.Service;

    [BroadcastReceiver(Exported = false)]
    public class SampleAlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            DownloaderService.StartDownloadServiceIfRequired(context, intent, typeof(SampleDownloaderService));
        }
    }
}