namespace ExpansionDownloader.impl
{
    using Android;
    using Android.App;
    using Android.Content;

    /// <summary>
    /// The custom notification for API levels 11+.
    /// </summary>
    public class V11CustomNotification : DownloadNotification.ICustomNotification
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="V11CustomNotification"/> class.
        /// </summary>
        public V11CustomNotification()
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
            var builder = new Notification.Builder(context);
            if (!string.IsNullOrEmpty(this.PausedText))
            {
                builder.SetContentTitle(this.PausedText);
                builder.SetContentText(string.Empty);
                builder.SetContentInfo(string.Empty);
            }
            else
            {
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
                builder.SetContentInfo(string.Format("{0}s left", Helpers.GetTimeRemaining(this.TimeRemaining)));
            }

            builder.SetSmallIcon(this.Icon != 0 ? this.Icon : Resource.Drawable.StatSysDownload);
            builder.SetOngoing(true);
            builder.SetTicker(this.Ticker);
            builder.SetContentIntent(this.PendingIntent);

            return builder.Notification;
        }

        #endregion
    }
}