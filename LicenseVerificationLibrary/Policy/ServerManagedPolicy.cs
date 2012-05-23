namespace LicenseVerificationLibrary.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Android.Content;

    using LicenseVerificationLibrary.Obfuscator;

    /// <summary>
    /// <para>
    /// Default policy. 
    /// All policy decisions are based off of response data received from the 
    /// licensing service. Specifically, the licensing server sends the 
    /// following information: response validity period, error retry period, 
    /// and error retry count.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// These values will vary based on the the way the application is 
    /// configured in the Android Play publishing console, such as whether 
    /// the application is marked as free or is within its refund period, as 
    /// well as how often an application is checking with the licensing service.
    /// </para>
    /// <para>
    /// Developers who need more fine grained control over their application's
    /// licensing policy should implement a custom <see cref="IPolicy"/>.
    /// </para>
    /// </remarks>
    public class ServerManagedPolicy : IPolicy
    {
        #region Constants and Fields

        /// <summary>
        /// The default max retries.
        /// </summary>
        private const string DefaultMaxRetries = "0";

        /// <summary>
        /// The default retry count.
        /// </summary>
        private const string DefaultRetryCount = "0";

        /// <summary>
        /// The default retry until.
        /// </summary>
        private const string DefaultRetryUntil = "0";

        /// <summary>
        /// The default validity timestamp.
        /// </summary>
        private const string DefaultValidityTimestamp = "0";

        /// <summary>
        /// The pref last response.
        /// </summary>
        private const string PrefLastResponse = "lastResponse";

        /// <summary>
        /// The pref max retries.
        /// </summary>
        private const string PrefMaxRetries = "maxRetries";

        /// <summary>
        /// The pref retry count.
        /// </summary>
        private const string PrefRetryCount = "retryCount";

        /// <summary>
        /// The pref retry until.
        /// </summary>
        private const string PrefRetryUntil = "retryUntil";

        /// <summary>
        /// The pref validity timestamp.
        /// </summary>
        private const string PrefValidityTimestamp = "validityTimestamp";

        /// <summary>
        /// The prefs file.
        /// </summary>
        private const string PrefsFile = "com.android.vending.licensing.ServerManagedPolicy";

        /// <summary>
        /// The preferences.
        /// </summary>
        private readonly PreferenceObfuscator preferences;

        /// <summary>
        /// The last response.
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
        /// Initializes a new instance of the <see cref="ServerManagedPolicy"/> class. 
        /// The server managed policy.
        /// </summary>
        /// <param name="context">
        /// The context for the current application
        /// </param>
        /// <param name="obfuscator">
        /// An obfuscator to be used with preferences.
        /// </param>
        public ServerManagedPolicy(Context context, IObfuscator obfuscator)
        {
            // Import old values
            ISharedPreferences sp = context.GetSharedPreferences(PrefsFile, FileCreationMode.Private);
            this.preferences = new PreferenceObfuscator(sp, obfuscator);
            string lastResponse = this.preferences.GetString(
                PrefLastResponse, ((int)PolicyServerResponse.Retry).ToString());
            this.LastResponse = (PolicyServerResponse)Enum.Parse(typeof(PolicyServerResponse), lastResponse);
            this.ValidityTimestamp =
                long.Parse(this.preferences.GetString(PrefValidityTimestamp, DefaultValidityTimestamp));
            this.RetryUntil = long.Parse(this.preferences.GetString(PrefRetryUntil, DefaultRetryUntil));
            this.MaxRetries = long.Parse(this.preferences.GetString(PrefMaxRetries, DefaultMaxRetries));
            this.RetryCount = long.Parse(this.preferences.GetString(PrefRetryCount, DefaultRetryCount));
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Set the last license response received from the server and add to
        ///   preferences. You must manually call PreferenceObfuscator.commit() to
        ///   commit these changes to disk.
        /// </summary>
        public PolicyServerResponse LastResponse
        {
            get
            {
                return this.lastResponse;
            }

            private set
            {
                this.lastResponseTime = PolicyExtensions.GetCurrentMilliseconds();
                this.lastResponse = value;
                this.preferences.PutString(PrefLastResponse, this.lastResponse.ToString());
            }
        }

        /// <summary>
        ///   The max retries value (GR) as received from the server
        /// </summary>
        public long MaxRetries
        {
            get
            {
                return this.maxRetries;
            }

            private set
            {
                this.maxRetries = value;
                this.preferences.PutString(PrefMaxRetries, this.maxRetries.ToString());
            }
        }

        /// <summary>
        ///   Set the current retry count and add to preferences. You must manually
        ///   call PreferenceObfuscator.commit() to commit these changes to disk.
        /// </summary>
        public long RetryCount
        {
            get
            {
                return this.retryCount;
            }

            private set
            {
                this.retryCount = value;
                this.preferences.PutString(PrefRetryCount, this.retryCount.ToString());
            }
        }

        /// <summary>
        ///   The retry until timestamp (GT) received from the server.
        /// </summary>
        public long RetryUntil
        {
            get
            {
                return this.retryUntil;
            }

            private set
            {
                this.retryUntil = value;
                this.preferences.PutString(PrefRetryUntil, this.retryUntil.ToString());
            }
        }

        /// <summary>
        ///   The last validity timestamp (VT) received from the server
        /// </summary>
        public long ValidityTimestamp
        {
            get
            {
                return this.validityTimestamp;
            }

            private set
            {
                this.validityTimestamp = value;
                this.preferences.PutString(PrefValidityTimestamp, this.validityTimestamp.ToString());
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// This implementation allows access if either:
        ///   <ol>
        ///     <li>a LICENSED response was received within the validity period</li>
        ///     <li>
        ///       a RETRY response was received in the last minute, and we are under
        ///       the RETRY count or in the RETRY period.
        ///     </li>
        ///   </ol>
        /// </summary>
        /// <returns>
        /// The allow access.
        /// </returns>
        public bool AllowAccess()
        {
            bool allowed = false;

            long ts = PolicyExtensions.GetCurrentMilliseconds();
            if (this.LastResponse == PolicyServerResponse.Licensed)
            {
                // Check if the LICENSED response occurred within the validity timeout and is still valid.
                allowed = ts <= this.ValidityTimestamp;
            }

            if (this.LastResponse == PolicyServerResponse.Retry
                && ts < this.lastResponseTime + PolicyExtensions.MillisPerMinute)
            {
                // Only allow access if we are within the retry period or we haven't used up our max retries.
                allowed = ts <= this.RetryUntil || this.RetryCount <= this.MaxRetries;
            }

            return allowed;
        }

        /// <summary>
        /// Process a new response from the license server. 
        ///   This data will be used for computing future policy decisions. The
        ///   following parameters are processed:
        ///   <ul>
        ///     <li>VT: the timestamp that the client should consider the response valid until</li>
        ///     <li>GT: the timestamp that the client should ignore retry errors until</li>
        ///     <li>GR: the number of retry errors that the client should ignore</li>
        ///   </ul>
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
            this.RetryCount = response == PolicyServerResponse.Retry ? this.RetryCount + 1 : 0;

            switch (response)
            {
                case PolicyServerResponse.Licensed:

                    // Update server policy data
                    Dictionary<string, string> extras;
                    if (!PolicyExtensions.TryDecodeExtras(rawData.Extra, out extras))
                    {
                        Debug.WriteLine("Invalid syntax error while decoding extras data from server.");
                    }
                    else
                    {
                        this.SetValidityTimestamp(extras["VT"]);
                        this.SetRetryUntil(extras["GT"]);
                        this.SetMaxRetries(extras["GR"]);
                    }

                    break;
                case PolicyServerResponse.NotLicensed:
                    this.SetValidityTimestamp(DefaultValidityTimestamp);
                    this.SetRetryUntil(DefaultRetryUntil);
                    this.SetMaxRetries(DefaultMaxRetries);
                    break;
            }

            this.LastResponse = response;

            this.preferences.Commit();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Set the max retries value (GR) as received from the server and add to
        ///   preferences. You must manually call PreferenceObfuscator.commit() to
        ///   commit these changes to disk.
        /// </summary>
        /// <param name="retries">
        /// the GR string received
        /// </param>
        private void SetMaxRetries(string retries)
        {
            long r;
            if (!long.TryParse(retries, out r))
            {
                // No response or not parsable, expire immediately
                System.Diagnostics.Debug.WriteLine("Licence retry count (GR) missing, grace period disabled");
            }

            this.MaxRetries = r;
        }

        /// <summary>
        /// Set the retry until timestamp (GT) received from the server and add to
        ///   preferences. You must manually call PreferenceObfuscator.commit() to
        ///   commit these changes to disk.
        /// </summary>
        /// <param name="retry">
        /// the GT string received
        /// </param>
        private void SetRetryUntil(string retry)
        {
            long r;
            if (!long.TryParse(retry, out r))
            {
                // No response or not parsable, expire immediately
                System.Diagnostics.Debug.WriteLine("License retry timestamp (GT) missing, grace period disabled");
            }

            this.RetryUntil = r;
        }

        /// <summary>
        /// Set the last validity timestamp (VT) received from the server and add to
        ///   preferences. You must manually call PreferenceObfuscator.commit() to
        ///   commit these changes to disk.
        /// </summary>
        /// <param name="timestamp">
        /// the VT string received
        /// </param>
        private void SetValidityTimestamp(string timestamp)
        {
            long t;
            if (!long.TryParse(timestamp, out t))
            {
                // No response or not parsable, expire in one minute.
                System.Diagnostics.Debug.WriteLine("License validity timestamp (VT) missing, caching for a minute");
                t = PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute;
            }

            this.ValidityTimestamp = t;
        }

        #endregion
    }
}