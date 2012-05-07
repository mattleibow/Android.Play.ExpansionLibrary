using System.IO;
using System.IO.Compression.Zip;
using System.Threading;
using System.Threading.Tasks;

namespace ExpansionDownloader.Sample
{
    using System;
    using System.Linq;

    using Android.App;
    using Android.Content;
    using Android.Content.PM;
    using Android.OS;
    using Android.Provider;
    using Android.Views;

    using ExpansionDownloader.Client;
    using ExpansionDownloader.Service;
    using ExpansionDownloader.impl;

    [Activity(Label = "ExpansionDownloader.Sample", MainLauncher = true, Icon = "@drawable/icon")]
    public partial class SampleDownloaderActivity : Activity, IDownloaderClient
    {
        private IDownloaderServiceConnection _downloaderServiceConnection;
        private IDownloaderService downloaderService;
        private DownloaderClientState downloaderState;
        private bool isPaused;
        
        #region IDownloaderClient Members
        
        /// <summary>
        /// Create the remote service and marshaler.
        /// </summary>
        /// <remarks>
        /// This is how we pass the client information back to the service so 
        /// the client can be properly notified of changes. 
        /// Do this every time we reconnect to the service.
        /// </remarks>
        /// <param name="m">The messenger to use.</param>
        public void OnServiceConnected(Messenger m)
        {
            this.downloaderService = DownloaderServiceMarshaller.CreateProxy(m);
            this.downloaderService.OnClientUpdated(this._downloaderServiceConnection.GetMessenger());
        }

        /// <summary>
        /// The download state should trigger changes in the UI.
        /// It may be useful to show the state as being indeterminate at times.  
        /// </summary>
        /// <param name="newState">The new state.</param>
        public void OnDownloadStateChanged(DownloaderClientState newState)
        {
            if (this.downloaderState != newState)
            {
                this.downloaderState = newState;
                this.statusTextView.Text = Helpers.GetDownloaderStringFromState(newState);
            }

            bool showDashboard = true;
            bool showCellMessage = false;
            bool paused = false;
            bool indeterminate = true;
            switch (newState)
            {
                case DownloaderClientState.Idle:
                case DownloaderClientState.Connecting:
                case DownloaderClientState.FetchingUrl:
                    break;
                case DownloaderClientState.Downloading:
                    indeterminate = false;
                    break;
                case DownloaderClientState.Failed:
                case DownloaderClientState.FailedCanceled:
                case DownloaderClientState.FailedFetchingUrl:
                case DownloaderClientState.FailedUnlicensed:
                    paused = true;
                    showDashboard = false;
                    indeterminate = false;
                    break;
                case DownloaderClientState.PausedNeedCellularPermission:
                case DownloaderClientState.PausedWifiDisabledNeedCellularPermission:
                    showDashboard = false;
                    paused = true;
                    indeterminate = false;
                    showCellMessage = true;
                    break;
                case DownloaderClientState.PausedByRequest:
                    paused = true;
                    indeterminate = false;
                    break;
                case DownloaderClientState.PausedRoaming:
                case DownloaderClientState.PausedSdCardUnavailable:
                    paused = true;
                    indeterminate = false;
                    break;
                default:
                    paused = true;
                    break;
            }

            if (newState != DownloaderClientState.Completed)
            {
                this.dashboardView.Visibility = showDashboard ? ViewStates.Visible : ViewStates.Gone;
                this.useCellDataView.Visibility = showCellMessage ? ViewStates.Visible : ViewStates.Gone;
                this.progressBar.Indeterminate = indeterminate;
                this.UpdatePauseButton(paused);
            }
            else
            {
                this.ValidateExpansionFiles();
            }
        }

