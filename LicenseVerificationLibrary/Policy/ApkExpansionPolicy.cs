namespace LicenseVerificationLibrary.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Android.Content;

    using LicenseVerificationLibrary.Obfuscator;

    /// <summary>
    /// Default policy.
    /// All policy decisions are based off of response data received from the 
    /// licensing service.
    /// </summary>
    /// <remarks>
    /// Specifically, the licensing server sends the following information: 
    /// <ul>
    /// <li>response validity period,</li>
    /// <li>error retry period, and</li>
    /// <li>error retry count.</li>
    /// </ul>
    /// These values will vary based on the the way the application is
    /// configured in the Android Play publishing console, such as whether the 
    /// application is marked as free or is within its refund period, as well 
    /// as how often an application is checking with the licensing service.
    /// Developers who need more fine grained control over their application's
    /// licensing policy should implement a custom <see cref="IPolicy"/>.
    /// </remarks>
	public class ApkExpansionPolicy : ServerManagedPolicy
    {
        #region Constants and Fields

		/// <summary>
		/// The file.
		/// </summary>
		public const string PreferencesFile = "com.android.vending.licensing.APKExpansionPolicy";

        /// <summary>
        /// The string that contains the key for finding file names.
        /// </summary>
        private const string FileNameKey = "FILE_NAME";

        /// <summary>
        /// The string that contains the key for finding file sizes.
        /// </summary>
        private const string FileSizeKey = "FILE_SIZE";

        /// <summary>
        /// The string that contains the key for finding file urls.
        /// </summary>
        private const string FileUrlKey = "FILE_URL";

        public class ExpansionFile
        {
            public ExpansionFile()
            {
                FileName = null;
                FileSize = -1;
                Url = null;
            }

            public string FileName { get; set; }

            public long FileSize { get; set; }

            /// <summary>
            /// Gets or sets the expansion URL. 
            /// </summary>
            /// <remarks>
            /// Expansion URL's are not committed to preferences, but are 
            /// instead intended to be stored when the license response is 
            /// processed by the front-end.
            /// Since these URLs are not committed to preferences, this will 
            /// always return null if there has not been an LVL fetch in the 
            /// current session.
            /// </remarks>
            public string Url { get; set; }
        }

        /// <summary>
        /// The expansion files.
        /// </summary>
        private readonly ExpansionFile[] expansionFiles;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ApkExpansionPolicy"/> class. 
        /// </summary>
        /// <param name="context">
        /// The context for the current application
        /// </param>
        /// <param name="obfuscator">
        /// An obfuscator to be used when reading/writing to shared preferences.
        /// </param>
        public ApkExpansionPolicy(Context context, IObfuscator obfuscator)
			: base(context, obfuscator)
        {
            this.expansionFiles = new[] { new ExpansionFile(), new ExpansionFile() };
        }

        #endregion

        #region Enums

        /// <summary>
        /// The design of the protocol supports n files. Currently the market can
        /// only deliver two files. To accommodate this, we have these two constants,
        /// but the order is the only relevant thing here.
        /// </summary>
        public enum ExpansionFileType
        {
            /// <summary>
            /// The main file.
            /// </summary>
            MainFile = 0, 

            /// <summary>
            /// The patch file.
            /// </summary>
            PatchFile = 1
        }

        #endregion

        #region Public Methods and Operators

        public ExpansionFile GetExpansionFile(ExpansionFileType index)
        {
            return this.expansionFiles[(int)index];
        }

        /// <summary>
        /// Gets the count of expansion URLs. Since expansionURLs are not committed
        /// to preferences, this will return zero if there has been no LVL fetch in
        /// the current session. 
        /// </summary>
        /// <returns>
        /// the number of expansion URLs. (0,1,2)
        /// </returns>
        public int GetExpansionFilesCount()
        {
            return this.expansionFiles.Length;
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Parse each extra in the response
        /// </summary>
        /// <param name="pair"></param>
        protected override void ProcessResponseExtra(KeyValuePair<string, string> pair)
        {
			base.ProcessResponseExtra(pair);

            var key = pair.Key;
            var value = pair.Value;

            if (key.StartsWith(FileUrlKey))
            {
                var index = int.Parse(key.Substring(FileUrlKey.Length)) - 1;
                this.GetExpansionFile((ExpansionFileType)index).Url = value;
            }
            else if (key.StartsWith(FileNameKey))
            {
                var index = int.Parse(key.Substring(FileNameKey.Length)) - 1;
                this.GetExpansionFile((ExpansionFileType)index).FileName = value;
            }
            else if (key.StartsWith(FileSizeKey))
            {
                var index = int.Parse(key.Substring(FileSizeKey.Length)) - 1;
                this.GetExpansionFile((ExpansionFileType)index).FileSize = long.Parse(value);
            }
        }

        #endregion
    }
}