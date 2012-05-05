namespace ExpansionDownloader.Sample
{
    using Android.App;
    using Android.Content;
    using Android.Views;
    using Android.Widget;

    using ExpansionDownloader.impl;

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
        public int Icon { private get; set; }

        /// <summary>
        /// Gets or sets PausedText.
        /// </summary>
        public string PausedText { private get; set; }

        /// <summary>
        /// Gets or sets PendingIntent.
        /// </summary>
        public PendingIntent PendingIntent { private get; set; }

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
        public string Title { private get; set; }

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
            this.notification.Icon = this.Icon;

            this.notification.Flags |= NotificationFlags.OngoingEvent;

            // Build the RemoteView object
            var expandedView = new RemoteViews(
                context.PackageName, Resource.Layout.status_bar_ongoing_event_progress_bar);

            if (hasPausedText)
            {
                expandedView.SetViewVisibility(Resource.Id.progress_bar_frame, ViewStates.Gone);
                expandedView.SetViewVisibility(Resource.Id.description, ViewStates.Gone);
                expandedView.SetTextViewText(Resource.Id.paused_text, this.PausedText);
                expandedView.SetViewVisibility(Resource.Id.time_remaining, ViewStates.Gone);
            }
            else
            {
                expandedView.SetTextViewText(Resource.Id.title, this.Title);

                // look at strings
                expandedView.SetViewVisibility(Resource.Id.description, ViewStates.Visible);
                expandedView.SetTextViewText(Resource.Id.description, Helpers.GetDownloadProgressString(this.CurrentBytes, this.TotalBytes));
                expandedView.SetViewVisibility(Resource.Id.progress_bar_frame, (int)ViewStates.Visible);
                expandedView.SetViewVisibility(Resource.Id.paused_text, ViewStates.Gone);
                expandedView.SetProgressBar(
                    Resource.Id.progress_bar, 
                    (int)(this.TotalBytes >> 8), 
                    (int)(this.CurrentBytes >> 8), 
                    this.TotalBytes <= 0);
                expandedView.SetViewVisibility(Resource.Id.time_remaining, ViewStates.Visible);
                expandedView.SetTextViewText(Resource.Id.time_remaining, string.Format("Time remaining: {0}", Helpers.GetTimeRemaining(this.TimeRemaining)));
            }

            expandedView.SetTextViewText(Resource.Id.progress_text, Helpers.GetDownloadProgressPercent(this.CurrentBytes, this.TotalBytes));
            expandedView.SetImageViewResource(Resource.Id.appIcon, this.Icon);
            this.notification.ContentView = expandedView;
            this.notification.ContentIntent = this.PendingIntent;

            return this.notification;
        }

        #endregion
    }
}