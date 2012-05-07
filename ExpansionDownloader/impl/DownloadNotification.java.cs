using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Java.Lang;

namespace ExpansionDownloader.impl
{
    using ExpansionDownloader.Client;

    public class DownloadNotification : IDownloaderClient
    {
        private static string LOGTAG = "DownloadNotification";
        private static readonly int NOTIFICATION_ID = LOGTAG.GetHashCode();
        private readonly Context mContext;
        private readonly string mLabel;
        private readonly Notification mNotification;
        private readonly NotificationManager mNotificationManager;
        private IDownloaderClient mClientProxy;

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
                if (newState == DownloaderClientState.Idle || null == this.PendingIntent)
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

                    case DownloaderClientState.Failed:
                    case DownloaderClientState.FailedCanceled:
                    case DownloaderClientState.FailedFetchingUrl:
                    case DownloaderClientState.FailedSdCardFull:
                    case DownloaderClientState.FailedUnlicensed:
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
                mCurrentNotification.SetLatestEventInfo(mContext, mCurrentTitle, mCurrentText, this.PendingIntent);
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
                mNotification.SetLatestEventInfo(mContext, mLabel, mCurrentText, this.PendingIntent);
                mCurrentNotification = mNotification;
            }
            else
            {
                CustomNotificationFactory.Notification.CurrentBytes=(progress.OverallProgress);
                CustomNotificationFactory.Notification.TotalBytes=(progress.OverallTotal);
                CustomNotificationFactory.Notification.Icon=(Resource.Drawable.StatSysDownload);
                CustomNotificationFactory.Notification.PendingIntent=(this.PendingIntent);
                CustomNotificationFactory.Notification.Ticker=(mLabel + ": " + mCurrentText);
                CustomNotificationFactory.Notification.Title=(mLabel);
                CustomNotificationFactory.Notification.TimeRemaining=(progress.TimeRemaining);
                mCurrentNotification = CustomNotificationFactory.Notification.UpdateNotification(mContext);
            }
            mNotificationManager.Notify(NOTIFICATION_ID, mCurrentNotification);
        }

        public void OnServiceConnected(Messenger m)
        {
        }

        #endregion

        public PendingIntent PendingIntent { get; set; }

        public void resendState()
        {
            if (null != mClientProxy)
            {
                mClientProxy.OnDownloadStateChanged(mState);
            }
        }

        /// <summary>
        /// Called in response to OnClientUpdated. Creates a new proxy and 
        /// notifies it of the current state.
        /// </summary>
        /// <param name="msg">the client Messenger to notify</param>
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
            string Title { set; }

            string Ticker { set; }

            PendingIntent PendingIntent { set; }

            string PausedText { set; }

            long TotalBytes { set; }

            long CurrentBytes { set; }

            int Icon { set; }

            long TimeRemaining { set; }


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="context">The context to use to obtain access to the Notification Service</param>
            /// <returns></returns>
            Notification UpdateNotification(Context context);
        }

        #endregion
    }
}