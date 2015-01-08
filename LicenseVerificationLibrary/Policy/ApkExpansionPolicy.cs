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
    public class ApkExpansionPolicy : IPolicy
    {
        #region Constants and Fields

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

        /// <summary>
        /// The preference obfuscator.
        /// </summary>
        private readonly PreferenceObfuscator obfuscator;

        /// <summary>
        /// The last response recieved from the server.
        /// </summary>
        private PolicyServerResponse lastResponse;

        /// <summary>
        /// The last response time.
        /// </summary>
        private long lastResponseTime;

        /// <summary>
        /// The max retries.
        /// </summary>
        private long maxRetries;

        /// <summary>
        /// The retry count.
        /// </summary>
        private long retryCount;

        /// <summary>
        /// The retry until.
        /// </summary>
        private long retryUntil;

        /// <summary>
        /// The validity timestamp.
        /// </summary>
        private long validityTimestamp;

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
        {
            this.expansionFiles = new[] { new ExpansionFile(), new ExpansionFile() };

            // Import old values
            ISharedPreferences sp = context.GetSharedPreferences(Preferences.File, FileCreationMode.Private);
            this.obfuscator = new PreferenceObfuscator(sp, obfuscator);

            this.lastResponse = this.obfuscator.GetValue(Preferences.LastResponse, PolicyServerResponse.Retry);
            this.validityTimestamp = this.obfuscator.GetValue<long>(Preferences.ValidityTimestamp);
            this.retryUntil = this.obfuscator.GetValue<long>(Preferences.RetryUntil);
            this.maxRetries = this.obfuscator.GetValue<long>(Preferences.MaximumRetries);
            this.retryCount = this.obfuscator.GetValue<long>(Preferences.RetryCount);
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

        #region Public Properties

        /// <summary>
        /// Gets or sets the last license response received from the server and
        /// adds it to the preferences. 
        /// </summary>
        /// <remarks>
        /// You must manually call PreferenceObfuscator.Commit() to commit 
        /// these changes to disk.
        /// </remarks>
        public PolicyServerResponse LastResponse
        {
            get
            {
                return this.lastResponse;
            }

            set
            {
                this.lastResponseTime = PolicyExtensions.GetCurrentMilliseconds();
                this.lastResponse = value;
                this.obfuscator.PutString(Preferences.LastResponse, value.ToString());
            }
        }

        /// <summary>
        /// Gets or sets the server's last response time.
        /// </summary>
        public long LastResponseTime
        {
            get
            {
                return this.lastResponseTime;
            }

            set
            {
                this.lastResponseTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the max retries value (GR) as received from the server
        /// and add to preferences.
        /// </summary>
        public long MaxRetries
        {
            get
            {
                return this.maxRetries;
            }

            set
            {
                this.maxRetries = value;
                this.obfuscator.PutValue(Preferences.MaximumRetries, value);
            }
        }

        /// <summary>
        /// Gets or sets the current retry count and add to preferences.
        /// </summary>
        public long RetryCount
        {
            get
            {
                return this.retryCount;
            }

            set
            {
                this.retryCount = value;
                this.obfuscator.PutString(Preferences.RetryCount, value.ToString());
            }
        }

        /// <summary>
        /// Gets or sets the retry until timestamp (GT) received from the server and add to
        /// preferences.
        /// </summary>
        public long RetryUntil
        {
            get
            {
                return this.retryUntil;
            }

            set
            {
                this.retryUntil = value;
                this.obfuscator.PutValue(Preferences.RetryUntil, value);
            }
        }

        /// <summary>
        /// Gets or sets the last validity timestamp (VT) received from the server and add to
        /// preferences.
        /// </summary>
        public long ValidityTimestamp
        {
            get
            {
                return this.validityTimestamp;
            }

            set
            {
                this.validityTimestamp = value;
                this.obfuscator.PutValue(Preferences.ValidityTimestamp, value);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// This implementation allows access if either:
        /// <ol>
        /// <li>a LICENSED response was received within the validity period</li>
        /// <li>
        /// a RETRY response was received in the last minute, and we 
        /// are under the RETRY count or in the RETRY period.
        /// </li>
        /// </ol>
        /// </summary>
        /// <returns>
        /// True if access is allowed, otherwise false.
        /// </returns>
        public bool AllowAccess()
        {
            long ts = PolicyExtensions.GetCurrentMilliseconds();
            if (this.LastResponse == PolicyServerResponse.Licensed)
            {
                // Check if the LICENSED response occurred within the validity
                // timeout.
                if (ts <= this.ValidityTimestamp)
                {
                    // Cached LICENSED response is still valid.
                    return true;
                }
            }
            else if (this.LastResponse == PolicyServerResponse.Retry
                     && ts < this.LastResponseTime + PolicyExtensions.MillisPerMinute)
            {
                // Only allow access if we are within the retry period or we 
                // haven't used up our max retries.
                return ts <= this.RetryUntil || this.RetryCount <= this.MaxRetries;
            }

            return false;
        }
        
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

        /// <summary>
        /// Process a new response from the license server.
        /// We call this to guarantee that we fetch a fresh policy from the 
        /// server. This is to be used if the URL is invalid.
        /// This data will be used for computing future policy decisions. 
        /// The following parameters are processed:
        /// <ul>
        /// <li>VT: the timestamp that the client should consider the response valid until</li>
        /// <li>GT: the timestamp that the client should ignore retry errors until</li>
        /// <li>GR: the number of retry errors that the client should ignore</li>
        /// </ul>
        /// </summary>
        /// <param name="response">
        /// the result from validating the server response
        /// </param>
        /// <param name="rawData">
        /// the raw server response data
        /// </param>
        public void ProcessServerResponse(PolicyServerResponse response, ResponseData rawData)
        {
            // Update retry counter
            if (response == PolicyServerResponse.Retry)
            {
                this.RetryCount = this.RetryCount + 1;
            }
            else
            {
                this.RetryCount = 0;
            }

            if (response == PolicyServerResponse.Licensed)
            {
                // Update server policy data
                var extras = new Dictionary<string, string>();
                try
                {
                    extras = PolicyExtensions.DecodeExtras(rawData.Extra);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Invalid syntax error while decoding extras data from server.");
                }
                
                // If no response or not parseable, expire in one minute.
                this.ValidityTimestamp = PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute;

                foreach (var pair in extras)
                {
                    this.ProcessResponseExtra(pair);
                }
            }
            else if (response == PolicyServerResponse.NotLicensed)
            {
                // Clear out stale policy data
                this.ValidityTimestamp = 0;
                this.RetryUntil = 0;
                this.MaxRetries = 0;
            }

            this.LastResponse = response;
            this.obfuscator.Commit();
        }

        /// <summary>
        /// The reset policy.
        /// </summary>
        public void ResetPolicy()
        {
            this.LastResponse = PolicyServerResponse.Retry;
			this.LastResponseTime = 0;
            this.RetryUntil = 0;
            this.MaxRetries = 0;
            this.RetryCount = 0;
            this.ValidityTimestamp = 0;

            this.obfuscator.Commit();
        }
        
        #endregion

        #region Methods
        
        /// <summary>
        /// Parse each extra in the response
        /// </summary>
        /// <param name="pair"></param>
        private void ProcessResponseExtra(KeyValuePair<string, string> pair)
        {
            var key = pair.Key;
            var value = pair.Value;

            if (key == "VT")
            {
                long l;
                if (long.TryParse(value, out l))
                {
                    this.ValidityTimestamp = l;
                }
            }
            else if (key == "GT")
            {
                long l;
                if (!long.TryParse(value, out l))
                {
                    // No response or not parseable, expire immediately.
                    Debug.WriteLine("License retry timestamp (GT) missing, grace period disabled");
                }

                this.RetryUntil = l;
            }
            else if (key == "GR")
            {
                long l;
                if (!long.TryParse(value, out l))
                {
                    // No response or not parseable, immediately.
                    Debug.WriteLine("Licence retry count (GR) missing, grace period disabled");
                }

                this.MaxRetries = l;
            }
            else if (key.StartsWith(FileUrlKey))
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

        /// <summary>
        /// The apk expansion preferences.
        /// </summary>
        private static class Preferences
        {
            #region Constants and Fields

            /// <summary>
            /// The file.
            /// </summary>
            public const string File = "com.android.vending.licensing.APKExpansionPolicy";

            /// <summary>
            /// The last response.
            /// </summary>
            public const string LastResponse = "lastResponse";

            /// <summary>
            /// The maximum retries.
            /// </summary>
            public const string MaximumRetries = "maxRetries";

            /// <summary>
            /// The retry count.
            /// </summary>
            public const string RetryCount = "retryCount";

            /// <summary>
            /// The retry until.
            /// </summary>
            public const string RetryUntil = "retryUntil";

            /// <summary>
            /// The validity timestamp.
            /// </summary>
            public const string ValidityTimestamp = "validityTimestamp";

            #endregion
        }
    }
}