namespace ExpansionDownloader.Sample
{
    using Android.App;
    using Android.Content;
    using Android.Views;
    using Android.Widget;

    using ExpansionDownloader.Service;

    /// <summary>
    /// The v 3 custom notification.
    /// </summary>
    public class V3CustomNotification : DownloadNotification.ICustomNotification
    {
        #region Constants and Fields

        /// <summary>
        /// The notification.
        /// </summary>
        private readonly Notification notification;

        private RemoteViews expandedView;

        private bool lastHasPausedText;
        private PendingIntent pendingIntent;
        private int notificationIcon;
        private string title;
        private string pausedText;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="V3CustomNotification"/> class.
        /// </summary>
        public V3CustomNotification()
        {
            this.CurrentBytes = -1;
            this.TotalBytes = -1;
            this.notification = new Notification();
            this.expandedView = null;

            this.notification.Flags |= NotificationFlags.OngoingEvent;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets CurrentBytes.
        /// </summary>
        public long CurrentBytes { private get; set; }

        /// <summary>
        /// Gets or sets Icon.
        /// </summary>
        public int Icon
        {
            private get { return this.notificationIcon; }
            set
            {
                if (this.notificationIcon != value)
                {
                    this.notificationIcon = value;
                    this.notification.Icon = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets PausedText.
        /// </summary>
        public string PausedText
        {
            private get { return this.pausedText; }
            set
            {
                if (this.pausedText != value)
                {
                    this.pausedText = value;
                    if (this.expandedView != null)
                    {
                        expandedView.SetTextViewText(Resource.Id.paused_text, value);
                    }
                }
            }
        }


        /// <summary>
        /// Gets or sets PendingIntent.
        /// </summary>
        public PendingIntent PendingIntent
        {
            private get { return this.pendingIntent; }
            set
            {
                if (this.pendingIntent != value)
                {
                    this.pendingIntent = value;
                    this.notification.ContentIntent = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets Ticker.
        /// </summary>
        public string Ticker { private get; set; }

        /// <summary>
        /// Gets or sets TimeRemaining.
        /// </summary>
        public long TimeRemaining { private get; set; }

        /// <summary>
        /// Gets or sets Title.
        /// </summary>
        public string Title
        {
            private get { return this.title; }
            set
            {
                if (this.title != value)
                {
                    this.title = value;
                    if (this.expandedView != null)
                    {
                        expandedView.SetTextViewText(Resource.Id.title, value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets TotalBytes.
        /// </summary>
        public long TotalBytes { private get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Update the notification.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The updated notification.
        /// </returns>
        public Notification UpdateNotification(Context context)
        {
            bool hasPausedText = this.PausedText != null;
            bool mustUpdate = this.lastHasPausedText != hasPausedText;

            // Build the RemoteView object
            if (expandedView == null)
            {
                expandedView = new RemoteViews(context.PackageName, Resource.Layout.status_bar_ongoing_event_progress_bar);
                expandedView.SetImageViewResource(Resource.Id.appIcon, this.Icon);
                expandedView.SetTextViewText(Resource.Id.paused_text, this.PausedText);
                expandedView.SetTextViewText(Resource.Id.title, this.Title);

                this.notification.ContentView = expandedView;

                mustUpdate = true;
            }

            if (mustUpdate)
            {
                expandedView.SetViewVisibility(Resource.Id.progress_bar_frame, hasPausedText ? ViewStates.Gone : ViewStates.Visible);
                expandedView.SetViewVisibility(Resource.Id.description, hasPausedText ? ViewStates.Gone : ViewStates.Visible);
                expandedView.SetViewVisibility(Resource.Id.time_remaining, hasPausedText ? ViewStates.Gone : ViewStates.Visible);
                expandedView.SetViewVisibility(Resource.Id.paused_text, hasPausedText ? ViewStates.Visible : ViewStates.Gone);
            }

            if (!hasPausedText)
            {
                expandedView.SetTextViewText(Resource.Id.progress_text, Helpers.GetDownloadProgressPercent(this.CurrentBytes, this.TotalBytes));
                expandedView.SetTextViewText(Resource.Id.description, Helpers.GetDownloadProgressString(this.CurrentBytes, this.TotalBytes));
                expandedView.SetProgressBar(
                    Resource.Id.progress_bar,
                    (int)(this.TotalBytes >> 8),
                    (int)(this.CurrentBytes >> 8),
                    this.TotalBytes <= 0);
                expandedView.SetTextViewText(Resource.Id.time_remaining, string.Format("Time remaining: {0}", Helpers.GetTimeRemaining(this.TimeRemaining)));
            }

            this.lastHasPausedText = hasPausedText;

            return this.notification;
        }

        #endregion
    }
}