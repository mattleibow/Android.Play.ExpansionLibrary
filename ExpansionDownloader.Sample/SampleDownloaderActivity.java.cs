using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Util;
using Android.Views;
using Android.Widget;
using ExpansionDownloader.impl;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;

namespace ExpansionDownloader.Sample
{
    using System.IO.Compression.Zip;

    [Activity(Label = "ExpansionDownloader.Sample", MainLauncher = true, Icon = "@drawable/icon")]
    public class SampleDownloaderActivity : Activity, IDownloaderClient
    {
        private static string LOG_TAG = "LVLDownloader";
        private static float SMOOTHING_FACTOR = 0.005f;
        private TextView mAverageSpeed;
        private bool mCancelValidation;
        private View mCellMessage;
        private View mDashboard;
        private IStub mDownloaderClientStub;

        private ProgressBar mPB;
        private Button mPauseButton;
        
        private TextView mProgressFraction;
        private TextView mProgressPercent;
        private IDownloaderService mRemoteService;
        private DownloaderClientState mState;
        private bool mStatePaused;
        private TextView mStatusText;
        private TextView mTimeRemaining;
        
        private Button mWiFiSettingsButton;
        private readonly XAPKFile[] xAPKS = new[] { new XAPKFile(true, 4, 12), new XAPKFile(false, 4, 12) };

        #region IDownloaderClient Members
        
        public void OnServiceConnected(Messenger m)
        {
            System.Diagnostics.Debug.WriteLine("Activity.onServiceConnected");
            mRemoteService = DownloaderServiceMarshaller.CreateProxy(m);
            mRemoteService.OnClientUpdated(mDownloaderClientStub.GetMessenger());
        }

        /**
         * The download state should trigger changes in the UI --- it may be useful
         * to show the state as being indeterminate at times. This sample can be
         * considered a guideline.
         */

        public void OnDownloadStateChanged(DownloaderClientState newState)
        {
            System.Diagnostics.Debug.WriteLine("Activity.OnDownloadStateChanged");
            setState(newState);
            bool showDashboard = true;
            bool showCellMessage = false;
            bool paused;
            bool indeterminate;
            switch (newState)
            {
                case DownloaderClientState.Idle:
                    // Idle means the service is listening, so it's
                    // safe to start making calls via mRemoteService.
                    paused = false;
                    indeterminate = true;
                    break;
                case DownloaderClientState.Connecting:
                case DownloaderClientState.FetchingUrl:
                    showDashboard = true;
                    paused = false;
                    indeterminate = true;
                    break;
                case DownloaderClientState.Downloading:
                    paused = false;
                    showDashboard = true;
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
                case DownloaderClientState.Completed:
                    showDashboard = false;
                    paused = false;
                    indeterminate = false;
                    validateXAPKZipFiles();
                    return;
                default:
                    paused = true;
                    indeterminate = true;
                    showDashboard = true;
                    break;
            }
            ViewStates newDashboardVisibility = showDashboard ? ViewStates.Visible : ViewStates.Gone;
            if (mDashboard.Visibility != newDashboardVisibility)
            {
                mDashboard.Visibility = (newDashboardVisibility);
            }
            ViewStates cellMessageVisibility = showCellMessage ? ViewStates.Visible : ViewStates.Gone;
            if (mCellMessage.Visibility != cellMessageVisibility)
            {
                mCellMessage.Visibility = (cellMessageVisibility);
            }
            mPB.Indeterminate = (indeterminate);
            setButtonPausedState(paused);
        }

        /**
         * Sets the state of the various controls based on the progressinfo object
         * sent from the downloader service.
         */

