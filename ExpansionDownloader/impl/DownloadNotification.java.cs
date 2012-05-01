using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Java.Lang;

namespace ExpansionDownloader.impl
{
    public class DownloadNotification : IDownloaderClient
    {
        private static string LOGTAG = "DownloadNotification";
        private static readonly int NOTIFICATION_ID = LOGTAG.GetHashCode();
        private readonly Context mContext;
        private readonly string mLabel;
        private readonly Notification mNotification;
        private readonly NotificationManager mNotificationManager;
        private IDownloaderClient mClientProxy;
        private PendingIntent mContentIntent;
        private Notification mCurrentNotification;
        private string mCurrentText;
        private string mCurrentTitle;
        private DownloadProgressInfo mProgressInfo;
        private DownloaderClientState mState;

        internal DownloadNotification(Context ctx, string applicationLabel)
        {
            mState = DownloaderClientState.Unknown;
            mContext = ctx;
            mLabel = applicationLabel;
            mNotificationManager = mContext.GetSystemService(Context.NotificationService).JavaCast<NotificationManager>();
            mNotification = new Notification();
            mCurrentNotification = mNotification;
        }

        #region IDownloaderClient Members

        public void OnDownloadStateChanged(DownloaderClientState newState)
        {
            if (null != mClientProxy)
            {
                mClientProxy.OnDownloadStateChanged(newState);
            }
            if (newState != mState)
            {
                mState = newState;
                if (newState == DownloaderClientState.Idle || null == mContentIntent)
                {
                    return;
                }
                string stringDownload;
                int iconResource;
                bool ongoingEvent;

                // get the new title string and paused text
                switch (newState)
                {
                    case DownloaderClientState.Downloading:
                        iconResource = Resource.Drawable.StatSysDownload;
                        stringDownload = Helpers.GetDownloaderStringFromState(newState);
                        ongoingEvent = true;
                        break;

                    case DownloaderClientState.FetchingUrl:
                    case DownloaderClientState.Connecting:
                        iconResource = Resource.Drawable.StatSysDownloadDone;
                        stringDownload = Helpers.GetDownloaderStringFromState(newState);
                        ongoingEvent = true;
                        break;

                    case DownloaderClientState.Completed:
                    case DownloaderClientState.PausedByRequest:
                        iconResource = Resource.Drawable.StatSysDownloadDone;
                        stringDownload = Helpers.GetDownloaderStringFromState(newState);
                        ongoingEvent = false;
                        break;

                    case DownloaderClientState.STATE_FAILED:
                    case DownloaderClientState.STATE_FAILED_CANCELED:
                    case DownloaderClientState.STATE_FAILED_FETCHING_URL:
                    case DownloaderClientState.STATE_FAILED_SDCARD_FULL:
                    case DownloaderClientState.STATE_FAILED_UNLICENSED:
                        iconResource = Resource.Drawable.StatSysWarning;
                        stringDownload = Helpers.GetDownloaderStringFromState(newState);
                        ongoingEvent = false;
                        break;

                    default:
                        iconResource = Resource.Drawable.StatSysWarning;
                        stringDownload = Helpers.GetDownloaderStringFromState(newState);
                        ongoingEvent = true;
                        break;
                }
                mCurrentText = stringDownload;
                mCurrentTitle = mLabel;
                mCurrentNotification.TickerText = new String(mLabel + ": " + mCurrentText);
                mCurrentNotification.Icon = iconResource;
                mCurrentNotification.SetLatestEventInfo(mContext, mCurrentTitle, mCurrentText, mContentIntent);
                if (ongoingEvent)
                {
                    mCurrentNotification.Flags |= NotificationFlags.OngoingEvent;
                }
                else
                {
                    mCurrentNotification.Flags &= ~NotificationFlags.OngoingEvent;
                    mCurrentNotification.Flags |= NotificationFlags.AutoCancel;
                }
                mNotificationManager.Notify(NOTIFICATION_ID, mCurrentNotification);
            }
        }

        public void OnDownloadProgress(DownloadProgressInfo progress)
        {
            mProgressInfo = progress;
            if (null != mClientProxy)
            {
                mClientProxy.OnDownloadProgress(progress);
            }
            if (progress.OverallTotal <= 0)
            {
                // we just show the text
                mNotification.TickerText = new String(mCurrentTitle);
                mNotification.Icon = Resource.Drawable.StatSysDownload;
                mNotification.SetLatestEventInfo(mContext, mLabel, mCurrentText, mContentIntent);
                mCurrentNotification = mNotification;
            }
            else
            {
                CustomNotificationFactory.Notification.setCurrentBytes(progress.OverallProgress);
                CustomNotificationFactory.Notification.setTotalBytes(progress.OverallTotal);
                CustomNotificationFactory.Notification.setIcon(Resource.Drawable.StatSysDownload);
                CustomNotificationFactory.Notification.setPendingIntent(mContentIntent);
                CustomNotificationFactory.Notification.setTicker(mLabel + ": " + mCurrentText);
                CustomNotificationFactory.Notification.setTitle(mLabel);
                CustomNotificationFactory.Notification.setTimeRemaining(progress.TimeRemaining);
                mCurrentNotification = CustomNotificationFactory.Notification.updateNotification(mContext);
            }
            mNotificationManager.Notify(NOTIFICATION_ID, mCurrentNotification);
        }

        public void OnServiceConnected(Messenger m)
        {
        }

        #endregion

        public PendingIntent getClientIntent()
        {
            return mContentIntent;
        }

        public void setClientIntent(PendingIntent mClientIntent)
        {
            mContentIntent = mClientIntent;
        }

        public void resendState()
        {
            if (null != mClientProxy)
            {
                mClientProxy.OnDownloadStateChanged(mState);
            }
        }

        /**
     * Called in response to OnClientUpdated. Creates a new proxy and notifies
     * it of the current state.
     * 
     * @param msg the client Messenger to notify
     */

        public void setMessenger(Messenger msg)
        {
            mClientProxy = DownloaderClientMarshaller.CreateProxy(msg);
            if (null != mProgressInfo)
            {
                mClientProxy.OnDownloadProgress(mProgressInfo);
            }
            if (mState != DownloaderClientState.Unknown)
            {
                mClientProxy.OnDownloadStateChanged(mState);
            }
        }

        #region Nested type: ICustomNotification

        public interface ICustomNotification
        {
            void setTitle(string title);

            void setTicker(string ticker);

            void setPendingIntent(PendingIntent mContentIntent);

            void setPausedText(string pausedText);

            void setTotalBytes(long totalBytes);

            void setCurrentBytes(long currentBytes);

            void setIcon(int iconResource);

            void setTimeRemaining(long timeRemaining);

            Notification updateNotification(Context c);
        }

        #endregion

        /**
     * Constructor
     * 
     * @param ctx The context to use to obtain access to the Notification
     *            Service
     */
    }
}