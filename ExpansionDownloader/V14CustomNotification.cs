namespace ExpansionDownloader
{
    using Android.App;
    using Android.Content;

    using ExpansionDownloader.Service;

    /// <summary>
    /// The custom notification for API levels 14+.
    /// </summary>
    public class V14CustomNotification : DownloadNotification.ICustomNotification
    {
        #region Constructors and Destructors
		
		Notification.Builder builder;

		#endregion

		#region Constructors and Destructors

		/// <summary>
        /// Initializes a new instance of the <see cref="V14CustomNotification"/> class.
        /// </summary>
        public V14CustomNotification()
        {
            this.CurrentBytes = -1;
            this.TotalBytes = -1;
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
	        if (builder == null)
	        {
		        builder = new Notification.Builder(context);
	        }

		    builder.SetContentTitle(this.Title);
			if (this.TotalBytes > 0 && this.CurrentBytes != -1)
            {
                builder.SetProgress((int)(this.TotalBytes >> 8), (int)(this.CurrentBytes >> 8), false);
            }
            else
            {
                builder.SetProgress(0, 0, true);
            }
            builder.SetContentText(Helpers.GetDownloadProgressString(this.CurrentBytes, this.TotalBytes));
            builder.SetContentInfo(context.GetString(Resource.String.time_remaining_notification, Helpers.GetTimeRemaining(this.TimeRemaining)));
            builder.SetSmallIcon(this.Icon != 0 ? this.Icon : Android.Resource.Drawable.StatSysDownload);
            builder.SetOngoing(true);
            builder.SetTicker(this.Ticker);
            builder.SetContentIntent(this.PendingIntent);

            return builder.Notification;
        }

        #endregion
    }
}