        public void OnDownloadProgress(DownloadProgressInfo progress)
        {
            mAverageSpeed.Text = Helpers.GetSpeedString(progress.CurrentSpeed) + " Kb/s";
            mTimeRemaining.Text = "Time remaining: " + Helpers.GetTimeRemaining(progress.TimeRemaining);

            mPB.Max = ((int)(progress.OverallTotal >> 8));
            mPB.Progress = ((int)(progress.OverallProgress >> 8));
            mProgressPercent.Text = (progress.OverallProgress * 100 / progress.OverallTotal) + "%";
            mProgressFraction.Text =
                (Helpers.GetDownloadProgressString(progress.OverallProgress, progress.OverallTotal));
        }

        #endregion

        private void setState(DownloaderClientState newState)
        {
            if (mState != newState)
            {
                mState = newState;
                mStatusText.Text = Helpers.GetDownloaderStringFromState(newState);
            }
        }

        private void setButtonPausedState(bool paused)
        {
            mStatePaused = paused;
            int stringResourceID = paused ? Resource.String.text_button_resume : Resource.String.text_button_pause;
            mPauseButton.SetText(stringResourceID);
        }

        /**
         * Go through each of the Expansion APK files defined in the project and
         * determine if the files are present and match the required size. Free
         * applications should definitely consider doing this, as this allows the
         * application to be launched for the first time without having a network
         * connection present. Paid applications that use LVL should probably do at
         * least one LVL check that requires the network to be present, so this is
         * not as necessary.
         * 
         * @return true if they are present.
         */

        private bool expansionFilesDelivered()
        {
            foreach (XAPKFile xf in xAPKS)
            {
                string fileName = Helpers.GetExpansionApkFileName(this, xf.mIsMain, xf.mFileVersion);
                if (!Helpers.DoesFileExist(this, fileName, xf.mFileSize, false))
                    return false;
            }
            return true;
        }

        /**
         * Calculating a moving average for the validation speed so we don't get jumpy
         * calculations for time etc.
         */

        /**
         * Go through each of the Expansion APK files and open each as a zip file.
         * Calculate the CRC for each file and return false if any fail to match.
         * 
         * @return true if XAPKZipFile is successful
         */

        private void validateXAPKZipFiles()
        {
            mDashboard.Visibility = ViewStates.Visible;
            mCellMessage.Visibility = ViewStates.Gone;
            mStatusText.SetText(Resource.String.text_verifying_download);
            mPauseButton.Click += delegate { mCancelValidation = true; };
            mPauseButton.SetText(Resource.String.text_button_cancel_verify);

            // todo do the actual checks...
        }

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
        ///  If the download isn't present, we initialize the download UI. This ties
        /// all of the controls into the remote service calls.
        /// </summary>
        private void initializeDownloadUI()
        {
            mDownloaderClientStub = DownloaderClientMarshaller.CreateStub(this, typeof(SampleDownloaderService));
            SetContentView(Resource.Layout.Main);

            mPB = (ProgressBar)FindViewById(Resource.Id.progressBar);
            mStatusText = (TextView)FindViewById(Resource.Id.statusText);
            mProgressFraction = (TextView)FindViewById(Resource.Id.progressAsFraction);
            mProgressPercent = (TextView)FindViewById(Resource.Id.progressAsPercentage);
            mAverageSpeed = (TextView)FindViewById(Resource.Id.progressAverageSpeed);
            mTimeRemaining = (TextView)FindViewById(Resource.Id.progressTimeRemaining);
            mDashboard = FindViewById(Resource.Id.downloaderDashboard);
            mCellMessage = FindViewById(Resource.Id.approveCellular);
            mPauseButton = (Button)FindViewById(Resource.Id.pauseButton);
            mWiFiSettingsButton = (Button)FindViewById(Resource.Id.wifiSettingsButton);

            mPauseButton.Click += delegate
                {
                    if (mStatePaused)
                    {
                        mRemoteService.RequestContinueDownload();
                    }
                    else
                    {
                        mRemoteService.RequestPauseDownload();
                    }
                    setButtonPausedState(!mStatePaused);
                };

            mWiFiSettingsButton.Click += delegate { StartActivity(new Intent(Settings.ActionWifiSettings)); };

            var resumeOnCell = (Button)FindViewById(Resource.Id.resumeOverCellular);
            resumeOnCell.Click += delegate
                {
                    mRemoteService.SetDownloadFlags(DownloaderServiceFlags.FlagsDownloadOverCellular);
                    mRemoteService.RequestContinueDownload();
                    mCellMessage.Visibility = ViewStates.Gone;
                };
        }

