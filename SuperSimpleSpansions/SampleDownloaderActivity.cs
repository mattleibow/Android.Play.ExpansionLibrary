namespace SuperSimpleSpansions
{
    using System.Linq;

    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Provider;
    using Android.Views;
    using Android.Widget;

    using ExpansionDownloader;
    using ExpansionDownloader.Client;
    using ExpansionDownloader.Database;
    using ExpansionDownloader.Service;

    [Activity(Label = "Super Simple Spansions", MainLauncher = true, Icon = "@drawable/Icon", NoHistory = true)]
    public class SuperSimpleActivity : Activity, IDownloaderClient
    {
        private View dashboardView;
        private Button openWiFiSettingsButton;
        private Button pauseButton;
        private ProgressBar progressBar;
        private TextView progressFractionTextView;
        private Button resumeOnCellDataButton;
        private TextView statusTextView;
        private View useCellDataView;
        private IDownloaderService downloaderService;
        private IDownloaderServiceConnection downloaderServiceConnection;
        private DownloaderState downloaderState;
        private bool isPaused;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.SetContentView(Resource.Layout.Main);

            this.progressBar = this.FindViewById<ProgressBar>(Resource.Id.prgProgress);
            this.statusTextView = this.FindViewById<TextView>(Resource.Id.lblStatus);
            this.progressFractionTextView = this.FindViewById<TextView>(Resource.Id.lblProgress);
            this.dashboardView = this.FindViewById(Resource.Id.dashboard);
            this.useCellDataView = this.FindViewById(Resource.Id.approve);
            this.pauseButton = this.FindViewById<Button>(Resource.Id.btnPause);
            this.openWiFiSettingsButton = this.FindViewById<Button>(Resource.Id.btnWifi);
            this.resumeOnCellDataButton = this.FindViewById<Button>(Resource.Id.btnResumeCell);

            this.pauseButton.Click += delegate
                {
                    if (this.isPaused)
                        this.downloaderService.RequestContinueDownload();
                    else
                        this.downloaderService.RequestPauseDownload();
                    this.UpdatePauseButton(!this.isPaused);
                };
            this.openWiFiSettingsButton.Click += delegate { this.StartActivity(new Intent(Settings.ActionWifiSettings)); };
            this.resumeOnCellDataButton.Click += delegate
                {
                    this.downloaderService.SetDownloadFlags(ServiceFlags.FlagsDownloadOverCellular);
                    this.downloaderService.RequestContinueDownload();
                    this.useCellDataView.Visibility = ViewStates.Gone;
                };

            this.dashboardView.Visibility = ViewStates.Gone;
            this.useCellDataView.Visibility = ViewStates.Gone;

            var delivered = this.AreExpansionFilesDelivered();

            if (delivered)
            {
                this.statusTextView.Text = "Download Complete!";
            }
            else if (!this.GetExpansionFiles())
            {
                this.downloaderServiceConnection = ClientMarshaller.CreateStub(this, typeof(SampleDownloaderService));
            }
        }

        protected override void OnResume()
        {
            if (this.downloaderServiceConnection != null)
            {
                this.downloaderServiceConnection.Connect(this);
            }

            base.OnResume();
        }

        protected override void OnStop()
        {
            if (this.downloaderServiceConnection != null)
            {
                this.downloaderServiceConnection.Disconnect(this);
            }

            base.OnStop();
        }

        public void OnDownloadProgress(DownloadProgressInfo progress)
        {
            this.progressBar.Max = (int)(progress.OverallTotal >> 8);
            this.progressBar.Progress = (int)(progress.OverallProgress >> 8);
            this.progressFractionTextView.Text = Helpers.GetDownloadProgressString(progress.OverallProgress, progress.OverallTotal);
        }

        public void OnDownloadStateChanged(DownloaderState newState)
        {
            if (this.downloaderState != newState)
            {
                this.downloaderState = newState;
                this.statusTextView.Text = Helpers.GetDownloaderStringFromState(newState);
            }

            if (newState != DownloaderState.Completed)
            {
                this.dashboardView.Visibility = newState.CanShowProgress() ? ViewStates.Visible : ViewStates.Gone;
                this.useCellDataView.Visibility = newState.IsWaitingForCellApproval() ? ViewStates.Visible : ViewStates.Gone;
                this.progressBar.Indeterminate = newState.IsIndeterminate();
                this.UpdatePauseButton(newState.IsPaused());
            }
        }

        public void OnServiceConnected(Messenger m)
        {
            this.downloaderService = ServiceMarshaller.CreateProxy(m);
            this.downloaderService.OnClientUpdated(this.downloaderServiceConnection.GetMessenger());
        }

        private bool AreExpansionFilesDelivered()
        {
            var downloads = DownloadsDatabase.GetDownloads();

            return downloads.Any() && downloads.All(x => Helpers.DoesFileExist(this, x.FileName, x.TotalBytes, false));
        }

        private bool GetExpansionFiles()
        {
            bool result = false;

            // Build the intent that launches this activity.
            Intent launchIntent = this.Intent;
            var intent = new Intent(this, typeof(SuperSimpleActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
            intent.SetAction(launchIntent.Action);

            if (launchIntent.Categories != null)
            {
                foreach (string category in launchIntent.Categories)
                {
                    intent.AddCategory(category);
                }
            }

            // Build PendingIntent used to open this activity when user 
            // taps the notification.
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.UpdateCurrent);

            // Request to start the download
            DownloadServiceRequirement startResult = DownloaderService.StartDownloadServiceIfRequired(this, pendingIntent, typeof(SampleDownloaderService));

            // The DownloaderService has started downloading the files, 
            // show progress otherwise, the download is not needed so  we 
            // fall through to starting the actual app.
            if (startResult != DownloadServiceRequirement.NoDownloadRequired)
            {
                this.downloaderServiceConnection = ClientMarshaller.CreateStub(this, typeof(SampleDownloaderService));

                result = true;
            }

            return result;
        }

        private void UpdatePauseButton(bool paused)
        {
            this.isPaused = paused;
            this.pauseButton.SetText(paused ? Resource.String.text_button_resume : Resource.String.text_button_pause);
        }
    }
}