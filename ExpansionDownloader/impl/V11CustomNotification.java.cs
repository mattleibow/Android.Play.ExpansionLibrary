using Android;
using Android.App;
using Android.Content;

namespace ExpansionDownloader.impl
{
    public class V11CustomNotification : DownloadNotification.ICustomNotification
    {
        private long mCurrentKB = -1;
        private int mIcon;
        private string mPausedText;
        private PendingIntent mPendingIntent;
        private string mTicker;
        private long mTimeRemaining;
        private string mTitle;
        private long mTotalKB = -1;

        #region ICustomNotification Members

        public void setIcon(int icon)
        {
            mIcon = icon;
        }

        public void setTitle(string title)
        {
            mTitle = title;
        }

        public void setPausedText(string pausedText)
        {
            mPausedText = pausedText;
        }

        public void setTotalBytes(long totalBytes)
        {
            mTotalKB = totalBytes;
        }

        public void setCurrentBytes(long currentBytes)
        {
            mCurrentKB = currentBytes;
        }

        public Notification updateNotification(Context c)
        {
            var builder = new Notification.Builder(c);
            bool hasPausedText = null != mPausedText;
            if (hasPausedText)
            {
                builder.SetContentTitle(mPausedText);
                builder.SetContentText(string.Empty);
                builder.SetContentInfo(string.Empty);
            }
            else
            {
                builder.SetContentTitle(mTitle);
                if (mTotalKB > 0 && -1 != mCurrentKB)
                {
                    builder.SetProgress((int) (mTotalKB >> 8), (int) (mCurrentKB >> 8), false);
                }
                else
                {
                    builder.SetProgress(0, 0, true);
                }
                builder.SetContentText(Helpers.getDownloadProgressString(mCurrentKB, mTotalKB));
                builder.SetContentInfo(string.Format("{0}s left", Helpers.getTimeRemaining(mTimeRemaining)));
            }
            if (mIcon != 0)
            {
                builder.SetSmallIcon(mIcon);
            }
            else
            {
                int iconResource = Resource.Drawable.StatSysDownload;
                if (hasPausedText)
                {
                    iconResource = Resource.Drawable.StatSysDownload;
                }
                builder.SetSmallIcon(iconResource);
            }
            builder.SetOngoing(true);
            builder.SetTicker(mTicker);
            builder.SetContentIntent(mPendingIntent);

            return builder.Notification;
        }

        public void setPendingIntent(PendingIntent contentIntent)
        {
            mPendingIntent = contentIntent;
        }

        public void setTicker(string ticker)
        {
            mTicker = ticker;
        }

        public void setTimeRemaining(long timeRemaining)
        {
            mTimeRemaining = timeRemaining;
        }

        #endregion
    }
}