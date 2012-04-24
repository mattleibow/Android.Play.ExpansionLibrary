using System;
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
using Java.IO;
using Java.Lang;
using LicenseVerificationLibrary;
using Exception = System.Exception;
using Object = Java.Lang.Object;

namespace ExpansionDownloader.impl
{
    public abstract class DownloaderService : CustomIntentService, IDownloaderService
    {
        // the following NETWORK_* constants are used to indicates specific reasons
        // for disallowing a
        // download from using a network, since specific causes can require special
        // handling

        /**
     * The network is usable for the given download.
     */
        public const int NETWORK_OK = 1;

        /**
     * There is no network connectivity.
     */
        public const int NETWORK_NO_CONNECTION = 2;

        /**
     * The download exceeds the maximum size for this network.
     */
        public const int NETWORK_UNUSABLE_DUE_TO_SIZE = 3;

        /**
     * The download exceeds the recommended maximum size for this network, the
     * user must confirm for this download to proceed without WiFi.
     */
        public const int NETWORK_RECOMMENDED_UNUSABLE_DUE_TO_SIZE = 4;

        /**
     * The current connection is roaming, and the download can't proceed over a
     * roaming connection.
     */
        public const int NETWORK_CANNOT_USE_ROAMING = 5;

        /**
     * The app requesting the download specific that it can't use the current
     * network connection.
     */
        public const int NETWORK_TYPE_DISALLOWED_BY_REQUESTOR = 6;

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
     * Broadcast intent action sent by the download manager when a download
     * completes.
     */
        public const string ACTION_DOWNLOAD_COMPLETE = "lvldownloader.intent.action.DOWNLOAD_COMPLETE";

        /**
     * Broadcast intent action sent by the download manager when download status
     * changes.
     */
        public const string ACTION_DOWNLOAD_STATUS = "lvldownloader.intent.action.DOWNLOAD_STATUS";

        /*
     * Lists the states that the download manager can set on a download to
     * notify applications of the download progress. The codes follow the HTTP
     * families:<br> 1xx: informational<br> 2xx: success<br> 3xx: redirects (not
     * used by the download manager)<br> 4xx: client errors<br> 5xx: server
     * errors
     */

        /**
     * Returns whether the status is informational (i.e. 1xx).
     */

        /**
     * This download hasn't stated yet
     */
        public const int STATUS_PENDING = 190;

        /**
     * This download has started
     */
        public const int STATUS_RUNNING = 192;

        /**
     * This download has been paused by the owning app.
     */
        public const int STATUS_PAUSED_BY_APP = 193;

        /**
     * This download encountered some network error and is waiting before
     * retrying the request.
     */
        public const int STATUS_WAITING_TO_RETRY = 194;

        /**
     * This download is waiting for network connectivity to proceed.
     */
        public const int STATUS_WAITING_FOR_NETWORK = 195;

        /**
     * This download exceeded a size limit for mobile networks and is waiting
     * for a Wi-Fi connection to proceed.
     */
        public const int STATUS_QUEUED_FOR_WIFI = 196;

        /**
     * This download has successfully completed. Warning: there might be other
     * status values that indicate success in the future. Use isSucccess() to
     * capture the entire category.
     * 
     * @hide
     */
        public const int STATUS_SUCCESS = 200;

        /**
     * The requested URL is no longer available
     */
        public const int STATUS_FORBIDDEN = 403;

        /**
     * The file was delivered incorrectly
     */
        public const int STATUS_FILE_DELIVERED_INCORRECTLY = 487;

        /**
     * The requested destination file already exists.
     */
        public const int STATUS_FILE_ALREADY_EXISTS_ERROR = 488;

        /**
     * Some possibly transient error occurred, but we can't resume the download.
     */
        public const int STATUS_CANNOT_RESUME = 489;

        /**
     * This download was canceled
     * 
     * @hide
     */
        public const int STATUS_CANCELED = 490;

        /**
     * This download has completed with an error. Warning: there will be other
     * status values that indicate errors in the future. Use isStatusError() to
     * capture the entire category.
     */
        public const int STATUS_UNKNOWN_ERROR = 491;