        /// <summary>
        /// Sets the state of the various controls based on the progressinfo 
        /// object sent from the downloader service.
        /// </summary>
        /// <param name="progress">
        /// The progressinfo object sent from the downloader service.
        /// </param>
        public void OnDownloadProgress(DownloadProgressInfo progress)
        {
            this.averageSpeedTextView.Text = string.Format("{0} Kb/s", Helpers.GetSpeedString(progress.CurrentSpeed));
            this.timeRemainingTextView.Text = string.Format("Time remaining: {0}", Helpers.GetTimeRemaining(progress.TimeRemaining));
            this.progressBar.Max = (int)(progress.OverallTotal >> 8);
            this.progressBar.Progress = (int)(progress.OverallProgress >> 8);
            this.progressPercentTextView.Text = string.Format("{0}%", progress.OverallProgress * 100 / progress.OverallTotal);
            this.progressFractionTextView.Text = Helpers.GetDownloadProgressString(progress.OverallProgress, progress.OverallTotal);
        }

        #endregion

        /// <summary>
        /// Update the pause button.
        /// </summary>
        /// <param name="paused">
        /// Is the download paused.
        /// </param>
        private void UpdatePauseButton(bool paused)
        {
            this.isPaused = paused;
            int stringResourceId = paused ? Resource.String.text_button_resume : Resource.String.text_button_pause;
            this.pauseButton.SetText(stringResourceId);
        }

        /// <summary>
        /// Go through each of the Expansion APK files defined in the project 
        /// and determine if the files are present and match the required size. 
        /// </summary>
        /// <remarks>
        /// Free applications should definitely consider doing this, as this 
        /// allows the application to be launched for the first time without
        /// having a network connection present.
        /// Paid applications that use LVL should probably do at least one LVL 
        /// check that requires the network to be present, so this is not as
        /// necessary.
        /// </remarks>
        /// <returns>
        /// True if they are present, otherwise False;
        /// </returns>
        private bool AreExpansionFilesDelivered()
        {
            // todo always check the latest files from the market.
            return false; 
        }
        
        /// <summary>
        /// Perfom a check to see if the expansion files are vanid zip files.
        /// </summary>
        private void ValidateExpansionFiles()
        {
            // Pre execute
            this.dashboardView.Visibility = ViewStates.Visible;
            this.useCellDataView.Visibility = ViewStates.Gone;
            this.statusTextView.SetText(Resource.String.text_verifying_download);
            this.pauseButton.Click += delegate
            {
                if (this.zipFileValidationHandler != null)
                {
                    this.zipFileValidationHandler.ShouldCancel = true;
                }
            };
            this.pauseButton.SetText(Resource.String.text_button_cancel_verify);

            ThreadPool.QueueUserWorkItem(DoValidateZipFiles);
        }

        private void DoValidateZipFiles(object state)
        {
            var result = ApkExpansionSupport.GetApkExpansionFiles(this, 4, 4).All(this.IsValidZipFile);

            this.RunOnUiThread(delegate
                                   {
                                       pauseButton.Click += delegate { Finish(); };
                                       dashboardView.Visibility = ViewStates.Visible;
                                       useCellDataView.Visibility = ViewStates.Gone;

                                       if (result)
                                       {
                                           statusTextView.SetText(Resource.String.text_validation_complete);
                                           pauseButton.SetText(Android.Resource.String.Ok);
                                       }
                                       else
                                       {
                                           statusTextView.SetText(Resource.String.text_validation_failed);
                                           pauseButton.SetText(Android.Resource.String.Cancel);
                                       }
                                   });
        }

        private bool IsValidZipFile(string filename)
        {
            this.zipFileValidationHandler = new ZipFileValidationHandler(filename)
                                                {
                                                    UpdateUi = OnUpdateValidationUi
                                                };

            return File.Exists(filename) && !ZipFile.Validate(this.zipFileValidationHandler);
        }

        private void OnUpdateValidationUi(ZipFileValidationHandler handler)
        {
            var info = new DownloadProgressInfo(handler.TotalBytes,
                                                handler.CurrentBytes,
                                                handler.TimeRemaining,
                                                handler.AverageSpeed);

            this.RunOnUiThread(() => this.OnDownloadProgress(info));
        }

        private ZipFileValidationHandler zipFileValidationHandler;

