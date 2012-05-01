namespace LicenseVerificationLibrary
{
    using System;
    using System.Collections.Generic;

    using Android.Content;

    /// <summary>
    /// Default policy. All policy decisions are based off of response data received
    /// from the licensing service. Specifically, the licensing server sends the
    /// following information: response validity period, error retry period, and
    /// error retry count.
    /// These values will vary based on the the way the application is configured in
    /// the Android Market publishing console, such as whether the application is
    /// marked as free or is within its refund period, as well as how often an
    /// application is checking with the licensing service.
    /// Developers who need more fine grained control over their application's
    /// licensing policy should implement a custom Policy.
    /// </summary>
    public class ApkExpansionPolicy : IPolicy
    {
        #region Constants and Fields

        /// <summary>
        /// The string that contains the key for finding file urls.
        /// </summary>
        private const string FileUrl = "FILE_URL";

        /// <summary>
        /// The string that contains the key for finding file names.
        /// </summary>
        private const string FileName = "FILE_NAME";

        /// <summary>
        /// The string that contains the key for finding file sizes.
        /// </summary>
        private const string FileSize = "FILE_SIZE";

        /// <summary>
        /// The expansion file names.
        /// </summary>
        private readonly string[] expansionFileNames;

        /// <summary>
        /// The expansion file sizes.
        /// </summary>
        private readonly long[] expansionFileSizes;

        /// <summary>
        /// The expansion urls.
        /// </summary>
        private readonly string[] expansionUrls;

        /// <summary>
        /// The preference obfuscator.
        /// </summary>
        private readonly PreferenceObfuscator preferenceObfuscator;

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
            this.expansionUrls = new string[] { null, null };
            this.expansionFileSizes = new long[] { -1, -1 };
            this.expansionFileNames = new string[] { null, null };

            // Import old values
            ISharedPreferences sp = context.GetSharedPreferences(ApkExpansionPreferences.File, FileCreationMode.Private);
            this.preferenceObfuscator = new PreferenceObfuscator(sp, obfuscator);
            string response = this.preferenceObfuscator.GetValue(ApkExpansionPreferences.LastResponse, PolicyServerResponse.Retry.ToString());
            this.lastResponse = (PolicyServerResponse)Enum.Parse(typeof(PolicyServerResponse), response);
            this.validityTimestamp = this.preferenceObfuscator.GetValue(
                ApkExpansionPreferences.ValidityTimestamp, ApkExpansionPreferences.DefaultValidityTimestamp);
            this.retryUntil = this.preferenceObfuscator.GetValue(ApkExpansionPreferences.RetryUntil, ApkExpansionPreferences.DefaultRetryUntil);
            this.maxRetries = this.preferenceObfuscator.GetValue(ApkExpansionPreferences.MaximumRetries, ApkExpansionPreferences.DefaultMaximumRetries);
            this.retryCount = this.preferenceObfuscator.GetValue(ApkExpansionPreferences.RetryCount, ApkExpansionPreferences.DefaultRetryCount);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the server's last response.
        /// </summary>
        public PolicyServerResponse LastResponse
        {
            get
            {
                return this.lastResponse;
            }

            set
            {
                this.lastResponse = value;
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
                this.preferenceObfuscator.PutValue(ApkExpansionPreferences.MaximumRetries, value);
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
                this.preferenceObfuscator.PutString(ApkExpansionPreferences.RetryCount, this.retryCount.ToString());
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
                this.preferenceObfuscator.PutValue(ApkExpansionPreferences.RetryUntil, value);
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
                this.preferenceObfuscator.PutValue(ApkExpansionPreferences.ValidityTimestamp, value);
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
            if (this.lastResponse == PolicyServerResponse.Licensed)
            {
                // Check if the LICENSED response occurred within the validity
                // timeout.
                if (ts <= this.validityTimestamp)
                {
                    // Cached LICENSED response is still valid.
                    return true;
                }
            }
            else if (this.lastResponse == PolicyServerResponse.Retry && ts < this.lastResponseTime + PolicyExtensions.MillisPerMinute)
            {
                // Only allow access if we are within the retry period or we 
                // haven't used up our max retries.
                return ts <= this.retryUntil || this.retryCount <= this.maxRetries;
            }

            return false;
        }

        /// <summary>
        /// Gets the expansion file name.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The expansion file name.
        /// </returns>
        public string GetExpansionFileName(int index)
        {
            if (index < this.expansionFileNames.Length)
            {
                return this.expansionFileNames[index];
            }

            return null;
        }

        /// <summary>
        /// Gets the expansion file size.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The expansion file size.
        /// </returns>
        public long GetExpansionFileSize(int index)
        {
            if (index < this.expansionFileSizes.Length)
            {
                return this.expansionFileSizes[index];
            }

            return -1;
        }

        /// <summary>
        /// Gets the expansion URL. Since these URLs are not committed to
        /// preferences, this will always return null if there has not been an LVL
        /// fetch in the current session.
        /// </summary>
        /// <param name="index">
        /// the index of the URL to fetch. 
        /// This value will be either MainFile or PatchFile
        /// </param>
        /// <returns>
        /// The get expansion url.
        /// </returns>
        public string GetExpansionUrl(int index)
        {
            var index0 = index;
            if (index0 < this.expansionUrls.Length)
            {
                return this.expansionUrls[index0];
            }

            return null;
        }

        /// <summary>
        /// Gets the count of expansion URLs. Since expansionURLs are not committed
        /// to preferences, this will return zero if there has been no LVL fetch in
        /// the current session. 
        /// </summary>
        /// <returns>
        /// the number of expansion URLs. (0,1,2)
        /// </returns>
        public int GetExpansionUrlCount()
        {
            return this.expansionUrls.Length;
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
                    System.Diagnostics.Debug.WriteLine("Invalid syntax error while decoding extras data from server.");
                }

                this.lastResponse = response;
                this.ValidityTimestamp = PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute;

                foreach (string key in extras.Keys)
                {
                    var value = extras[key];
                    long l;
                    long.TryParse(value, out l);

                    if (key == "VT")
                    {
                        if (l == 0)
                        {
                            // No response or not parseable, expire in one minute.
                            System.Diagnostics.Debug.WriteLine("License validity timestamp (VT) missing, caching for a minute");
                            l = PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute;
                        }

                        this.ValidityTimestamp = l;
                    }
                    else if (key == "GT")
                    {
                        if (l == 0)
                        {
                            // No response or not parseable, expire immediately.
                            System.Diagnostics.Debug.WriteLine("License retry timestamp (GT) missing, grace period disabled");
                        }

                        this.RetryUntil = l;
                    }
                    else if (key == "GR")
                    {
                        if (l == 0)
                        {
                            // No response or not parseable, immediately.
                            System.Diagnostics.Debug.WriteLine("Licence retry count (GR) missing, grace period disabled");
                        }

                        this.MaxRetries = l;
                    }
                    else if (key.StartsWith(FileUrl))
                    {
                        var index = int.Parse(key.Substring(FileUrl.Length));
                        this.SetExpansionUrl(index - 1, value);
                    }
                    else if (key.StartsWith(FileName))
                    {
                        var index = int.Parse(key.Substring(FileName.Length));
                        this.SetExpansionFileName(index - 1, value);
                    }
                    else if (key.StartsWith(FileSize))
                    {
                        var index = int.Parse(key.Substring(FileSize.Length));
                        this.SetExpansionFileSize(index - 1, long.Parse(value));
                    }
                }
            }
            else if (response == PolicyServerResponse.NotLicensed)
            {
                // Clear out stale policy data
                this.ValidityTimestamp = ApkExpansionPreferences.DefaultValidityTimestamp;
                this.RetryUntil = ApkExpansionPreferences.DefaultRetryUntil;
                this.MaxRetries = ApkExpansionPreferences.DefaultMaximumRetries;
            }

            this.SetLastResponse(response);
            this.preferenceObfuscator.Commit();
        }

        /// <summary>
        /// The reset policy.
        /// </summary>
        public void ResetPolicy()
        {
            this.preferenceObfuscator.PutString(ApkExpansionPreferences.LastResponse, PolicyServerResponse.Retry.ToString());

            this.RetryUntil = ApkExpansionPreferences.DefaultRetryUntil;
            this.MaxRetries = ApkExpansionPreferences.DefaultMaximumRetries;
            this.RetryCount = ApkExpansionPreferences.DefaultRetryCount;
            this.ValidityTimestamp = ApkExpansionPreferences.DefaultValidityTimestamp;

            this.preferenceObfuscator.Commit();
        }

        /// <summary>
        /// The set expansion file name.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        public void SetExpansionFileName(int index, string name)
        {
            var index0 = index;
            this.expansionFileNames[index0] = name;
        }

        /// <summary>
        /// The set expansion file size.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <param name="size">
        /// The size.
        /// </param>
        public void SetExpansionFileSize(int index, long size)
        {
            var index0 = index;
            this.expansionFileSizes[index0] = size;
        }

        /// <summary>
        /// Sets the expansion URL. 
        /// Expansion URL's are not committed to preferences, but are instead 
        /// intended to be stored when the license response is processed by 
        /// the front-end.
        /// </summary>
        /// <param name="index">
        ///            the index of the expansion URL. This value will be either
        ///            MainFile or PatchFile
        /// </param>
        /// <param name="url">
        /// the URL to set
        /// </param>
        public void SetExpansionUrl(int index, string url)
        {
            var index0 = index;
            this.expansionUrls[index0] = url;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Set the last license response received from the server and add to
        /// preferences. You must manually call PreferenceObfuscator.commit() to
        /// commit these changes to disk.
        /// </summary>
        /// <param name="response">
        /// The response.
        /// </param>
        private void SetLastResponse(PolicyServerResponse response)
        {
            this.lastResponseTime = PolicyExtensions.GetCurrentMilliseconds();
            this.lastResponse = response;
            this.preferenceObfuscator.PutString(ApkExpansionPreferences.LastResponse, response.ToString());
        }

        #endregion

        /// <summary>
        /// The apk expansion preferences.
        /// </summary>
        public class ApkExpansionPreferences
        {
            #region Constants and Fields

            /// <summary>
            /// The default maximum retries.
            /// </summary>
            public const long DefaultMaximumRetries = 0L;

            /// <summary>
            /// The default retry count.
            /// </summary>
            public const long DefaultRetryCount = 0L;

            /// <summary>
            /// The default retry until.
            /// </summary>
            public const long DefaultRetryUntil = 0L;

            /// <summary>
            /// The default validity timestamp.
            /// </summary>
            public const long DefaultValidityTimestamp = 0L;

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

        /// <summary>
        /// The design of the protocol supports n files. Currently the market can
        /// only deliver two files. To accommodate this, we have these two constants,
        /// but the order is the only relevant thing here.
        /// </summary>
        public class ExpansionFileType
        {
            #region Constants and Fields

            /// <summary>
            /// The main file.
            /// </summary>
            public const long MainFile = 0;

            /// <summary>
            /// The patch file.
            /// </summary>
            public const long PatchFile = 1;

            #endregion
        }
    }
}