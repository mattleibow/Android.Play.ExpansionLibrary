using System;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Telephony;
using Android.Util;
using Java.Lang;
using LicenseVerificationLibrary;
using Debug = System.Diagnostics.Debug;
using Exception = System.Exception;
using File = Java.IO.File;
using Object = Java.Lang.Object;
using String = System.String;

namespace ExpansionDownloader.impl
{
    public abstract class DownloaderService : CustomIntentService, IDownloaderService
    {

        /** Tag used for debugging/logging */
        public static string TAG = "LVLDL";

        /// <summary>
        /// Expansion path where we store obb files.
        /// </summary>
        public static string ExpansionPath = String.Format("{0}Android{0}obb{0}", Path.DirectorySeparatorChar);

        /// <summary>
        /// The intent that gets sent when the service must wake up for a retry.
        /// </summary>
        public static string ActionRetry = "android.intent.action.DOWNLOAD_WAKEUP";

        /// <summary>
        /// The intent that gets sent when clicking a successful download
        /// </summary>
        public static string ActionOpen = "android.intent.action.DOWNLOAD_OPEN";

        /// <summary>
        /// The intent that gets sent when clicking an incomplete/failed download
        /// </summary>
        public static string ActionList = "android.intent.action.DOWNLOAD_LIST";

        /// <summary>
        /// The intent that gets sent when deleting the notification of a completed download
        /// </summary>
        public static string ActionHide = "android.intent.action.DOWNLOAD_HIDE";

        /// <summary>
        /// When a number has to be appended to the filename, this string is
        /// used to separate the base filename from the sequence number.
        /// </summary>
        public static string FILENAME_SEQUENCE_SEPARATOR = "-";

        /// <summary>
        /// The default user agent used for downloads.
        /// </summary>
        public static string DefaultUserAgent = "Android.LVLDM";

        /// <summary>
        /// The buffer size used to stream the data.
        /// </summary>
        public static int BufferSize = 4096;

        /// <summary>
        /// The minimum amount of progress that has to be done before the 
        /// progress bar gets updated.
        /// </summary>
        public static int MinimumProgressStep = 4096;

        /// <summary>
        /// The minimum amount of time that has to elapse before the progress 
        /// bar gets updated, in milliseconds.
        /// </summary>
        public static long MinimumProgressTime = 1000;

        /// <summary>
        /// The maximum number of rows in the database (FIFO).
        /// </summary>
        public static int MaximumDownloads = 1000;

        /**
     * The number of times that the download manager will retry its network
     * operations when no progress is happening before it gives up.
     */
        public static int MaximumRetries = 5;

        /**
     * The minimum amount of time that the download manager accepts for
     * a Retry-After response header with a parameter in delta-seconds.
     */
        public static int MinimumRetryAfter = 30; // 30s

        /**
     * The maximum amount of time that the download manager accepts for
     * a Retry-After response header with a parameter in delta-seconds.
     */
        public static int MAX_RETRY_AFTER = 24 * 60 * 60; // 24h

        /**
     * The maximum number of redirects.
     */
        public static int MAX_REDIRECTS = 5; // can't be more than 7.

        /**
     * The time between a failure and the first retry after an IOException.
     * Each subsequent retry grows exponentially, doubling each time.
     * The time is in seconds.
     */
        public static int RETRY_FIRST_DELAY = 30;
        /**
     * The wake duration to check to see if a download is possible.
     */
        public static long WATCHDOG_WAKE_TIMER = 60 * 1000;

        /**
     * The wake duration to check to see if the process was killed.
     */
        public static long ACTIVE_THREAD_WATCHDOG = 5 * 1000;

        /**
     * For intents used to notify the user that a download exceeds a size
     * threshold, if this extra is true, WiFi is required for this download
     * size; otherwise, it is only recommended.
     */
        public const string EXTRA_IS_WIFI_REQUIRED = "isWifiRequired";
        public const string EXTRA_FILE_NAME = "downloadId";

        /**
     * Used with DOWNLOAD_STATUS
     */
        public const string EXTRA_STATUS_STATE = "ESS";
        public const string EXTRA_STATUS_TOTAL_SIZE = "ETS";
        public const string EXTRA_STATUS_CURRENT_FILE_SIZE = "CFS";
        public const string EXTRA_STATUS_TOTAL_PROGRESS = "TFP";
        public const string EXTRA_STATUS_CURRENT_PROGRESS = "CFP";


        public const string ACTION_DOWNLOADS_CHANGED = "downloadsChanged";

        /**
     * Broadcast intent action sent by the download manager when a download completes.
     */
        public const string ACTION_DOWNLOAD_COMPLETE = "lvldownloader.intent.action.DOWNLOAD_COMPLETE";

        /**
     * Broadcast intent action sent by the download manager when download status changes.
     */
        public const string ACTION_DOWNLOAD_STATUS = "lvldownloader.intent.action.DOWNLOAD_STATUS";

        /**
     * This download is allowed to run.
     * 
     * @hide
     */
        public const int CONTROL_RUN = 0;