        /**
     * This download couldn't be completed because of a storage issue.
     * Typically, that's because the filesystem is missing or full. Use the more
     * specific {@link #STATUS_INSUFFICIENT_SPACE_ERROR} and
     * {@link #STATUS_DEVICE_NOT_FOUND_ERROR} when appropriate.
     * 
     * @hide
     */
        public const int STATUS_FILE_ERROR = 492;

        /**
     * This download couldn't be completed because of an HTTP redirect response
     * that the download manager couldn't handle.
     * 
     * @hide
     */
        public const int STATUS_UNHANDLED_REDIRECT = 493;

        /**
     * This download couldn't be completed because of an unspecified unhandled
     * HTTP code.
     * 
     * @hide
     */
        public const int STATUS_UNHANDLED_HTTP_CODE = 494;

        /**
     * This download couldn't be completed because of an error receiving or
     * processing data at the HTTP level.
     * 
     * @hide
     */
        public const int STATUS_HTTP_DATA_ERROR = 495;

        /**
     * This download couldn't be completed because of an HttpException while
     * setting up the request.
     * 
     * @hide
     */
        public const int STATUS_HTTP_EXCEPTION = 496;

        /**
     * This download couldn't be completed because there were too many
     * redirects.
     * 
     * @hide
     */
        public const int STATUS_TOO_MANY_REDIRECTS = 497;

        /**
     * This download couldn't be completed due to insufficient storage space.
     * Typically, this is because the SD card is full.
     * 
     * @hide
     */
        public const int STATUS_INSUFFICIENT_SPACE_ERROR = 498;

        /**
     * This download couldn't be completed because no external storage device
     * was found. Typically, this is because the SD card is not mounted.
     * 
     * @hide
     */
        public const int STATUS_DEVICE_NOT_FOUND_ERROR = 499;

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
     * This download is visible but only shows in the notifications while it's
     * in progress.
     * 
     * @hide
     */
        public const int VISIBILITY_VISIBLE = 0;

        /**
     * This download is visible and shows in the notifications while in progress
     * and after completion.
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
     * Bit flag for {@link #setAllowedNetworkTypes} corresponding to
     * {@link ConnectivityManager#TYPE_MOBILE}.
     */
        public const int NETWORK_MOBILE = 1 << 0;

        /**
     * Bit flag for {@link #setAllowedNetworkTypes} corresponding to
     * {@link ConnectivityManager#TYPE_WIFI}.
     */
        public const int NETWORK_WIFI = 1 << 1;
        public const int NO_DOWNLOAD_REQUIRED = 0;
        public const int LVL_CHECK_REQUIRED = 1;
        public const int DOWNLOAD_REQUIRED = 2;

        public const string EXTRA_PACKAGE_NAME = "EPN";
        public const string EXTRA_PENDING_INTENT = "EPI";
        public const string EXTRA_MESSAGE_HANDLER = "EMH";
        private static string LOG_TAG = "LVLDL";

        private static string TEMP_EXT = ".tmp";

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
            mServiceMessenger = mServiceStub.getMessenger();
        }

        #region IDownloaderService Members

        public void requestAbortDownload()
        {
            mControl = CONTROL_PAUSED;
            mStatus = STATUS_CANCELED;
        }

        public void requestPauseDownload()
        {
            mControl = CONTROL_PAUSED;
            mStatus = STATUS_PAUSED_BY_APP;
        }

        public void setDownloadFlags(int flags)
        {
            DownloadsDB.getDB(this).updateFlags(flags);
        }

        public void requestContinueDownload()
        {
            if (mControl == CONTROL_PAUSED)
            {
                mControl = CONTROL_RUN;
            }
            var fileIntent = new Intent(this, GetType());
            fileIntent.PutExtra(EXTRA_PENDING_INTENT, mPendingIntent);
            StartService(fileIntent);
        }

        public void requestDownloadStatus()
        {
            mNotification.resendState();
        }

