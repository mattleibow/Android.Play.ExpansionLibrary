using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using ExpansionDownloader.impl;

public class V3CustomNotification : DownloadNotification.ICustomNotification
{
    string mTitle;
    string mPausedText;
    string mTicker;
    int mIcon;
    long mTotalBytes = -1;
    long mCurrentBytes = -1;
    long mTimeRemaining;
    PendingIntent mPendingIntent;
    Notification mNotification = new Notification();

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
        mTotalBytes = totalBytes;
    }

    public void setCurrentBytes(long currentBytes)
    {
        mCurrentBytes = currentBytes;
    }

    public Notification updateNotification(Context c)
    {
        Notification n = mNotification;

        bool hasPausedText = (mPausedText != null);
        n.Icon = mIcon;

        n.Flags |= NotificationFlags.OngoingEvent;

        // Build the RemoteView object
        RemoteViews expandedView = new RemoteViews(c.PackageName, Resource.layout.status_bar_ongoing_event_progress_bar);

        if (hasPausedText)
        {
            expandedView.SetViewVisibility(R.id.progress_bar_frame, ViewStates.Gone);
            expandedView.SetViewVisibility(R.id.description, ViewStates.Gone);
            expandedView.SetTextViewText(R.id.paused_text, mPausedText);
            expandedView.SetViewVisibility(R.id.time_remaining, ViewStates.Gone);
        }
        else
        {
            expandedView.SetTextViewText(R.id.title, mTitle);

            // look at strings
            expandedView.SetViewVisibility(R.id.description, ViewStates.Visible);
            expandedView.SetTextViewText(R.id.description, Helpers.getDownloadProgressString(mCurrentBytes, mTotalBytes));
            expandedView.SetViewVisibility(R.id.progress_bar_frame, ViewStates.Visible);
            expandedView.SetViewVisibility(R.id.paused_text, ViewStates.Gone);
            expandedView.SetProgressBar(R.id.progress_bar, (int) (mTotalBytes >> 8), (int) (mCurrentBytes >> 8), mTotalBytes <= 0);
            expandedView.SetViewVisibility(R.id.time_remaining, ViewStates.Visible);
            expandedView.SetTextViewText(R.id.time_remaining, string.Format("Time remaining: {0}", Helpers.getTimeRemaining(mTimeRemaining)));
        }

        expandedView.SetTextViewText(R.id.progress_text, Helpers.getDownloadProgressPercent(mCurrentBytes, mTotalBytes));
        expandedView.SetImageViewResource(R.id.appIcon, mIcon);
        n.ContentView = expandedView;
        n.ContentIntent = mPendingIntent;

        return n;
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
}