        /**
     * This download must pause at the first opportunity.
     * 
     * @hide
     */
        public const int CONTROL_PAUSED = 1;

        /**
     * This download is visible but only shows in the notifications while it's in progress.
     * 
     * @hide
     */
        public const int VISIBILITY_VISIBLE = 0;

        /**
     * This download is visible and shows in the notifications while in progress and after completion.
     * 
     * @hide
     */
        public const int VISIBILITY_VISIBLE_NOTIFY_COMPLETED = 1;

        /**
     * This download doesn't show in the UI or in the notifications.
     * 
     * @hide
     */
        public const int VISIBILITY_HIDDEN = 2;

        /**
     * Bit flag for {@link #setAllowedNetworkTypes} corresponding to {@link ConnectivityManager#TYPE_MOBILE}.
     */
        public const int NETWORK_MOBILE = 1 << 0;

        /**
     * Bit flag for {@link #setAllowedNetworkTypes} corresponding to {@link ConnectivityManager#TYPE_WIFI}.
     */
        public const int NETWORK_WIFI = 1 << 1;
        public const int NO_DOWNLOAD_REQUIRED = 0;
        public const int LVL_CHECK_REQUIRED = 1;
        public const int DOWNLOAD_REQUIRED = 2;

        public const string EXTRA_PACKAGE_NAME = "EPN";
        public const string EXTRA_PENDING_INTENT = "EPI";
        public const string EXTRA_MESSAGE_HANDLER = "EMH";
        private static string LOG_TAG = "LVLDL";

        private static string TemporaryFileExtension = ".tmp";

        /**
     * Service thread status
     */
        private static bool sIsRunning;
        private static float SMOOTHING_FACTOR = 0.005f;
        private readonly object _locker = new object();
        private readonly Messenger mServiceMessenger;
        private readonly IStub mServiceStub;
        private PendingIntent mAlarmIntent;
        private float mAverageDownloadSpeed;
        private long mBytesAtSample;
        public long mBytesSoFar;
        private Messenger mClientMessenger;
        private BroadcastReceiver mConnReceiver;
        private ConnectivityManager mConnectivityManager;
        private int mControl;
        private int mFileCount;
        private bool mIsAtLeast3G;
        private bool mIsAtLeast4G;
        private bool mIsCellularConnection;

        /**
     * Network state.
     */
        private bool mIsConnected;
        private bool mIsFailover;
        private bool mIsRoaming;
        private long mMillisecondsAtSample;
        private DownloadNotification mNotification;
        private PackageInfo mPackageInfo;
        private PendingIntent mPendingIntent;
        private bool mStateChanged;
        private int mStatus;
        public long mTotalLength;
        private WifiManager mWifiManager;

        public DownloaderService()
            : base("LVLDownloadService")
        {
            Log.Info(LOG_TAG, "DownloaderService()");
            
            mServiceStub = DownloaderServiceMarshaller.CreateStub(this);
            mServiceMessenger = mServiceStub.GetMessenger();
        }

        #region IDownloaderService Members

        public void RequestAbortDownload()
        {
            mControl = CONTROL_PAUSED;
            mStatus = DownloadStatus.Canceled;
        }

        public void RequestPauseDownload()
        {
            mControl = CONTROL_PAUSED;
            mStatus = DownloadStatus.PausedByApp;
        }

        public void SetDownloadFlags(DownloaderServiceFlags flags)
        {
            DownloadsDB.getDB(this).UpdateFlags(flags);
        }

        public void RequestContinueDownload()
        {
            if (mControl == CONTROL_PAUSED)
            {
                mControl = CONTROL_RUN;
            }
            var fileIntent = new Intent(this, GetType());
            fileIntent.PutExtra(EXTRA_PENDING_INTENT, mPendingIntent);
            StartService(fileIntent);
        }

        public void RequestDownloadStatus()
        {
            mNotification.resendState();
        }

        public void OnClientUpdated(Messenger clientMessenger)
        {
            mClientMessenger = clientMessenger;
            mNotification.setMessenger(mClientMessenger);
        }

        #endregion

        /// <summary>
        /// Returns whether the status is informational (i.e. 1xx).
        /// </summary>
        public static bool IsStatusInformational(int status)
        {
            return (status >= 100 && status < 200);
        }

        /**
     * Returns whether the status is a success (i.e. 2xx).
     */

        public static bool isStatusSuccess(int status)
        {
            return (status >= 200 && status < 300);
        }

        /**
     * Returns whether the status is an error (i.e. 4xx or 5xx).
     */

        public static bool isStatusError(int status)
        {
            return (status >= 400 && status < 600);
        }

        /**
     * Returns whether the status is a client error (i.e. 4xx).
     */

        public static bool isStatusClientError(int status)
        {
            return (status >= 400 && status < 500);
        }

        /**
     * Returns whether the status is a server error (i.e. 5xx).
     */

        public static bool isStatusServerError(int status)
        {
            return (status >= 500 && status < 600);
        }

