namespace ExpansionDownloader
{
    /// <summary>
    /// The download progress info.
    /// </summary>
    public class DownloadProgressInfo
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadProgressInfo"/> 
        /// class from a string that represents the info.
        /// </summary>
        /// <param name="parcel">
        /// The parcel.
        /// </param>
        public DownloadProgressInfo(string parcel)
        {
            string[] items = parcel.Split('|');
            this.OverallTotal = long.Parse(items[0]);
            this.OverallProgress = long.Parse(items[1]);
            this.TimeRemaining = long.Parse(items[2]);
            this.CurrentSpeed = float.Parse(items[3]);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadProgressInfo"/> class. 
        /// </summary>
        /// <param name="overallTotal">
        /// The overall total.
        /// </param>
        /// <param name="overallProgress">
        /// The overall progress.
        /// </param>
        /// <param name="timeRemaining">
        /// The time remaining.
        /// </param>
        /// <param name="currentSpeed">
        /// The current speed.
        /// </param>
        public DownloadProgressInfo(long overallTotal, long overallProgress, long timeRemaining, float currentSpeed)
        {
            this.OverallTotal = overallTotal;
            this.OverallProgress = overallProgress;
            this.TimeRemaining = timeRemaining;
            this.CurrentSpeed = currentSpeed;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current speed in KB/S.
        /// </summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>
        /// Gets the overall progress.
        /// </summary>
        public long OverallProgress { get; private set; }

        /// <summary>
        /// Gets the overall total.
        /// </summary>
        public long OverallTotal { get; private set; }

        /// <summary>
        /// Gets the time remaining.
        /// </summary>
        public long TimeRemaining { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Conver the current download info objet to a string.
        /// </summary>
        /// <returns>
        /// A string representing the download info.
        /// </returns>
        public override string ToString()
        {
            return string.Join(
                "|", this.OverallTotal, this.OverallProgress, this.TimeRemaining, this.CurrentSpeed.ToString("0.00"));
        }

        #endregion
    }
}