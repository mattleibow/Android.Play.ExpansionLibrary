namespace ExpansionDownloader.impl
{
    using System;
    using System.IO;
    using System.Linq;

    using Android.App;
    using Android.Content;
    using Android.Content.PM;
    using Android.Net;
    using Android.Net.Wifi;
    using Android.OS;
    using Android.Runtime;
    using Android.Telephony;

    using ExpansionDownloader.Client;
    using ExpansionDownloader.Service;

    using Java.Util;

    using LicenseVerificationLibrary;

    using Debug = System.Diagnostics.Debug;

    /// <summary>
    /// The downloader service.
    /// </summary>
    public abstract partial class DownloaderService : CustomIntentService, IDownloaderService
    {
        #region Constants and Fields

        /// <summary>
        /// The buffer size used to stream the data.
        /// </summary>
        public const int BufferSize = 4096;

        /// <summary>
        /// The default user agent used for downloads.
        /// </summary>
        public const string DefaultUserAgent = "Android.LVLDM";

        /// <summary>
        /// The maximum number of redirects. (can't be more than 7)
        /// </summary>
        public const int MaxRedirects = 5;

        /// <summary>
        /// The number of times that the download manager will retry its network
        /// operations when no progress is happening before it gives up.
        /// </summary>
        public const int MaximumRetries = 5;

        /// <summary>
        /// The minimum amount of progress that has to be done before the 
        /// progress bar gets updated.
        /// </summary>
        public const int MinimumProgressStep = 4096;

        /// <summary>
        /// The minimum amount of time that has to elapse before the progress 
        /// bar gets updated, in milliseconds.
        /// </summary>
        public const long MinimumProgressTime = 1000;

        /// <summary>
        /// The minimum amount of time that the download manager accepts for
        /// a Retry-After response header with a parameter in delta-seconds.
        /// </summary>
        public const int MinimumRetryAfter = 30;

        /// <summary>
        /// The time between a failure and the first retry after an IOException.
        /// Each subsequent retry grows exponentially, doubling each time.
        /// The time is in seconds.
        /// </summary>
        public const int RetryFirstDelay = 30;

        /// <summary>
        /// The wake duration to check to see if a download is possible. (seconds)
        /// </summary>
        public const int WatchdogWakeTimer = 60;

        /// <summary>
        /// The expansion path.
        /// </summary>
        public static readonly string ExpansionPath = string.Format("{0}Android{0}obb{0}", Path.DirectorySeparatorChar);

        /// <summary>
        /// The maximum amount of time that the download manager accepts for a 
        /// Retry-After response header with a parameter in delta-seconds.
        /// </summary>
        public static readonly int MaxRetryAfter = (int)TimeSpan.FromDays(1).TotalSeconds;

        /// <summary>
        /// The wake duration to check to see if the process was killed. (seconds)
        /// </summary>
        private const int ActiveThreadWatchdog = 5;

        /// <summary>
        /// When a number has to be appended to the filename, this string is
        /// used to separate the base filename from the sequence number.
        /// </summary>
        private const string FilenameSequenceSeparator = "-";

        /// <summary>
        /// The maximum number of rows in the database (FIFO).
        /// </summary>
        private const int MaximumDownloads = 1000;

        /// <summary>
        /// Service thread status
        /// </summary>
        private const float SmoothingFactor = 0.005f;

        /// <summary>
        /// The temporary file extension.
        /// </summary>
        private const string TemporaryFileExtension = ".tmp";

        /// <summary>
        /// The locker.
        /// </summary>
        private readonly object locker = new object();

        /// <summary>
        /// Our binding to the network state broadcasts
        /// </summary>
        private readonly IDownloaderServiceConnection serviceConnection;

        /// <summary>
        /// Our binding to the network state broadcasts
        /// </summary>
        private readonly Messenger serviceMessenger;

        /// <summary>
        /// Service thread status
        /// </summary>
        private static volatile bool isRunning;

        /// <summary>
        /// Our binding to the network state broadcasts
        /// </summary>
        private Messenger clientMessenger;

        /// <summary>
        /// Our binding to the network state broadcasts
        /// </summary>
        private BroadcastReceiver connectionReceiver;

        /// <summary>
        /// Our binding to the network state broadcasts
        /// </summary>
        private DownloadNotification downloadNotification;

        /// <summary>
        /// Our binding to the network state broadcasts
        /// </summary>
        private PendingIntent alarmIntent;

        /// <summary>
        /// Used for calculating time remaining and speed
        /// </summary>
        private float averageDownloadSpeed;

        /// <summary>
        /// Used for calculating time remaining and speed
        /// </summary>
        private long bytesAtSample;

        /// <summary>
        /// Bindings to important services
        /// </summary>
        private ConnectivityManager connectivityManager;

        /// <summary>
        /// Byte counts
        /// </summary>
        private int fileCount;

        /// <summary>
        /// Package we are downloading for (defaults to package of application)
        /// </summary>
        private PackageInfo packageInfo;

        /// <summary>
        /// Our binding to the network state broadcasts
        /// </summary>
        private PendingIntent pPendingIntent;

        /// <summary>
        /// Network state.
        /// </summary>
        private bool stateChanged;

        /// <summary>
        /// Bindings to important services
        /// </summary>
        private WifiManager wifiManager;

        /// <summary>
        /// Used for calculating time remaining and speed
        /// </summary>
        private long millisecondsAtSample;

        /// <summary>
        /// The current network state.
        /// </summary>
        private NetworkState networkState;

        /// <summary>
        /// The status.
        /// </summary>
        private DownloadStatus status;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloaderService"/> class.
        /// </summary>
        protected DownloaderService()
            : base("LVLDownloadService")
        {
            Debug.WriteLine("LVLDL DownloaderService()");

            this.serviceConnection = DownloaderServiceMarshaller.CreateStub(this);
            this.serviceMessenger = this.serviceConnection.GetMessenger();

            var database = DownloadsDatabase.Instance;

            this.Control = database.DownloadStatus == DownloadStatus.PausedByApp
                               ? ControlAction.Paused
                               : ControlAction.Run;
        }

        #endregion

        #region Enums

        /// <summary>
        /// The network state.
        /// </summary>
        [Flags]
        protected enum NetworkState
        {
            /// <summary>
            /// The disconnected.
            /// </summary>
            Disconnected = 0, 

            /// <summary>
            /// The connected.
            /// </summary>
            Connected = 1, 

            /// <summary>
            /// The roaming.
            /// </summary>
            Roaming = 2, 

            /// <summary>
            /// The is 3 g.
            /// </summary>
            Is3G = 4, 

            /// <summary>
            /// The is 4 g.
            /// </summary>
            Is4G = 8, 

            /// <summary>
            /// The is cellular.
            /// </summary>
            IsCellular = 16, 

            /// <summary>
            /// The is fail over.
            /// </summary>
            IsFailOver = 32
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the number of bytes downloaded so far
        /// </summary>
        public long BytesSoFar { get; private set; }

        /// <summary>
        /// Gets the cntrol action.
        /// </summary>
        public ControlAction Control { get; private set; }

        /// <summary>
        /// Gets or sets the download state
        /// </summary>
        public DownloadStatus Status
        {
            get
            {
                return this.status;
            }

            set
            {
                this.status = value;

                var database = DownloadsDatabase.Instance;
                database.UpdateMetadata(this.packageInfo.VersionCode, this.status);
            }
        }

        /// <summary>
        /// Gets the total length of the downloads.
        /// </summary>
        public long TotalLength { get; private set; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets AlarmReceiverClassName.
        /// </summary>
        protected abstract string AlarmReceiverClassName { get; }

        /// <summary>
        /// Gets PublicKey.
        /// </summary>
        protected abstract string PublicKey { get; }

        /// <summary>
        /// Gets Salt.
        /// </summary>
        protected abstract byte[] Salt { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the service is running.
        /// Note: Only use this internally.
        /// </summary>
        private bool IsServiceRunning
        {
            get
            {
                lock (this.locker)
                {
                    return isRunning;
                }
            }

            set
            {
                lock (this.locker)
                {
                    isRunning = value;
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The start download service if required.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="intent">
        /// The intent.
        /// </param>
        /// <param name="serviceClass">
        /// The service class.
        /// </param>
        /// <returns>
        /// The start download service if required.
        /// </returns>
        public static DownloadServiceRequirement StartDownloadServiceIfRequired(
            Context context, Intent intent, Type serviceClass)
        {
            var pendingIntent = (PendingIntent)intent.GetParcelableExtra(DownloaderServiceExtras.PendingIntent);
            return StartDownloadServiceIfRequired(context, pendingIntent, serviceClass);
        }

        /// <summary>
        /// <para>
        /// Starts the download if necessary. 
        /// </para>
        /// <para>
        /// This function starts a flow that does many things:<br/>
        ///  (1) Checks to see if the APK version has been checked and the metadata database updated <br/>
        ///  (2) If the APK version does not match, checks the new LVL status to see if a new download is required <br/>
        ///  (3) If the APK version does match, then checks to see if the download(s) have been completed <br/>
        ///  (4) If the downloads have been completed, returns NO_DOWNLOAD_REQUIRED.
        /// </para>
        /// <para>
        /// The idea is that this can be called during the startup of an application to quickly
        /// ascertain if the application needs to wait to hear about any updated APK expansion files.
        /// </para>
        /// <para>
        /// Note: this does mean that the application MUST be run for the first time with a network connection, 
        /// even if Market delivers all of the files.
        /// </para>
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="pendingIntent">
        /// The pending Intent.
        /// </param>
        /// <param name="serviceClass">
        /// The service Class.
        /// </param>
        /// <returns>
        /// true if the app should wait for more guidance from the downloader, false if the app can continue
        /// </returns>
        public static DownloadServiceRequirement StartDownloadServiceIfRequired(
            Context context, PendingIntent pendingIntent, Type serviceClass)
        {
            // first: do we need to do an LVL update?
            // we begin by getting our APK version from the package manager
            PackageInfo pi = context.PackageManager.GetPackageInfo(context.PackageName, 0);

            DownloadServiceRequirement status = DownloadServiceRequirement.NoDownloadRequired;

            // the database automatically reads the metadata for version code
            // and download status when the instance is created
            DownloadsDatabase database = DownloadsDatabase.Instance;

            // we need to update the LVL check and get a successful status to proceed
            if (IsLvlCheckRequired(database, pi))
            {
                status = DownloadServiceRequirement.LvlCheckRequired;
            }

            // we don't have to update LVL. Do we still have a download to start?
            if (database.DownloadStatus == DownloadStatus.None)
            {
                var infos = database.GetDownloads();
                var existing = infos.Where(i => !Helpers.DoesFileExist(context, i.FileName, i.TotalBytes, true));

                if (existing.Count() > 0)
                {
                    status = DownloadServiceRequirement.DownloadRequired;
                    database.DownloadStatus = DownloadStatus.Unknown;
                }
            }
            else
            {
                status = DownloadServiceRequirement.DownloadRequired;
            }

            switch (status)
            {
                case DownloadServiceRequirement.DownloadRequired:
                case DownloadServiceRequirement.LvlCheckRequired:
                    var fileIntent = new Intent(context.ApplicationContext, serviceClass);
                    fileIntent.PutExtra(DownloaderServiceExtras.PendingIntent, pendingIntent);
                    context.StartService(fileIntent);
                    break;
            }

            return status;
        }

        /// <summary>
        /// Creates a filename (where the file should be saved) from info about a download.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <param name="filesize">
        /// The filesize.
        /// </param>
        /// <returns>
        /// The generate save file.
        /// </returns>
        public string GenerateSaveFile(string filename, long filesize)
        {
            string path = this.GenerateTempSaveFileName(filename);

            if (!Helpers.IsExternalMediaMounted)
            {
                Debug.WriteLine("External media not mounted: {0}", path);

                throw new GenerateSaveFileError(DownloadStatus.DeviceNotFoundError, "external media is not yet mounted");
            }

            if (File.Exists(path))
            {
                Debug.WriteLine("File already exists: {0}", path);

                throw new GenerateSaveFileError(
                    DownloadStatus.FileAlreadyExists, "requested destination file already exists");
            }

            if (Helpers.GetAvailableBytes(Helpers.GetFileSystemRoot(path)) < filesize)
            {
                throw new GenerateSaveFileError(
                    DownloadStatus.InsufficientSpaceError, "insufficient space on external storage");
            }

            return path;
        }

        /// <summary>
        /// Returns the filename (where the file should be saved) from info about a download
        /// </summary>
        /// <param name="fileName">
        /// The file Name.
        /// </param>
        /// <returns>
        /// The generate temp save file name.
        /// </returns>
        public string GenerateTempSaveFileName(string fileName)
        {
            return string.Format(
                "{0}{1}{2}{3}", 
                Helpers.GetSaveFilePath(this), 
                Path.DirectorySeparatorChar, 
                fileName, 
                TemporaryFileExtension);
        }

        /// <summary>
        /// a non-localized string appropriate for logging corresponding to one of the NETWORK_* constants.
        /// </summary>
        /// <param name="networkError">
        /// The network Error.
        /// </param>
        /// <returns>
        /// The get log message for network error.
        /// </returns>
        public string GetLogMessageForNetworkError(NetworkConstants networkError)
        {
            switch (networkError)
            {
                case NetworkConstants.RecommendedUnusableDueToSize:
                    return "download size exceeds recommended limit for mobile network";

                case NetworkConstants.UnusableDueToSize:
                    return "download size exceeds limit for mobile network";

                case NetworkConstants.NoConnection:
                    return "no network connection available";

                case NetworkConstants.CannotUseRoaming:
                    return "download cannot use the current network connection because it is roaming";

                case NetworkConstants.TypeDisallowedByRequestor:
                    return "download was requested to not use the current network type";

                default:
                    return "unknown error with network connectivity";
            }
        }

        /// <summary>
        /// Calculating a moving average for the speed so we don't get jumpy calculations for time etc.
        /// </summary>
        /// <param name="totalBytesSoFar">
        /// The total Bytes So Far.
        /// </param>
        public void NotifyUpdateBytes(long totalBytesSoFar)
        {
            long timeRemaining;
            long currentTime = SystemClock.UptimeMillis();
            if (0 != this.millisecondsAtSample)
            {
                // we have a sample.
                long timePassed = currentTime - this.millisecondsAtSample;
                long bytesInSample = totalBytesSoFar - this.bytesAtSample;
                float currentSpeedSample = bytesInSample / (float)timePassed;
                if (Math.Abs(0 - this.averageDownloadSpeed) > SmoothingFactor)
                {
                    var smoothSpeed = SmoothingFactor * currentSpeedSample;
                    var averageSpeed = (1 - SmoothingFactor) * this.averageDownloadSpeed;
                    this.averageDownloadSpeed = smoothSpeed + averageSpeed;
                }
                else
                {
                    this.averageDownloadSpeed = currentSpeedSample;
                }

                timeRemaining = (long)((this.TotalLength - totalBytesSoFar) / this.averageDownloadSpeed);
            }
            else
            {
                timeRemaining = -1;
            }

            this.millisecondsAtSample = currentTime;
            this.bytesAtSample = totalBytesSoFar;
            this.downloadNotification.OnDownloadProgress(
                new DownloadProgressInfo(this.TotalLength, totalBytesSoFar, timeRemaining, this.averageDownloadSpeed));
        }

        /// <summary>
        /// The on bind.
        /// </summary>
        /// <param name="intent">
        /// The intent.
        /// </param>
        /// <returns>
        /// the binder
        /// </returns>
        public override IBinder OnBind(Intent intent)
        {
            return this.serviceMessenger.Binder;
        }

        /// <summary>
        /// The on client updated.
        /// </summary>
        /// <param name="messenger">
        /// The client messenger.
        /// </param>
        public void OnClientUpdated(Messenger messenger)
        {
            this.clientMessenger = messenger;
            this.downloadNotification.SetMessenger(this.clientMessenger);
        }

        /// <summary>
        /// The on create.
        /// </summary>
        public override void OnCreate()
        {
            base.OnCreate();
            try
            {
                this.packageInfo = this.PackageManager.GetPackageInfo(this.PackageName, 0);
                string applicationLabel = this.PackageManager.GetApplicationLabel(this.ApplicationInfo);
                this.downloadNotification = new DownloadNotification(this, applicationLabel);
            }
            catch (PackageManager.NameNotFoundException e)
            {
                e.PrintStackTrace();
            }
        }

        /// <summary>
        /// The on destroy.
        /// </summary>
        public override void OnDestroy()
        {
            if (this.connectionReceiver != null)
            {
                this.UnregisterReceiver(this.connectionReceiver);
                this.connectionReceiver = null;
            }

            this.serviceConnection.Disconnect(this);
            base.OnDestroy();
        }

        /// <summary>
        /// The request abort download.
        /// </summary>
        public void RequestAbortDownload()
        {
            this.Control = ControlAction.Paused;
            this.Status = DownloadStatus.Canceled;
        }

        /// <summary>
        /// The request continue download.
        /// </summary>
        public void RequestContinueDownload()
        {
            Debug.WriteLine("RequestContinueDownload");

            if (this.Control == ControlAction.Paused)
            {
                this.Control = ControlAction.Run;
            }

            var fileIntent = new Intent(this, this.GetType());
            fileIntent.PutExtra(DownloaderServiceExtras.PendingIntent, this.pPendingIntent);
            this.StartService(fileIntent);
        }

        /// <summary>
        /// The request download status.
        /// </summary>
        public void RequestDownloadStatus()
        {
            this.downloadNotification.ResendState();
        }

        /// <summary>
        /// The request pause download.
        /// </summary>
        public void RequestPauseDownload()
        {
            this.Control = ControlAction.Paused;
            this.Status = DownloadStatus.PausedByApp;
        }

        /// <summary>
        /// The set download flags.
        /// </summary>
        /// <param name="flags">
        /// The flags.
        /// </param>
        public void SetDownloadFlags(DownloaderServiceFlags flags)
        {
            DownloadsDatabase.Instance.Flags = flags;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The get network availability state.
        /// </summary>
        /// <param name="database">
        /// The database.
        /// </param>
        /// <returns>
        /// </returns>
        internal NetworkConstants GetNetworkAvailabilityState(DownloadsDatabase database)
        {
            if (!this.networkState.HasFlag(NetworkState.Connected))
            {
                return NetworkConstants.NoConnection;
            }

            if (!this.networkState.HasFlag(NetworkState.IsCellular))
            {
                return NetworkConstants.Ok;
            }

            if (this.networkState.HasFlag(NetworkState.Roaming))
            {
                return NetworkConstants.CannotUseRoaming;
            }

            if (!database.Flags.HasFlag(DownloaderServiceFlags.FlagsDownloadOverCellular))
            {
                return NetworkConstants.TypeDisallowedByRequestor;
            }

            return NetworkConstants.Ok;
        }

        /// <summary>
        /// Updates the network type based upon the info returned from the 
        /// connectivity manager. 
        /// </summary>
        /// <param name="info">
        /// </param>
        /// <returns>
        /// </returns>
        protected virtual NetworkState GetNetworkState(NetworkInfo info)
        {
            var state = NetworkState.Disconnected;

            switch (info.Type)
            {
                case ConnectivityType.Wimax:
                    state = NetworkState.Is3G | NetworkState.Is3G | NetworkState.IsCellular;
                    break;

                case ConnectivityType.Mobile:
                    state = NetworkState.IsCellular;
                    switch ((NetworkType)info.Subtype)
                    {
                        case NetworkType.Hsdpa:
                        case NetworkType.Hsupa:
                        case NetworkType.Hspa:
                        case NetworkType.Evdo0:
                        case NetworkType.EvdoA:
                        case NetworkType.Umts:
                            state |= NetworkState.Is3G;
                            break;
                    }

                    break;
            }

            return state;
        }

        /// <summary>
        /// This is the main thread for the Downloader. 
        /// This thread is responsible for queuing up downloads and other goodness.
        /// </summary>
        /// <param name="intent">
        /// The intent that was recieved.
        /// </param>
        protected override void OnHandleIntent(Intent intent)
        {
            Debug.WriteLine("DownloaderService.OnHandleIntent");

            this.IsServiceRunning = true;
            try
            {
                // the database automatically reads the metadata for version code
                // and download status when the instance is created
                DownloadsDatabase database = DownloadsDatabase.Instance;
                var pendingIntent = (PendingIntent)intent.GetParcelableExtra(DownloaderServiceExtras.PendingIntent);

                if (null != pendingIntent)
                {
                    this.downloadNotification.PendingIntent = pendingIntent;
                    this.pPendingIntent = pendingIntent;
                }
                else if (null != this.pPendingIntent)
                {
                    this.downloadNotification.PendingIntent = this.pPendingIntent;
                }
                else
                {
                    Debug.WriteLine("LVLDL Downloader started in bad state without notification intent.");
                    return;
                }

                // when the LVL check completes, a successful response will update the service
                if (IsLvlCheckRequired(database, this.packageInfo))
                {
                    this.UpdateLvl(this);
                    return;
                }

                // get each download
                var infos = database.GetDownloads();
                this.BytesSoFar = 0;
                this.TotalLength = 0;
                this.fileCount = infos.Count();
                foreach (DownloadInfo info in infos)
                {
                    // We do an (simple) integrity check on each file, just to 
                    // make sure and to verify that the file matches the state
                    if (info.Status == DownloadStatus.Success
                        && !Helpers.DoesFileExist(this, info.FileName, info.TotalBytes, true))
                    {
                        info.Status = DownloadStatus.None;
                        info.CurrentBytes = 0;
                    }

                    // get aggregate data
                    this.TotalLength += info.TotalBytes;
                    this.BytesSoFar += info.CurrentBytes;
                }

                this.PollNetworkState();
                if (this.connectionReceiver == null)
                {
                    // We use this to track network state, such as when WiFi, Cellular, etc. is enabled
                    // when downloads are paused or in progress.
                    this.connectionReceiver = new InnerBroadcastReceiver(this);
                    var intentFilter = new IntentFilter(ConnectivityManager.ConnectivityAction);
                    intentFilter.AddAction(WifiManager.WifiStateChangedAction);
                    this.RegisterReceiver(this.connectionReceiver, intentFilter);
                }

                // loop through all downloads and fetch them
                var types = Enum.GetValues(typeof(ApkExpansionPolicy.ExpansionFileType)).Length;
                for (int index = 0; index < types; index++)
                {
                    DownloadInfo info = infos[index];
                    Debug.WriteLine("Starting download of " + info.FileName);

                    long startingCount = info.CurrentBytes;

                    if (info.Status != DownloadStatus.Success)
                    {
                        var dt = new DownloadThread(info, this, this.downloadNotification);
                        this.CancelAlarms();
                        this.ScheduleAlarm(ActiveThreadWatchdog);
                        dt.Run();
                        this.CancelAlarms();
                    }

                    database.UpdateFromDatabase(ref info);
                    bool setWakeWatchdog = false;
                    DownloaderClientState notifyStatus;
                    switch (info.Status)
                    {
                        case DownloadStatus.Forbidden:

                            // the URL is out of date
                            this.UpdateLvl(this);
                            return;
                        case DownloadStatus.Success:
                            this.BytesSoFar += info.CurrentBytes - startingCount;

                            if (index < infos.Count() - 1)
                            {
                                continue;
                            }

                            database.UpdateMetadata(this.packageInfo.VersionCode, DownloadStatus.None);
                            this.downloadNotification.OnDownloadStateChanged(DownloaderClientState.Completed);
                            return;
                        case DownloadStatus.FileDeliveredIncorrectly:

                            // we may be on a network that is returning us a web page on redirect
                            notifyStatus = DownloaderClientState.PausedNetworkSetupFailure;
                            info.CurrentBytes = 0;
                            database.UpdateDownload(info);
                            setWakeWatchdog = true;
                            break;
                        case DownloadStatus.PausedByApp:
                            notifyStatus = DownloaderClientState.PausedByRequest;
                            break;
                        case DownloadStatus.WaitingForNetwork:
                        case DownloadStatus.WaitingToRetry:
                            notifyStatus = DownloaderClientState.PausedNetworkUnavailable;
                            setWakeWatchdog = true;
                            break;
                        case DownloadStatus.QueuedForWifi:

                            // look for more detail here
                            notifyStatus = this.wifiManager != null && !this.wifiManager.IsWifiEnabled
                                               ? DownloaderClientState.PausedWifiDisabledNeedCellularPermission
                                               : DownloaderClientState.PausedNeedCellularPermission;
                            setWakeWatchdog = true;
                            break;
                        case DownloadStatus.Canceled:
                            notifyStatus = DownloaderClientState.FailedCanceled;
                            setWakeWatchdog = true;
                            break;

                        case DownloadStatus.InsufficientSpaceError:
                            notifyStatus = DownloaderClientState.FailedSdCardFull;
                            setWakeWatchdog = true;
                            break;

                        case DownloadStatus.DeviceNotFoundError:
                            notifyStatus = DownloaderClientState.PausedSdCardUnavailable;
                            setWakeWatchdog = true;
                            break;

                        default:
                            notifyStatus = DownloaderClientState.Failed;
                            break;
                    }

                    if (setWakeWatchdog)
                    {
                        this.ScheduleAlarm(WatchdogWakeTimer);
                    }
                    else
                    {
                        this.CancelAlarms();
                    }

                    // failure or pause state
                    this.downloadNotification.OnDownloadStateChanged(notifyStatus);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            finally
            {
                this.IsServiceRunning = false;
            }
        }

        /// <summary>
        /// Returns a value indicating whether the downloader should stop. 
        /// This will return True if all the downloads are complete.
        /// </summary>
        /// <returns>
        /// True if the downloader should stop.
        /// </returns>
        protected override bool ShouldStop()
        {
            // the database automatically reads the metadata for version code 
            // and download status when the instance is created
            return DownloadsDatabase.Instance.DownloadStatus == DownloadStatus.None;
        }

        /// <summary>
        /// The cancel alarms.
        /// </summary>
        private void CancelAlarms()
        {
            if (null != this.alarmIntent)
            {
                var alarms = this.GetSystemService(AlarmService).JavaCast<AlarmManager>();
                if (alarms == null)
                {
                    Debug.WriteLine("LVLDL couldn't get alarm manager");
                    return;
                }

                alarms.Cancel(this.alarmIntent);
                this.alarmIntent = null;
            }
        }

        /// <summary>
        /// The APK has been updated and a filename has been sent down from the
        /// Market call. If the file has the same name as the previous file, we do
        /// nothing as the file is guaranteed to be the same. If the file does not
        /// have the same name, we download it if it hasn't already been delivered by
        /// Market.
        /// </summary>
        /// <param name="database">
        /// The database.
        /// </param>
        /// <param name="filename">
        /// the name of the new file
        /// </param>
        /// <param name="fileSize">
        /// the size of the new file
        /// </param>
        /// <returns>
        /// The handle file updated.
        /// </returns>
        private bool HandleFileUpdated(DownloadsDatabase database, string filename, long fileSize)
        {
            DownloadInfo di = database.GetDownloadInfo(filename);

            if (di != null && di.FileName != null)
            {
                if (filename == di.FileName)
                {
                    return false;
                }

                // remove partially downloaded file if it is there
                string deleteFile = Helpers.GenerateSaveFileName(this, di.FileName);
                if (File.Exists(deleteFile))
                {
                    File.Delete(deleteFile);
                }
            }

            return !Helpers.DoesFileExist(this, filename, fileSize, true);
        }

        /// <summary>
        /// Polls the network state, setting the flags appropriately.
        /// </summary>
        private void PollNetworkState()
        {
            if (this.connectivityManager == null)
            {
                this.connectivityManager = this.GetSystemService(ConnectivityService).JavaCast<ConnectivityManager>();
            }

            if (this.wifiManager == null)
            {
                this.wifiManager = this.GetSystemService(WifiService).JavaCast<WifiManager>();
            }

            if (this.connectivityManager == null)
            {
                Debug.WriteLine("LVLDL couldn't get connectivity manager to poll network state");
            }
            else
            {
                NetworkInfo activeInfo = this.connectivityManager.ActiveNetworkInfo;
                this.UpdateNetworkState(activeInfo);
            }
        }

        /// <summary>
        /// The schedule alarm.
        /// </summary>
        /// <param name="wakeUp">
        /// The wake up.
        /// </param>
        private void ScheduleAlarm(int wakeUp)
        {
            var alarms = this.GetSystemService(AlarmService).JavaCast<AlarmManager>();
            if (alarms == null)
            {
                Debug.WriteLine("LVLDL couldn't get alarm manager");
                return;
            }

            Calendar cal = Calendar.Instance;
            cal.Add(CalendarField.Second, wakeUp);

            Debug.WriteLine("LVLDL scheduling retry in {0} seconds ({1})", wakeUp, cal.Time.ToLocaleString());

            var intent = new Intent(DownloaderSeviceActions.ActionRetry);
            intent.PutExtra(DownloaderServiceExtras.PendingIntent, this.pPendingIntent);
            intent.SetClassName(this.PackageName, this.AlarmReceiverClassName);
            this.alarmIntent = PendingIntent.GetBroadcast(this, 0, intent, PendingIntentFlags.OneShot);
            alarms.Set(AlarmType.RtcWakeup, cal.TimeInMillis, this.alarmIntent);
        }

        /// <summary>
        /// Updates the LVL information from the server.
        /// </summary>
        /// <param name="context">
        /// </param>
        private void UpdateLvl(DownloaderService context)
        {
            var h = new Handler(context.MainLooper);
            h.Post(new LvlRunnable(context, this.pPendingIntent));
        }

        /// <summary>
        /// The update network state.
        /// </summary>
        /// <param name="info">
        /// The info.
        /// </param>
        private void UpdateNetworkState(NetworkInfo info)
        {
            NetworkState tempState = this.networkState;

            this.networkState = NetworkState.Disconnected;

            if (info != null && info.IsConnected)
            {
                this.networkState = NetworkState.Connected;

                if (info.IsRoaming)
                {
                    this.networkState |= NetworkState.Roaming;
                }

                if (info.IsFailover)
                {
                    this.networkState |= NetworkState.IsFailOver;
                }

                this.networkState |= this.GetNetworkState(info);
            }

            this.stateChanged = this.stateChanged || this.networkState != tempState;

            if (this.stateChanged)
            {
                Debug.WriteLine("LVLDL Network state changed: ");
                Debug.WriteLine("LVLDL Starting State: {0}", tempState);
                Debug.WriteLine("LVLDL Ending State: {0}", this.networkState);

                if (this.IsServiceRunning)
                {
                    if (this.networkState.HasFlag(NetworkState.Roaming))
                    {
                        this.Status = DownloadStatus.WaitingForNetwork;
                        this.Control = ControlAction.Paused;
                    }
                    else if (this.networkState.HasFlag(NetworkState.IsCellular))
                    {
                        DownloadsDatabase database = DownloadsDatabase.Instance;
                        DownloaderServiceFlags flags = database.Flags;
                        if (!flags.HasFlag(DownloaderServiceFlags.FlagsDownloadOverCellular))
                        {
                            this.Status = DownloadStatus.QueuedForWifi;
                            this.Control = ControlAction.Paused;
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Exception thrown from methods called by generateSaveFile() for any fatal
        /// error.
        /// </summary>
        public class GenerateSaveFileError : Exception
        {
            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="GenerateSaveFileError"/> class.
            /// </summary>
            /// <param name="status">
            /// The status.
            /// </param>
            /// <param name="message">
            /// The message.
            /// </param>
            public GenerateSaveFileError(DownloadStatus status, string message)
                : base(message)
            {
                this.Status = status;
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// Gets the status.
            /// </summary>
            public DownloadStatus Status { get; private set; }

            #endregion
        }

        /// <summary>
        /// We use this to track network state, such as when WiFi, Cellular, etc. is
        /// enabled when downloads are paused or in progress.
        /// </summary>
        private class InnerBroadcastReceiver : BroadcastReceiver
        {
            #region Constants and Fields

            /// <summary>
            /// The m service.
            /// </summary>
            private readonly DownloaderService service;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="InnerBroadcastReceiver"/> class.
            /// </summary>
            /// <param name="service">
            /// The service.
            /// </param>
            internal InnerBroadcastReceiver(DownloaderService service)
            {
                this.service = service;
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The on receive.
            /// </summary>
            /// <param name="context">
            /// The context.
            /// </param>
            /// <param name="intent">
            /// The intent.
            /// </param>
            public override void OnReceive(Context context, Intent intent)
            {
                this.service.PollNetworkState();
                if (this.service.stateChanged && !this.service.IsServiceRunning)
                {
                    Debug.WriteLine("LVLDL InnerBroadcastReceiver Called");
                    var fileIntent = new Intent(context, this.service.GetType());
                    fileIntent.PutExtra(DownloaderServiceExtras.PendingIntent, this.service.pPendingIntent);

                    // send a new intent to the service
                    context.StartService(fileIntent);
                }
            }

            #endregion
        }
    }
}