        /**
     * Returns whether the download has completed (either with success or
     * error).
     */

        public static bool isStatusCompleted(int status)
        {
            return (status >= 200 && status < 300) || (status >= 400 && status < 600);
        }

        public override IBinder OnBind(Intent paramIntent)
        {
            Log.Debug(TAG, "Service Bound");
            return mServiceMessenger.Binder;
        }

        public bool isWiFi()
        {
            return mIsConnected && !mIsCellularConnection;
        }

        /**
     * Updates the network type based upon the type and subtype returned from
     * the connectivity manager. Subtype is only used for cellular signals.
     * 
     * @param type
     * @param subType
     */

        private void updateNetworkType(int type, int subType)
        {
            switch ((ConnectivityType) type)
            {
                case ConnectivityType.Wifi:
                //case ConnectivityType.Ethernet:
                //case ConnectivityType.Bluetooth:
                    mIsCellularConnection = false;
                    mIsAtLeast3G = false;
                    mIsAtLeast4G = false;
                    break;
                case ConnectivityType.Wimax:
                    mIsCellularConnection = true;
                    mIsAtLeast3G = true;
                    mIsAtLeast4G = true;
                    break;
                case ConnectivityType.Mobile:
                    mIsCellularConnection = true;
                    switch ((NetworkType) subType)
                    {
                        case NetworkType.OneXrtt:
                        case NetworkType.Cdma:
                        case NetworkType.Edge:
                        case NetworkType.Gprs:
                        case NetworkType.Iden:
                            mIsAtLeast3G = false;
                            mIsAtLeast4G = false;
                            break;
                        case NetworkType.Hsdpa:
                        case NetworkType.Hsupa:
                        case NetworkType.Hspa:
                        case NetworkType.Evdo0:
                        case NetworkType.EvdoA:
                        case NetworkType.Umts:
                            mIsAtLeast3G = true;
                            mIsAtLeast4G = false;
                            break;
                        //case NetworkType.Lte: // 4G
                        //case NetworkType.Ehrpd: // 3G ++ interop with 4G
                        //case NetworkType.Hspap: // 3G ++ but marketed as 4G
                        //    mIsAtLeast3G = true;
                        //    mIsAtLeast4G = true;
                        //    break;
                        default:
                            mIsCellularConnection = false;
                            mIsAtLeast3G = false;
                            mIsAtLeast4G = false;
                            break;
                    }
                    break;
            }
        }