        public void onClientUpdated(Messenger clientMessenger)
        {
            mClientMessenger = clientMessenger;
            mNotification.setMessenger(mClientMessenger);
        }

        #endregion

        public static bool isStatusInformational(int status)
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

        public IBinder onBind(Intent paramIntent)
        {
            Log.Debug(Constants.TAG, "Service Bound");
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
                case ConnectivityType.Ethernet:
                case ConnectivityType.Bluetooth:
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
                        case NetworkType.Lte: // 4G
                        case NetworkType.Ehrpd: // 3G ++ interop with 4G
                        case NetworkType.Hspap: // 3G ++ but marketed as 4G
                            mIsAtLeast3G = true;
                            mIsAtLeast4G = true;
                            break;
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
            if (null != info)
            {
                mIsRoaming = info.IsRoaming;
                mIsFailover = info.IsFailover;
                mIsConnected = info.IsConnected;
                updateNetworkType((int) info.Type, (int) info.Subtype);
            }
            else
            {
                mIsRoaming = false;
                mIsFailover = false;
                mIsConnected = false;
                updateNetworkType(-1, -1);
            }

            mStateChanged = (mStateChanged ||
                             isConnected != mIsConnected ||
                             isFailover != mIsFailover ||
                             isCellularConnection != mIsCellularConnection ||
                             isRoaming != mIsRoaming ||
                             isAtLeast3G != mIsAtLeast3G);

            if (Constants.LOGVV)
            {
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
                            mStatus = STATUS_WAITING_FOR_NETWORK;
                            mControl = CONTROL_PAUSED;
                        }
                        else if (mIsCellularConnection)
                        {
                            DownloadsDB db = DownloadsDB.getDB(this);
                            int flags = db.getFlags();
                            if (0 == (flags & IDownloaderServiceConsts.FLAGS_DOWNLOAD_OVER_CELLULAR))
                            {
                                mStatus = STATUS_QUEUED_FOR_WIFI;
                                mControl = CONTROL_PAUSED;
                            }
                        }
                    }
                }
            }
        }

        /**
     * Polls the network state, setting the flags appropriately.
     */

        private void pollNetworkState()
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
                Log.Warn(Constants.TAG, "couldn't get connectivity manager to poll network state");
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

        public static int startDownloadServiceIfRequired(Context context, Intent intent, Type serviceClass)
        {
            var pendingIntent = (PendingIntent) intent.GetParcelableExtra(EXTRA_PENDING_INTENT);
            return startDownloadServiceIfRequired(context, pendingIntent, serviceClass);
        }

        /**
     * Starts the download if necessary. This function starts a flow that does `
     * many things. 1) Checks to see if the APK version has been checked and
     * the metadata database updated 2) If the APK version does not match,
     * checks the new LVL status to see if a new download is required 3) If the
     * APK version does match, then checks to see if the download(s) have been
     * completed 4) If the downloads have been completed, returns
     * NO_DOWNLOAD_REQUIRED The idea is that this can be called during the
     * startup of an application to quickly ascertain if the application needs
     * to wait to hear about any updated APK expansion files. Note that this does
     * mean that the application MUST be run for the first time with a network
     * connection, even if Market delivers all of the files.
     * 
     * @param context
     * @param thisIntent
     * @return true if the app should wait for more guidance from the
     *         downloader, false if the app can continue
     * @throws NameNotFoundException
     */

        public static int startDownloadServiceIfRequired(Context context, PendingIntent pendingIntent, Type serviceClass)
        {
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
                DownloadInfo[] infos = db.getDownloads();
                if (null != infos)
                {
                    foreach (DownloadInfo info in infos)
                    {
                        if (!Helpers.doesFileExist(context, info.mFileName, info.mTotalBytes, true))
                        {
                            status = DOWNLOAD_REQUIRED;
                            db.updateStatus(-1);
                            break;
                        }
                    }
                }
            } else
            {
                status = DOWNLOAD_REQUIRED;
            }
            switch (status)
            {
                case DOWNLOAD_REQUIRED:
                case LVL_CHECK_REQUIRED:
                    var fileIntent = new Intent(context.ApplicationContext, serviceClass);
                    fileIntent.PutExtra(EXTRA_PENDING_INTENT, pendingIntent);
                    context.StartService(fileIntent);
                    break;
            }
            return status;
        }

        public abstract string getPublicKey(); // Your public licensing key.

        public abstract byte[] getSALT();

        public abstract string getAlarmReceiverClassName();

        /**
     * Updates the LVL information from the server.
     * 
     * @param context
     */

        public void updateLVL(DownloaderService context)
        {
            var h = new Handler(context.MainLooper);
            h.Post(new LVLRunnable(context, mPendingIntent));
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

        public bool handleFileUpdated(DownloadsDB db, int index, string filename, long fileSize)
        {
            DownloadInfo di = db.getDownloadInfoByFileName(filename);
            if (null != di)
            {
                string oldFile = di.mFileName;
                // cleanup
                if (null != oldFile)
                {
                    if (filename == oldFile)
                    {
                        return false;
                    }

                    // remove partially downloaded file if it is there
                    string deleteFile = Helpers.generateSaveFileName(this, oldFile);
                    var f = new File(deleteFile);
                    if (f.Exists())
                        f.Delete();
                }
            }
            return !Helpers.doesFileExist(this, filename, fileSize, true);
        }

        private void scheduleAlarm(long wakeUp)
        {
            var alarms = GetSystemService(AlarmService).JavaCast<AlarmManager>();
            if (alarms == null)
            {
                Log.Error(Constants.TAG, "couldn't get alarm manager");
                return;
            }

            if (Constants.LOGV)
            {
                Log.Verbose(Constants.TAG, "scheduling retry in " + wakeUp + "ms");
            }

            string className = getAlarmReceiverClassName();
            var intent = new Intent(Constants.ACTION_RETRY);
            intent.PutExtra(EXTRA_PENDING_INTENT, mPendingIntent);
            intent.SetClassName(PackageName, className);
            mAlarmIntent = PendingIntent.GetBroadcast(this, 0, intent, PendingIntentFlags.OneShot);
            alarms.Set(AlarmType.RtcWakeup, PolicyExtensions.GetCurrentMilliseconds() + wakeUp, mAlarmIntent);
        }

        private void cancelAlarms()
        {
            if (null != mAlarmIntent)
            {
                var alarms = GetSystemService(AlarmService).JavaCast<AlarmManager>();
                if (alarms == null)
                {
                    Log.Error(Constants.TAG, "couldn't get alarm manager");
                    return;
                }
                alarms.Cancel(mAlarmIntent);
                mAlarmIntent = null;
            }
        }

        /**
     * We use this to track network state, such as when WiFi, Cellular, etc. is
     * enabled when downloads are paused or in progress.
     */

        /**
     * This is the main thread for the Downloader. This thread is responsible
     * for queuing up downloads and other goodness.
     */

        protected override void onHandleIntent(Intent intent)
        {
            setServiceRunning(true);
            try
            {
                // the database automatically reads the metadata for version code
                // and download status when the instance is created
                DownloadsDB db = DownloadsDB.getDB(this);
                var pendingIntent = (PendingIntent) intent.GetParcelableExtra(EXTRA_PENDING_INTENT);

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

                // when the LVL check completes, a successful response will update
                // the service
                if (isLVLCheckRequired(db, mPackageInfo))
                {
                    updateLVL(this);
                    return;
                }

                // get each download
                DownloadInfo[] infos = db.getDownloads();
                mBytesSoFar = 0;
                mTotalLength = 0;
                mFileCount = infos.Length;
                foreach (DownloadInfo info in infos)
                {
                    // We do an (simple) integrity check on each file, just to make sure
                    if (info.mStatus == STATUS_SUCCESS)
                    {
                        // verify that the file matches the state
                        if (!Helpers.doesFileExist(this, info.mFileName, info.mTotalBytes, true))
                        {
                            info.mStatus = 0;
                            info.mCurrentBytes = 0;
                        }
                    }
                    // get aggregate data
                    mTotalLength += info.mTotalBytes;
                    mBytesSoFar += info.mCurrentBytes;
                }

                // loop through all downloads and fetch them
                pollNetworkState();
                if (null == mConnReceiver)
                {
                    /**
                 * We use this to track network state, such as when WiFi,
                 * Cellular, etc. is enabled when downloads are paused or in
                 * progress.
                 */
                    mConnReceiver = new InnerBroadcastReceiver(this);
                    var intentFilter = new IntentFilter(ConnectivityManager.ConnectivityAction);
                    intentFilter.AddAction(WifiManager.WifiStateChangedAction);
                    RegisterReceiver(mConnReceiver, intentFilter);
                }

                foreach (DownloadInfo info in infos)
                {
                    long startingCount = info.mCurrentBytes;

                    if (info.mStatus != STATUS_SUCCESS)
                    {
                        var dt = new DownloadThread(info, this, mNotification);
                        cancelAlarms();
                        scheduleAlarm(Constants.ACTIVE_THREAD_WATCHDOG);
                        dt.run();
                        cancelAlarms();
                    }
                    db.updateFromDb(info);
                    bool setWakeWatchdog = false;
                    DownloaderClientState notifyStatus;
                    switch (info.mStatus)
                    {
                        case STATUS_FORBIDDEN:
                            // the URL is out of date
                            updateLVL(this);
                            return;
                        case STATUS_SUCCESS:
                            mBytesSoFar += info.mCurrentBytes - startingCount;
                            db.updateMetadata(mPackageInfo.VersionCode, 0);
                            mNotification.onDownloadStateChanged(DownloaderClientState.STATE_COMPLETED);
                            return;
                        case STATUS_FILE_DELIVERED_INCORRECTLY:
                            // we may be on a network that is returning us a web page on redirect
                            notifyStatus = DownloaderClientState.STATE_PAUSED_NETWORK_SETUP_FAILURE;
                            info.mCurrentBytes = 0;
                            db.updateDownload(info);
                            setWakeWatchdog = true;
                            break;
                        case STATUS_PAUSED_BY_APP:
                            notifyStatus = DownloaderClientState.STATE_PAUSED_BY_REQUEST;
                            break;
                        case STATUS_WAITING_FOR_NETWORK:
                        case STATUS_WAITING_TO_RETRY:
                            notifyStatus = DownloaderClientState.STATE_PAUSED_NETWORK_UNAVAILABLE;
                            setWakeWatchdog = true;
                            break;
                        case STATUS_QUEUED_FOR_WIFI:
                            // look for more detail here
                            if (null != mWifiManager)
                            {
                                if (!mWifiManager.IsWifiEnabled)
                                {
                                    notifyStatus = DownloaderClientState.STATE_PAUSED_WIFI_DISABLED_NEED_CELLULAR_PERMISSION;
                                    setWakeWatchdog = true;
                                    break;
                                }
                            }
                            notifyStatus = DownloaderClientState.STATE_PAUSED_NEED_CELLULAR_PERMISSION;
                            setWakeWatchdog = true;
                            break;
                        case STATUS_CANCELED:
                            notifyStatus = DownloaderClientState.STATE_FAILED_CANCELED;
                            setWakeWatchdog = true;
                            break;

                        case STATUS_INSUFFICIENT_SPACE_ERROR:
                            notifyStatus = DownloaderClientState.STATE_FAILED_SDCARD_FULL;
                            setWakeWatchdog = true;
                            break;

                        case STATUS_DEVICE_NOT_FOUND_ERROR:
                            notifyStatus = DownloaderClientState.STATE_PAUSED_SDCARD_UNAVAILABLE;
                            setWakeWatchdog = true;
                            break;

                        default:
                            notifyStatus = DownloaderClientState.STATE_FAILED;
                            break;
                    }
                    if (setWakeWatchdog)
                    {
                        scheduleAlarm(Constants.WATCHDOG_WAKE_TIMER);
                    }
                    else
                    {
                        cancelAlarms();
                    }
                    // failure or pause state
                    mNotification.onDownloadStateChanged(notifyStatus);
                    return;
                }
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
            mServiceStub.disconnect(this);
            base.OnDestroy();
        }

        public int getNetworkAvailabilityState(DownloadsDB db)
        {
            if (mIsConnected)
            {
                if (!mIsCellularConnection)
                    return NETWORK_OK;
                int flags = db.mFlags;
                if (mIsRoaming)
                    return NETWORK_CANNOT_USE_ROAMING;
                if (0 != (flags & IDownloaderServiceConsts.FLAGS_DOWNLOAD_OVER_CELLULAR))
                {
                    return NETWORK_OK;
                }
                else
                {
                    return NETWORK_TYPE_DISALLOWED_BY_REQUESTOR;
                }
            }
            return NETWORK_NO_CONNECTION;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            try
            {
                mPackageInfo = PackageManager.GetPackageInfo(PackageName, 0);
                ApplicationInfo ai = ApplicationInfo;
                string applicationLabel = PackageManager.GetApplicationLabel(ai);
                mNotification = new DownloadNotification(this, applicationLabel);
            }
            catch (PackageManager.NameNotFoundException e)
            {
                e.PrintStackTrace();
            }
        }

        /**
     * Returns maximum size, in bytes, of downloads that may go over a mobile connection; or null if
     * there's no limit
     *
     * @param context the {@link Context} to use for accessing the {@link ContentResolver}
     * @return maximum size, in bytes, of downloads that may go over a mobile connection; or null if
     * there's no limit
     */

        public static long getMaxBytesOverMobile(Context context)
        {
            return DownloadManager.GetMaxBytesOverMobile(context).LongValue();
        }

        /**
     * Returns recommended maximum size, in bytes, of downloads that may go over a mobile
     * connection; or null if there's no recommended limit.  The user will have the option to bypass
     * this limit.
     *
     * @param context the {@link Context} to use for accessing the {@link ContentResolver}
     * @return recommended maximum size, in bytes, of downloads that may go over a mobile
     * connection; or null if there's no recommended limit.
     */

        public static long getRecommendedMaxBytesOverMobile(Context context)
        {
            return DownloadManager.GetRecommendedMaxBytesOverMobile(context).LongValue();
        }

        /**
     * Exception thrown from methods called by generateSaveFile() for any fatal
     * error.
     */

        /**
     * Returns the filename (where the file should be saved) from info about a
     * download
     */

        public string generateTempSaveFileName(string fileName)
        {
            string path = Helpers.getSaveFilePath(this) + File.Separator + fileName + TEMP_EXT;
            return path;
        }

        /**
     * Creates a filename (where the file should be saved) from info about a
     * download.
     */

        public string generateSaveFile(string filename, long filesize)
        {
            string path = generateTempSaveFileName(filename);
            var expPath = new File(path);
            if (!Helpers.isExternalMediaMounted())
            {
                Log.Debug(Constants.TAG, "External media not mounted: " + path);
                throw new GenerateSaveFileError(STATUS_DEVICE_NOT_FOUND_ERROR, "external media is not yet mounted");
            }
            if (expPath.Exists())
            {
                Log.Debug(Constants.TAG, "File already exists: " + path);
                throw new GenerateSaveFileError(STATUS_FILE_ALREADY_EXISTS_ERROR, "requested destination file already exists");
            }
            if (Helpers.getAvailableBytes(Helpers.getFilesystemRoot(path)) < filesize)
            {
                throw new GenerateSaveFileError(STATUS_INSUFFICIENT_SPACE_ERROR, "insufficient space on external storage");
            }
            return path;
        }

        /**
     * @return a non-localized string appropriate for logging corresponding to
     *         one of the NETWORK_* constants.
     */

        public string getLogMessageForNetworkError(int networkError)
        {
            switch (networkError)
            {
                case NETWORK_RECOMMENDED_UNUSABLE_DUE_TO_SIZE:
                    return "download size exceeds recommended limit for mobile network";

                case NETWORK_UNUSABLE_DUE_TO_SIZE:
                    return "download size exceeds limit for mobile network";

                case NETWORK_NO_CONNECTION:
                    return "no network connection available";

                case NETWORK_CANNOT_USE_ROAMING:
                    return "download cannot use the current network connection because it is roaming";

                case NETWORK_TYPE_DISALLOWED_BY_REQUESTOR:
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

        /**
     * Calculating a moving average for the speed so we don't get jumpy
     * calculations for time etc.
     */

        public void notifyUpdateBytes(long totalBytesSoFar)
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
            mNotification.onDownloadProgress(
                new DownloadProgressInfo(mTotalLength, totalBytesSoFar, timeRemaining, mAverageDownloadSpeed));
        }

        protected override bool shouldStop()
        {
            // the database automatically reads the metadata for version code
            // and download status when the instance is created
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
            private static long serialVersionUID = 3465966015408936540L;
            public int mStatus;

            public GenerateSaveFileError(int status, string message)
                : base(message)
            {
                mStatus = status;
            }
        }

        #endregion

        #region Nested type: InnerBroadcastReceiver

        private class InnerBroadcastReceiver : BroadcastReceiver
        {
            private readonly DownloaderService mService;

            internal InnerBroadcastReceiver(DownloaderService service)
            {
                mService = service;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                mService.pollNetworkState();
                if (mService.mStateChanged && !mService.isServiceRunning())
                {
                    Log.Debug(Constants.TAG, "InnerBroadcastReceiver Called");
                    var fileIntent = new Intent(context, mService.GetType());
                    fileIntent.PutExtra(EXTRA_PENDING_INTENT, mService.mPendingIntent);
                    // send a new intent to the service
                    context.StartService(fileIntent);
                }
            }
        } ;

        #endregion

        #region Nested type: LVLRunnable

        private class LVLRunnable : Object, IRunnable
        {
            private readonly DownloaderService mContext;

            internal LVLRunnable(DownloaderService context, PendingIntent intent)
            {
                mContext = context;
                mContext.mPendingIntent = intent;
            }

            #region IRunnable Members

            public void Run()
            {
                mContext.setServiceRunning(true);
                mContext.mNotification.onDownloadStateChanged(DownloaderClientState.STATE_FETCHING_URL);
                string deviceId = Settings.Secure.GetString(mContext.ContentResolver, Settings.Secure.AndroidId);

                var aep = new APKExpansionPolicy(mContext, new AESObfuscator(mContext.getSALT(), mContext.PackageName, deviceId));

                // reset our policy back to the start of the world to force a re-check
                aep.resetPolicy();

                // let's try and get the OBB file from LVL first
                // Construct the LicenseChecker with a IPolicy.
                var checker = new LicenseChecker(mContext, aep, mContext.getPublicKey());
                checker.CheckAccess(new APKLicenseCheckerCallback(this, aep));
            }

            #endregion

            #region Nested type: APKLicenseCheckerCallback

            public class APKLicenseCheckerCallback : ILicenseCheckerCallback
            {
                private readonly APKExpansionPolicy _aep;
                private readonly LVLRunnable _lvlRunnable;

                public APKLicenseCheckerCallback(LVLRunnable lvlRunnable, APKExpansionPolicy aep)
                {
                    _lvlRunnable = lvlRunnable;
                    _aep = aep;
                }

                #region LicenseCheckerCallback Members

                public void Allow(PolicyServerResponse reason)
                {
                    try
                    {
                        int count = _aep.getExpansionURLCount();
                        DownloadsDB db = DownloadsDB.getDB(_lvlRunnable.mContext);
                        int status = 0;
                        if (count != 0)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                string currentFileName = _aep.getExpansionFileName(i);
                                if (null != currentFileName)
                                {
                                    var di = new DownloadInfo(i, currentFileName, _lvlRunnable.mContext.PackageName);

                                    long fileSize = _aep.getExpansionFileSize(i);
                                    if (_lvlRunnable.mContext.handleFileUpdated(db, i, currentFileName, fileSize))
                                    {
                                        status |= -1;
                                        di.resetDownload();
                                        di.mUri = _aep.getExpansionURL(i);
                                        di.mTotalBytes = fileSize;
                                        di.mStatus = status;
                                        db.updateDownload(di);
                                    }
                                    else
                                    {
                                        // we need to read the download
                                        // information
                                        // from
                                        // the database
                                        DownloadInfo dbdi = db.getDownloadInfoByFileName(di.mFileName);
                                        if (null == dbdi)
                                        {
                                            // the file exists already and is
                                            // the
                                            // correct size
                                            // was delivered by Market or
                                            // through
                                            // another mechanism
                                            Log.Debug(LOG_TAG, "file " + di.mFileName + " found. Not downloading.");
                                            di.mStatus = STATUS_SUCCESS;
                                            di.mTotalBytes = fileSize;
                                            di.mCurrentBytes = fileSize;
                                            di.mUri = _aep.getExpansionURL(i);
                                            db.updateDownload(di);
                                        }
                                        else if (dbdi.mStatus != STATUS_SUCCESS)
                                        {
                                            // we just update the URL
                                            dbdi.mUri = _aep.getExpansionURL(i);
                                            db.updateDownload(dbdi);
                                            status |= -1;
                                        }
                                    }
                                }
                            }
                        }
                        // first: do we need to do an LVL update?
                        // we begin by getting our APK version from the package
                        // manager
                        PackageInfo pi;
                        try
                        {
                            pi = _lvlRunnable.mContext.PackageManager.GetPackageInfo(_lvlRunnable.mContext.PackageName, 0);
                            db.updateMetadata(pi.VersionCode, status);
                            Type serviceClass = typeof (DownloaderService);
                            switch (startDownloadServiceIfRequired(_lvlRunnable.mContext, _lvlRunnable.mContext.mPendingIntent, serviceClass))
                            {
                                case NO_DOWNLOAD_REQUIRED:
                                    _lvlRunnable.mContext.mNotification.onDownloadStateChanged(DownloaderClientState.STATE_COMPLETED);
                                    break;
                                case LVL_CHECK_REQUIRED:
                                    // DANGER WILL ROBINSON!
                                    Log.Error(LOG_TAG, "In LVL checking loop!");
                                    _lvlRunnable.mContext.mNotification.onDownloadStateChanged(DownloaderClientState.STATE_FAILED_UNLICENSED);
                                    throw new RuntimeException(
                                        "Error with LVL checking and database integrity");
                                case DOWNLOAD_REQUIRED:
                                    // do nothing. the download will notify the
                                    // application
                                    // when things are done
                                    break;
                            }
                        }
                        catch (PackageManager.NameNotFoundException e1)
                        {
                            e1.PrintStackTrace();
                            throw new RuntimeException(
                                "Error with getting information from package name");
                        }
                    }
                    finally
                    {
                        _lvlRunnable.mContext.setServiceRunning(false);
                    }
                }

                public void DontAllow(PolicyServerResponse reason)
                {
                    try
                    {
                        switch (reason)
                        {
                            case PolicyServerResponse.NotLicensed:
                                _lvlRunnable.mContext.mNotification.onDownloadStateChanged(DownloaderClientState.STATE_FAILED_UNLICENSED);
                                break;
                            case PolicyServerResponse.Retry:
                                _lvlRunnable.mContext.mNotification.onDownloadStateChanged(DownloaderClientState.STATE_FAILED_FETCHING_URL);
                                break;
                        }
                    }
                    finally
                    {
                        _lvlRunnable.mContext.setServiceRunning(false);
                    }
                }

                public void ApplicationError(CallbackErrorCode errorCode)
                {
                    try
                    {
                        _lvlRunnable.mContext.mNotification.onDownloadStateChanged(DownloaderClientState.STATE_FAILED_FETCHING_URL);
                    }
                    finally
                    {
                        _lvlRunnable.mContext.setServiceRunning(false);
                    }
                }

                #endregion
            }

            #endregion
        }

        #endregion
    }
}