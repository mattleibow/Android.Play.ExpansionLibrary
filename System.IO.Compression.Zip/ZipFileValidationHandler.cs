namespace System.IO.Compression.Zip
{
    /// <summary>
    /// The zip file validation handler.
    /// </summary>
    public class ZipFileValidationHandler
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipFileValidationHandler"/> class.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        public ZipFileValidationHandler(string filename)
        {
            this.Filename = filename;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets AverageSpeed.
        /// </summary>
        public float AverageSpeed { get; set; }

        /// <summary>
        /// Gets or sets CurrentBytes.
        /// </summary>
        public long CurrentBytes { get; set; }

        /// <summary>
        /// Gets Filename.
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether ShouldCancel.
        /// </summary>
        public bool ShouldCancel { get; set; }

        /// <summary>
        /// Gets or sets TimeRemaining.
        /// </summary>
        public long TimeRemaining { get; set; }

        /// <summary>
        /// Gets or sets TotalBytes.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets UpdateUi.
        /// </summary>
        public Action<ZipFileValidationHandler> UpdateUi { get; set; }

        #endregion
    }
}