        private void updateNetworkState(NetworkInfo info)
        {
            bool isConnected = mIsConnected;
            bool isFailover = mIsFailover;
            bool isCellularConnection = mIsCellularConnection;
            bool isRoaming = mIsRoaming;
            bool isAtLeast3G = mIsAtLeast3G;
            if (info == null)
            {
                mIsRoaming = false;
                mIsFailover = false;
                mIsConnected = false;
                updateNetworkType(-1, -1);
            }
            else
            {
                mIsRoaming = info.IsRoaming;
                mIsFailover = info.IsFailover;
                mIsConnected = info.IsConnected;
                updateNetworkType((int)info.Type, (int)info.Subtype);
            }

            mStateChanged = (mStateChanged ||
                             isConnected != mIsConnected ||
                             isFailover != mIsFailover ||
                             isCellularConnection != mIsCellularConnection ||
                             isRoaming != mIsRoaming ||
                             isAtLeast3G != mIsAtLeast3G);

            if (mStateChanged)
            {
                Log.Verbose(LOG_TAG, "Network state changed: ");
                Log.Verbose(LOG_TAG, "Starting State: " +
                                     (isConnected ? "Connected " : "Not Connected ") +
                                     (isCellularConnection ? "Cellular " : "WiFi ") +
                                     (isRoaming ? "Roaming " : "Local ") +
                                     (isAtLeast3G ? "3G+ " : "<3G "));
                Log.Verbose(LOG_TAG, "Ending State: " +
                                     (mIsConnected ? "Connected " : "Not Connected ") +
                                     (mIsCellularConnection ? "Cellular " : "WiFi ") +
                                     (mIsRoaming ? "Roaming " : "Local ") +
                                     (mIsAtLeast3G ? "3G+ " : "<3G "));

                if (isServiceRunning())
                {
                    if (mIsRoaming)
                    {
                        mStatus = DownloadStatus.WaitingForNetwork;
                        mControl = CONTROL_PAUSED;
                    }
                    else if (mIsCellularConnection)
                    {
                        DownloadsDB db = DownloadsDB.getDB(this);
                        DownloaderServiceFlags flags = db.getFlags();
                        if (!flags.HasFlag(DownloaderServiceFlags.FlagsDownloadOverCellular))
                        {
                            mStatus = DownloadStatus.QueuedForWifi;
                            mControl = CONTROL_PAUSED;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Polls the network state, setting the flags appropriately.
        /// </summary>
        private void PollNetworkState()
        {
            if (null == mConnectivityManager)
            {
                mConnectivityManager = GetSystemService(ConnectivityService).JavaCast<ConnectivityManager>();
            }
            if (null == mWifiManager)
            {
                mWifiManager = GetSystemService(WifiService).JavaCast<WifiManager>();
            }
            if (mConnectivityManager == null)
            {
                Log.Warn(TAG, "couldn't get connectivity manager to poll network state");
            }
            else
            {
                NetworkInfo activeInfo = mConnectivityManager.ActiveNetworkInfo;
                updateNetworkState(activeInfo);
            }
        }

        /**
     * Returns true if the LVL check is required
     * 
     * @param db a downloads DB synchronized with the latest state
     * @param pi the package info for the project
     * @return returns true if the filenames need to be returned
     */

        private static bool isLVLCheckRequired(DownloadsDB db, PackageInfo pi)
        {
            // we need to update the LVL check and get a successful status to
            // proceed
            if (db.mVersionCode != pi.VersionCode)
            {
                return true;
            }
            return false;
        }

        /**
     * Careful! Only use this internally.
     * 
     * @return whether we think the service is running
     */

        private bool isServiceRunning()
        {
            lock (_locker)
                return sIsRunning;
        }

        private void setServiceRunning(bool isRunning)
        {
            lock (_locker)
                sIsRunning = isRunning;
        }

        public static int StartDownloadServiceIfRequired(Context context, Intent intent, Type serviceClass)
        {
            var pendingIntent = (PendingIntent) intent.GetParcelableExtra(EXTRA_PENDING_INTENT);
            return StartDownloadServiceIfRequired(context, pendingIntent, serviceClass);
        }

        /// <summary>
        /// Starts the download if necessary. 
        /// 
        /// This function starts a flow that does many things:
        ///  (1) Checks to see if the APK version has been checked and the metadata database updated 
        ///  (2) If the APK version does not match, checks the new LVL status to see if a new download is required 
        ///  (3) If the APK version does match, then checks to see if the download(s) have been completed
        ///  (4) If the downloads have been completed, returns NO_DOWNLOAD_REQUIRED.
        /// 
        /// The idea is that this can be called during the startup of an application to quickly
        /// ascertain if the application needs to wait to hear about any updated APK expansion files.
        /// 
        /// Note that this does mean that the application MUST be run for the first time with a network connection, 
        /// even if Market delivers all of the files.
        /// </summary>
        /// <returns>
        ///  true if the app should wait for more guidance from the downloader, false if the app can continue
        /// </returns>
        public static int StartDownloadServiceIfRequired(Context context, PendingIntent pendingIntent, Type serviceClass)
        {
            System.Diagnostics.Debug.WriteLine("StartDownloadServiceIfRequired");

            // first: do we need to do an LVL update?
            // we begin by getting our APK version from the package manager
            PackageInfo pi = context.PackageManager.GetPackageInfo(context.PackageName, 0);

            int status = NO_DOWNLOAD_REQUIRED;

            // the database automatically reads the metadata for version code
            // and download status when the instance is created
            DownloadsDB db = DownloadsDB.getDB(context);

            // we need to update the LVL check and get a successful status to proceed
            if (isLVLCheckRequired(db, pi))
            {
                status = LVL_CHECK_REQUIRED;
            }

            // we don't have to update LVL. do we still have a download to start?
            if (db.mStatus == 0)
            {
                DownloadInfo[] infos = db.GetDownloads();
                if (infos != null && infos.Any(i => !Helpers.DoesFileExist(context, i.FileName, i.TotalBytes, true)))
                {
                    status = DOWNLOAD_REQUIRED;
                    db.updateStatus(-1);
                }
            }
            else
            {
                status = DOWNLOAD_REQUIRED;
            }

            switch (status)
            {
                case DOWNLOAD_REQUIRED:
                case LVL_CHECK_REQUIRED:
                    System.Diagnostics.Debug.WriteLine("StartService: " + serviceClass);
                    var fileIntent = new Intent(context.ApplicationContext, serviceClass);
                    fileIntent.PutExtra(EXTRA_PENDING_INTENT, pendingIntent);
                    context.StartService(fileIntent);
                    break;
            }

            return status;
        }

        public abstract string GetPublicKey(); // Your public licensing key.

        public abstract byte[] GetSalt();

        public abstract string GetAlarmReceiverClassName();

       /// <summary>
        /// Updates the LVL information from the server.
       /// </summary>
       /// <param name="context"></param>
        public void UpdateLvl(DownloaderService context)
        {
            Debug.WriteLine("DownloaderService.UpdateLvl");
            var h = new Handler(context.MainLooper);
            h.Post(new LvlRunnable(context, mPendingIntent));
        }

        /**
     * The APK has been updated and a filename has been sent down from the
     * Market call. If the file has the same name as the previous file, we do
     * nothing as the file is guaranteed to be the same. If the file does not
     * have the same name, we download it if it hasn't already been delivered by
     * Market.
     * 
     * @param index the index of the file from market (0 = main, 1 = patch)
     * @param filename the name of the new file
     * @param fileSize the size of the new file
     * @return
     */

        public bool HandleFileUpdated(DownloadsDB db, int index, string filename, long fileSize)
        {
            DownloadInfo di = db.getDownloadInfoByFileName(filename);
            if (null != di)
            {
                string oldFile = di.FileName;
                // cleanup
                if (null != oldFile)
                {
                    if (filename == oldFile)
                    {
                        return false;
                    }

                    // remove partially downloaded file if it is there
                    string deleteFile = Helpers.GenerateSaveFileName(this, oldFile);
                    var f = new File(deleteFile);
                    if (f.Exists())
                        f.Delete();
                }
            }
            return !Helpers.DoesFileExist(this, filename, fileSize, true);
        }

        private void ScheduleAlarm(long wakeUp)
        {
            var alarms = GetSystemService(AlarmService).JavaCast<AlarmManager>();
            if (alarms == null)
            {
                Log.Error(TAG, "couldn't get alarm manager");
                return;
            }

                Log.Verbose(TAG, "scheduling retry in " + wakeUp + "ms");

            string className = GetAlarmReceiverClassName();
            var intent = new Intent(ActionRetry);
            intent.PutExtra(EXTRA_PENDING_INTENT, mPendingIntent);
            intent.SetClassName(PackageName, className);
            mAlarmIntent = PendingIntent.GetBroadcast(this, 0, intent, PendingIntentFlags.OneShot);
            alarms.Set(AlarmType.RtcWakeup, PolicyExtensions.GetCurrentMilliseconds() + wakeUp, mAlarmIntent);
        }

        private void CancelAlarms()
        {
            if (null != mAlarmIntent)
            {
                var alarms = GetSystemService(AlarmService).JavaCast<AlarmManager>();
                if (alarms == null)
                {
                    Log.Error(TAG, "couldn't get alarm manager");
                    return;
                }
                alarms.Cancel(mAlarmIntent);
                mAlarmIntent = null;
            }
        }

        /**
     * This is the main thread for the Downloader. This thread is responsible
     * for queuing up downloads and other goodness.
     */

        protected override void OnHandleIntent(Intent intent)
        {
            Debug.WriteLine("DownloaderService.OnHandleIntent");

            setServiceRunning(true);
            try
            {
                // the database automatically reads the metadata for version code
                // and download status when the instance is created
                DownloadsDB db = DownloadsDB.getDB(this);
                var pendingIntent = (PendingIntent)intent.GetParcelableExtra(EXTRA_PENDING_INTENT);

                if (null != pendingIntent)
                {
                    mNotification.setClientIntent(pendingIntent);
                    mPendingIntent = pendingIntent;
                }
                else if (null != mPendingIntent)
                {
                    mNotification.setClientIntent(mPendingIntent);
                }
                else
                {
                    Log.Error(LOG_TAG, "Downloader started in bad state without notification intent.");
                    return;
                }

                // when the LVL check completes, a successful response will update the service
                if (isLVLCheckRequired(db, mPackageInfo))
                {
                    UpdateLvl(this);
                    return;
                }

                // get each download
                DownloadInfo[] infos = db.GetDownloads();
                mBytesSoFar = 0;
                mTotalLength = 0;
                mFileCount = infos.Length;
                foreach (DownloadInfo info in infos)
                {
                    // We do an (simple) integrity check on each file, just to 
                    // make sure and to verify that the file matches the state
                    if (info.Status == DownloadStatus.Success &&
                        !Helpers.DoesFileExist(this, info.FileName, info.TotalBytes, true))
                    {
                        info.Status = 0;
                        info.CurrentBytes = 0;
                    }

                    // get aggregate data
                    mTotalLength += info.TotalBytes;
                    mBytesSoFar += info.CurrentBytes;
                }

                PollNetworkState();
                if (mConnReceiver == null)
                {
                    // We use this to track network state, such as when WiFi, Cellular, etc. is enabled
                    // when downloads are paused or in progress.
                    mConnReceiver = new InnerBroadcastReceiver(this);
                    var intentFilter = new IntentFilter(ConnectivityManager.ConnectivityAction);
                    intentFilter.AddAction(WifiManager.WifiStateChangedAction);
                    RegisterReceiver(mConnReceiver, intentFilter);
                }

                // loop through all downloads and fetch them
                for (int index = 0; index < infos.Length; index++)
                {
                    DownloadInfo info = infos[index];
                    Debug.WriteLine("Starting download of " + info.FileName);

                    long startingCount = info.CurrentBytes;

                    if (info.Status != DownloadStatus.Success)
                    {
                        var dt = new DownloadThread(info, this, this.mNotification);
                        this.CancelAlarms();
                        this.ScheduleAlarm(ACTIVE_THREAD_WATCHDOG);
                        dt.Run();
                        this.CancelAlarms();
                    }

                    db.updateFromDb(info);
                    bool setWakeWatchdog = false;
                    DownloaderClientState notifyStatus;
                    switch (info.Status)
                    {
                        case DownloadStatus.Forbidden:
                            // the URL is out of date
                            this.UpdateLvl(this);
                            return;
                        case DownloadStatus.Success:
                            this.mBytesSoFar += info.CurrentBytes - startingCount;
                            db.updateMetadata(this.mPackageInfo.VersionCode, 0);

                            if (index < infos.Length - 1) continue;

                            this.mNotification.OnDownloadStateChanged(DownloaderClientState.Completed);
                            return;
                        case DownloadStatus.FileDeliveredIncorrectly:
                            // we may be on a network that is returning us a web page on redirect
                            notifyStatus = DownloaderClientState.STATE_PAUSED_NETWORK_SETUP_FAILURE;
                            info.CurrentBytes = 0;
                            db.updateDownload(info);
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
                            notifyStatus = this.mWifiManager != null && !this.mWifiManager.IsWifiEnabled
                                               ? DownloaderClientState.PausedWifiDisabledNeedCellularPermission
                                               : DownloaderClientState.PausedNeedCellularPermission;
                            setWakeWatchdog = true;
                            break;
                        case DownloadStatus.Canceled:
                            notifyStatus = DownloaderClientState.STATE_FAILED_CANCELED;
                            setWakeWatchdog = true;
                            break;

                        case DownloadStatus.InsufficientSpaceError:
                            notifyStatus = DownloaderClientState.STATE_FAILED_SDCARD_FULL;
                            setWakeWatchdog = true;
                            break;

                        case DownloadStatus.DeviceNotFoundError:
                            notifyStatus = DownloaderClientState.STATE_PAUSED_SDCARD_UNAVAILABLE;
                            setWakeWatchdog = true;
                            break;

                        default:
                            notifyStatus = DownloaderClientState.STATE_FAILED;
                            break;
                    }
                    if (setWakeWatchdog)
                    {
                        this.ScheduleAlarm(WATCHDOG_WAKE_TIMER);
                    }
                    else
                    {
                        this.CancelAlarms();
                    }
                    // failure or pause state
                    this.mNotification.OnDownloadStateChanged(notifyStatus);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Some blasted exception was thrown somewhere...");
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            finally
            {
                setServiceRunning(false);
            }
        }

        public override void OnDestroy()
        {
            if (null != mConnReceiver)
            {
                UnregisterReceiver(mConnReceiver);
                mConnReceiver = null;
            }
            mServiceStub.Disconnect(this);
            base.OnDestroy();
        }

        public int GetNetworkAvailabilityState(DownloadsDB db)
        {
            if (!mIsConnected)
                return NetworkConstants.NETWORK_NO_CONNECTION;
            else if (!mIsCellularConnection)
                return NetworkConstants.NETWORK_OK;
            else if (mIsRoaming)
                return NetworkConstants.NETWORK_CANNOT_USE_ROAMING;
            else if (!db.mFlags.HasFlag(DownloaderServiceFlags.FlagsDownloadOverCellular))
                return NetworkConstants.NETWORK_TYPE_DISALLOWED_BY_REQUESTOR;
            else
                return NetworkConstants.NETWORK_OK;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            try
            {
                mPackageInfo = PackageManager.GetPackageInfo(PackageName, 0);
                string applicationLabel = PackageManager.GetApplicationLabel(ApplicationInfo);
                mNotification = new DownloadNotification(this, applicationLabel);
            }
            catch (PackageManager.NameNotFoundException e)
            {
                e.PrintStackTrace();
            }
        }

     //   /**
     //* Exception thrown from methods called by generateSaveFile() for any fatal
     //* error.
     //*/

        /// <summary>
        /// Returns the filename (where the file should be saved) from info about a download
        /// </summary>
        public string GenerateTempSaveFileName(string fileName)
        {
            return String.Format("{0}{1}{2}{3}",
                                 Helpers.GetSaveFilePath(this), 
                                 Path.DirectorySeparatorChar, 
                                 fileName, 
                                 TemporaryFileExtension);
        }

       /// <summary>
        /// Creates a filename (where the file should be saved) from info about a download.
       /// </summary>
        public string GenerateSaveFile(string filename, long filesize)
       {
           string path = GenerateTempSaveFileName(filename);

           if (!Helpers.IsExternalMediaMounted())
           {
               Debug.WriteLine("External media not mounted: {0}", path);

               throw new GenerateSaveFileError(DownloadStatus.DeviceNotFoundError,
                                               "external media is not yet mounted");
           }
           if (System.IO.File.Exists(path))
           {
               Debug.WriteLine("File already exists: {0}", path);

               throw new GenerateSaveFileError(DownloadStatus.FileAlreadyExists,
                                               "requested destination file already exists");
           }

           if (Helpers.GetAvailableBytes(Helpers.GetFileSystemRoot(path)) < filesize)
           {
               throw new GenerateSaveFileError(DownloadStatus.InsufficientSpaceError, "insufficient space on external storage");
           }

           return path;
       }

        /// <summary>
        /// a non-localized string appropriate for logging corresponding to one of the NETWORK_* constants.
        /// </summary>
        public string GetLogMessageForNetworkError(int networkError)
        {
            switch (networkError)
            {
                case NetworkConstants.NETWORK_RECOMMENDED_UNUSABLE_DUE_TO_SIZE:
                    return "download size exceeds recommended limit for mobile network";

                case NetworkConstants.NETWORK_UNUSABLE_DUE_TO_SIZE:
                    return "download size exceeds limit for mobile network";

                case NetworkConstants.NETWORK_NO_CONNECTION:
                    return "no network connection available";

                case NetworkConstants.NETWORK_CANNOT_USE_ROAMING:
                    return "download cannot use the current network connection because it is roaming";

                case NetworkConstants.NETWORK_TYPE_DISALLOWED_BY_REQUESTOR:
                    return "download was requested to not use the current network type";

                default:
                    return "unknown error with network connectivity";
            }
        }

        public int getControl()
        {
            return mControl;
        }

        public int getStatus()
        {
            return mStatus;
        }

        /// <summary>
        /// Calculating a moving average for the speed so we don't get jumpy calculations for time etc.
        /// </summary>
        public void NotifyUpdateBytes(long totalBytesSoFar)
        {
            long timeRemaining;
            long currentTime = SystemClock.UptimeMillis();
            if (0 != mMillisecondsAtSample)
            {
                // we have a sample.
                long timePassed = currentTime - mMillisecondsAtSample;
                long bytesInSample = totalBytesSoFar - mBytesAtSample;
                float currentSpeedSample = bytesInSample/(float) timePassed;
                if (0 != mAverageDownloadSpeed)
                {
                    mAverageDownloadSpeed = SMOOTHING_FACTOR*currentSpeedSample + (1 - SMOOTHING_FACTOR)*mAverageDownloadSpeed;
                }
                else
                {
                    mAverageDownloadSpeed = currentSpeedSample;
                }
                timeRemaining = (long) ((mTotalLength - totalBytesSoFar)/mAverageDownloadSpeed);
            }
            else
            {
                timeRemaining = -1;
            }
            mMillisecondsAtSample = currentTime;
            mBytesAtSample = totalBytesSoFar;
            mNotification.OnDownloadProgress(new DownloadProgressInfo(mTotalLength, totalBytesSoFar, timeRemaining, mAverageDownloadSpeed));
        }

        protected override bool shouldStop()
        {
            // the database automatically reads the metadata for version code and download
            // status when the instance is created
            DownloadsDB db = DownloadsDB.getDB(this);
            if (db.mStatus == 0)
            {
                return true;
            }
            return false;
        }

        #region Nested type: GenerateSaveFileError

        public class GenerateSaveFileError : Exception
        {
            public int mStatus;

            public GenerateSaveFileError(int status, string message)
                : base(message)
            {
                mStatus = status;
            }
        }

        #endregion

        #region Nested type: InnerBroadcastReceiver

        /// <summary>
        /// We use this to track network state, such as when WiFi, Cellular, etc. is
        /// enabled when downloads are paused or in progress.
        /// </summary>
        private class InnerBroadcastReceiver : BroadcastReceiver
        {
            private readonly DownloaderService mService;

            internal InnerBroadcastReceiver(DownloaderService service)
            {
                mService = service;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                mService.PollNetworkState();
                if (mService.mStateChanged && !mService.isServiceRunning())
                {
                    Log.Debug(TAG, "InnerBroadcastReceiver Called");
                    var fileIntent = new Intent(context, mService.GetType());
                    fileIntent.PutExtra(EXTRA_PENDING_INTENT, mService.mPendingIntent);
                    // send a new intent to the service
                    context.StartService(fileIntent);
                }
            }
        }

        #endregion

        #region Nested type: LVLRunnable

        private class LvlRunnable : Object, IRunnable
        {
            private readonly DownloaderService _context;

            internal LvlRunnable(DownloaderService context, PendingIntent intent)
            {
                Debug.WriteLine("DownloaderService.LvlRunnable.ctor");
                _context = context;
                _context.mPendingIntent = intent;
            }

            #region IRunnable Members

            public void Run()
            {
                Debug.WriteLine("DownloaderService.LvlRunnable.Run");
                _context.setServiceRunning(true);
                _context.mNotification.OnDownloadStateChanged(DownloaderClientState.FetchingUrl);
                string deviceId = Settings.Secure.GetString(_context.ContentResolver, Settings.Secure.AndroidId);

                var aep = new ApkExpansionPolicy(_context, new AesObfuscator(_context.GetSalt(), _context.PackageName, deviceId));

                // reset our policy back to the start of the world to force a re-check
                aep.ResetPolicy();

                // let's try and get the OBB file from LVL first
                // Construct the LicenseChecker with a IPolicy.
                var checker = new LicenseChecker(_context, aep, _context.GetPublicKey());
                checker.CheckAccess(new ApkLicenseCheckerCallback(this, aep));
            }

            #endregion

            #region Nested type: APKLicenseCheckerCallback

            private class ApkLicenseCheckerCallback : ILicenseCheckerCallback
            {
                private readonly ApkExpansionPolicy _aep;
                private readonly LvlRunnable _lvlRunnable;

                public ApkLicenseCheckerCallback(LvlRunnable lvlRunnable, ApkExpansionPolicy aep)
                {
                    _lvlRunnable = lvlRunnable;
                    _aep = aep;
                }

                #region LicenseCheckerCallback Members

                public void Allow(PolicyServerResponse reason)
                {
                    Debug.WriteLine("DownloaderService.LvlRunnable.ApkLicenseCheckerCallback.Allow");
                    try
                    {
                        int count = _aep.GetExpansionUrlCount();
                        DownloadsDB db = DownloadsDB.getDB(Context);
                        if (count == 0)
                        {
                            Debug.WriteLine("No expansion packs.");
                        }

                        int status = 0;
                        for (int index = 0; index < count; index++)
                        {
                            string currentFileName = _aep.GetExpansionFileName(index);
                            if (null != currentFileName)
                            {
                                var di = new DownloadInfo(index, currentFileName, Context.PackageName);

                                long fileSize = _aep.GetExpansionFileSize(index);
                                if (Context.HandleFileUpdated(db, index, currentFileName, fileSize))
                                {
                                    status |= -1;
                                    di.ResetDownload();
                                    di.Uri = _aep.GetExpansionUrl(index);
                                    di.TotalBytes = fileSize;
                                    di.Status = status;
                                    db.updateDownload(di);
                                }
                                else
                                {
                                    // we need to read the download information from the database
                                    DownloadInfo dbdi = db.getDownloadInfoByFileName(di.FileName);
                                    if (dbdi == null)
                                    {
                                        // the file exists already and is the correct size
                                        // was delivered by Market or through another mechanism
                                        Debug.WriteLine("file {0} found. Not downloading.", di.FileName);
                                        di.Status = DownloadStatus.Success;
                                        di.TotalBytes = fileSize;
                                        di.CurrentBytes = fileSize;
                                        di.Uri = _aep.GetExpansionUrl(index);
                                        db.updateDownload(di);
                                    }
                                    else if (dbdi.Status != DownloadStatus.Success)
                                    {
                                        // we just update the URL
                                        dbdi.Uri = _aep.GetExpansionUrl(index);
                                        db.updateDownload(dbdi);
                                        status |= -1;
                                    }
                                }
                            }
                        }
                        // first: do we need to do an LVL update?
                        // we begin by getting our APK version from the package manager
                        try
                        {
                            PackageInfo pi = Context.PackageManager.GetPackageInfo(Context.PackageName, 0);
                            db.updateMetadata(pi.VersionCode, status);
                            var required = StartDownloadServiceIfRequired(Context, Context.mPendingIntent, Context.GetType());
                            switch (required)
                            {
                                case NO_DOWNLOAD_REQUIRED:
                                    Context.mNotification.OnDownloadStateChanged(DownloaderClientState.Completed);
                                    break;
                                case LVL_CHECK_REQUIRED: // DANGER WILL ROBINSON!
                                    Debug.WriteLine("In LVL checking loop!");
                                    Context.mNotification.OnDownloadStateChanged(DownloaderClientState.STATE_FAILED_UNLICENSED);
                                    throw new RuntimeException("Error with LVL checking and database integrity");
                                case DOWNLOAD_REQUIRED:
                                    // do nothing: the download will notify the application when things are done
                                    break;
                            }
                        }
                        catch (PackageManager.NameNotFoundException e1)
                        {
                            e1.PrintStackTrace();
                            throw new RuntimeException("Error with getting information from package name");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("LVL Update Exception: " + ex.Message);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Allow Exception: " + ex.Message);
                        throw;
                    }
                    finally
                    {
                        Context.setServiceRunning(false);
                    }
                }

                private DownloaderService Context
                {
                    get { return _lvlRunnable._context; }
                }

                public void DontAllow(PolicyServerResponse reason)
                {
                    Debug.WriteLine("DownloaderService.LvlRunnable.ApkLicenseCheckerCallback.DontAllow");
                    try
                    {
                        switch (reason)
                        {
                            case PolicyServerResponse.NotLicensed:
                                Context.mNotification.OnDownloadStateChanged(DownloaderClientState.STATE_FAILED_UNLICENSED);
                                break;
                            case PolicyServerResponse.Retry:
                                Context.mNotification.OnDownloadStateChanged(DownloaderClientState.STATE_FAILED_FETCHING_URL);
                                break;
                        }
                    }
                    finally
                    {
                        Context.setServiceRunning(false);
                    }
                }

                public void ApplicationError(CallbackErrorCode errorCode)
                {
                    try
                    {
                        Context.mNotification.OnDownloadStateChanged(DownloaderClientState.STATE_FAILED_FETCHING_URL);
                    }
                    finally
                    {
                        Context.setServiceRunning(false);
                    }
                }

                #endregion
            }

            #endregion
        }

        #endregion
    }
}