        public void CreateCustomNotification()
        {
#if NOTIFICATION_BUILDER
            CustomNotificationFactory.Notification = new V11CustomNotification();
            CustomNotificationFactory.MaxBytesOverMobile = DownloadManager.GetMaxBytesOverMobile(ApplicationContext).LongValue();
            CustomNotificationFactory.RecommendedMaxBytesOverMobile = DownloadManager.GetRecommendedMaxBytesOverMobile(ApplicationContext).LongValue();
#else
            CustomNotificationFactory.Notification = new V3CustomNotification();
            CustomNotificationFactory.MaxBytesOverMobile = int.MaxValue;
            CustomNotificationFactory.RecommendedMaxBytesOverMobile = 2097152L;
#endif
        }

        /// <summary>
        /// If the download isn't present, we initialize the download UI. This ties
        /// all of the controls into the remote service calls.
        /// </summary>
        private void initializeDownloadUI()
        {
            this.InitializeControls();
            this._downloaderServiceConnection = DownloaderClientMarshaller.CreateStub(this, typeof(SampleDownloaderService));
        }

        private void OnEventHandler(object sender, EventArgs args)
        {
            this.downloaderService.SetDownloadFlags(DownloaderServiceFlags.FlagsDownloadOverCellular);
            this.downloaderService.RequestContinueDownload();
            this.useCellDataView.Visibility = ViewStates.Gone;
        }

        private void OnOpenWiFiSettingsButtonOnClick(object sender, EventArgs e)
        {
            this.StartActivity(new Intent(Settings.ActionWifiSettings));
        }

        private void OnButtonOnClick(object sender, EventArgs e)
        {
            if (this.isPaused)
            {
                this.downloaderService.RequestContinueDownload();
            }
            else
            {
                this.downloaderService.RequestPauseDownload();
            }

            this.UpdatePauseButton(!this.isPaused);
        }

        /// <summary>
        /// Called when the activity is first created; we wouldn't create a 
        /// layout in the case where we have the file and are moving to another
        /// activity without downloading.
        /// </summary>
        /// <param name="savedInstanceState">The saved instance state.</param>
        protected override void OnCreate(Bundle savedInstanceState)
        {
            this.CreateCustomNotification();

            base.OnCreate(savedInstanceState);

            // Before we do anything, are the files we expect already here and 
            // delivered (presumably by Market) 
            // For free titles, this is probably worth doing. (so no Market 
            // request is necessary)
            if (this.AreExpansionFilesDelivered() || !this.GetExpansionFiles())
            {
                this.initializeDownloadUI();
                this.ValidateExpansionFiles();
            }
        }

        private bool GetExpansionFiles()
        {
            bool result = false;

            try
            {
                // Build the intent that launches this activity.
                Intent launchIntent = Intent;
                var intent = new Intent(this, typeof(SampleDownloaderActivity));
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
                PendingIntent pendingIntent = PendingIntent.GetActivity(
                    this, 0, intent, PendingIntentFlags.UpdateCurrent);

                // Request to start the download
                DownloadServiceRequirement startResult = DownloaderClientMarshaller.StartDownloadServiceIfRequired(
                    this, pendingIntent, typeof(SampleDownloaderService));

                // The DownloaderService has started downloading the files, 
                // show progress otherwise, the download is not needed so  we 
                // fall through to starting the actual app.
                if (startResult != DownloadServiceRequirement.NoDownloadRequired)
                {
                    this.initializeDownloadUI();
                    result = true;
                }
            }
            catch (PackageManager.NameNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("Cannot find own package! MAYDAY!");
                e.PrintStackTrace();
            }

            return result;
        }

        /// <summary>
        /// Re-connect the stub to our service on resume.
        /// </summary>
        protected override void OnResume()
        {
            if (this._downloaderServiceConnection != null)
            {
                this._downloaderServiceConnection.Connect(this);
            }

            base.OnResume();
        }

        /// <summary>
        /// Disconnect the stub from our service on stop.
        /// </summary>
        protected override void OnStop()
        {
            if (this._downloaderServiceConnection != null)
            {
                this._downloaderServiceConnection.Disconnect(this);
            }

            base.OnStop();
        }
        
        protected override void OnDestroy()
        {
            if (this.zipFileValidationHandler != null)
            {
                this.zipFileValidationHandler.ShouldCancel= true;
            }

            base.OnDestroy();
        }
    }
}
