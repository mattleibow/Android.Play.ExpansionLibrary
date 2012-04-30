using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using ExpansionDownloader;
using ExpansionDownloader.Sample;
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
        RemoteViews expandedView = new RemoteViews(c.PackageName, Resource.Layout.status_bar_ongoing_event_progress_bar);

        if (hasPausedText)
        {
            expandedView.SetViewVisibility(Resource.Id.progress_bar_frame, ViewStates.Gone);
            expandedView.SetViewVisibility(Resource.Id.description, ViewStates.Gone);
            expandedView.SetTextViewText(Resource.Id.paused_text, mPausedText);
            expandedView.SetViewVisibility(Resource.Id.time_remaining, ViewStates.Gone);
        }
        else
        {
            expandedView.SetTextViewText(Resource.Id.title, mTitle);

            // look at strings
            expandedView.SetViewVisibility(Resource.Id.description, ViewStates.Visible);
            expandedView.SetTextViewText(Resource.Id.description, Helpers.GetDownloadProgressString(mCurrentBytes, mTotalBytes));
            expandedView.SetViewVisibility(Resource.Id.progress_bar_frame, (int)ViewStates.Visible);
            expandedView.SetViewVisibility(Resource.Id.paused_text, ViewStates.Gone);
            expandedView.SetProgressBar(Resource.Id.progress_bar, (int) (mTotalBytes >> 8), (int) (mCurrentBytes >> 8), mTotalBytes <= 0);
            expandedView.SetViewVisibility(Resource.Id.time_remaining, ViewStates.Visible);
            expandedView.SetTextViewText(Resource.Id.time_remaining, string.Format("Time remaining: {0}", Helpers.GetTimeRemaining(mTimeRemaining)));
        }

        expandedView.SetTextViewText(Resource.Id.progress_text, Helpers.GetDownloadProgressPercent(mCurrentBytes, mTotalBytes));
        expandedView.SetImageViewResource(Resource.Id.appIcon, mIcon);
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
