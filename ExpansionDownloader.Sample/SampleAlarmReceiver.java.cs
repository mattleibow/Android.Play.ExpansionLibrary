namespace ExpansionDownloader.Sample
{
    using System.Diagnostics;

    using Android.Content;
    using Android.Content.PM;

    using ExpansionDownloader.Client;
    using ExpansionDownloader.Service;

    /// <summary>
    /// The alarm receiver for this sample.
    /// </summary>
    [BroadcastReceiver]
    public class SampleAlarmReceiver : BroadcastReceiver
    {
        #region Public Methods and Operators

        /// <summary>
        /// This method is called when the BroadcastReceiver is receiving an Intent
        /// broadcast.
        /// </summary>
        /// <param name="context">
        /// The Context in which the receiver is running.
        /// </param>
        /// <param name="intent">
        /// The Intent being received.
        /// </param>
        public override void OnReceive(Context context, Intent intent)
        {
            Debug.WriteLine("SampleAlarmReceiver.OnReceive");

            try
            {
                DownloaderService.StartDownloadServiceIfRequired(
                    context, intent, typeof(SampleDownloaderService));
            }
            catch (PackageManager.NameNotFoundException e)
            {
                e.PrintStackTrace();
            }
        }

        #endregion
    }
}