        /**
         * Called when the activity is first create; we wouldn't create a layout in the case
         * where we have the file and are moving to another activity without downloading.
         */

        protected override void OnCreate(Bundle savedInstanceState)
        {
            CreateCustomNotification();

            base.OnCreate(savedInstanceState);

            // Before we do anything, are the files we expect already here and delivered (presumably by Market) 
            // For free titles, this is probably worth doing. (so no Market request is necessary)
            if (expansionFilesDelivered() || !GetExpansionFiles())
            {
                initializeDownloadUI();
                validateXAPKZipFiles();
            }
        }

        private bool GetExpansionFiles()
        {
            bool result = false;
            try
            {
                Intent launchIntent = Intent;
                var intentToLaunchThisActivityFromNotification = new Intent(this, typeof(SampleDownloaderActivity));
                intentToLaunchThisActivityFromNotification.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                intentToLaunchThisActivityFromNotification.SetAction(launchIntent.Action);

                if (launchIntent.Categories != null)
                {
                    foreach (string category in launchIntent.Categories)
                    {
                        intentToLaunchThisActivityFromNotification.AddCategory(category);
                    }
                }

                // Build PendingIntent used to open this activity from Notification
                PendingIntent pendingIntent = PendingIntent.GetActivity(
                    this, 0, intentToLaunchThisActivityFromNotification, PendingIntentFlags.UpdateCurrent);

                // Request to start the download
                DownloadServiceRequirement startResult = DownloaderClientMarshaller.StartDownloadServiceIfRequired(
                    this, pendingIntent, typeof(SampleDownloaderService));

                if (startResult != DownloadServiceRequirement.NoDownloadRequired)
                {
                    // The DownloaderService has started downloading the files, show progress
                    // otherwise, download not needed so we fall through to starting the movie
                    this.initializeDownloadUI();
                    result = true;
                }
            }
            catch (PackageManager.NameNotFoundException e)
            {
                Log.Error(LOG_TAG, "Cannot find own package! MAYDAY!");
                e.PrintStackTrace();
            }

            return result;
        }

        /**
         * Connect the stub to our service on resume.
         */

        protected override void OnResume()
        {
            if (null != mDownloaderClientStub)
            {
                mDownloaderClientStub.Connect(this);
            }
            base.OnResume();
        }

        /**
         * Disconnect the stub from our service on stop
         */

        protected override void OnStop()
        {
            if (null != mDownloaderClientStub)
            {
                mDownloaderClientStub.Disconnect(this);
            }
            base.OnStop();
        }

        /**
         * Critical implementation detail. In onServiceConnected we create the
         * remote service and marshaler. This is how we pass the client information
         * back to the service so the client can be properly notified of changes. We
         * must do this every time we reconnect to the service.
         */

        protected override void OnDestroy()
        {
            mCancelValidation = true;
            base.OnDestroy();
        }

        #region Nested type: XAPKFile

        /**
         * This is a little helper class that demonstrates simple testing of an
         * Expansion APK file delivered by Market. You may not wish to hard-code
         * things such as file lengths into your executable... and you may wish to
         * turn this code off during application development.
         */

        private class XAPKFile
        {
            public readonly long mFileSize;

            public readonly int mFileVersion;

            public readonly bool mIsMain;

            public XAPKFile(bool isMain, int fileVersion, long fileSize)
            {
                mIsMain = isMain;
                mFileVersion = fileVersion;
                mFileSize = fileSize;
            }
        }

        #endregion
    }
}
