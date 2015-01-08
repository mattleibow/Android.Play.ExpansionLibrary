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
        /// The prefs file.
        /// </summary>
        public const string PreferencesFile = "com.android.vending.licensing.ServerManagedPolicy";

        /// <summary>
        /// The preferences.
        /// </summary>
		protected readonly PreferenceObfuscator Obfuscator;

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
            ISharedPreferences sp = context.GetSharedPreferences(PreferencesFile, FileCreationMode.Private);
			this.Obfuscator = new PreferenceObfuscator(sp, obfuscator);

			this.lastResponse = this.Obfuscator.GetValue<PolicyServerResponse>(Preferences.LastResponse, Preferences.DefaultLastResponse);
			this.validityTimestamp = this.Obfuscator.GetValue<long>(Preferences.ValidityTimestamp, Preferences.DefaultValidityTimestamp);
			this.retryUntil = this.Obfuscator.GetValue<long>(Preferences.RetryUntil, Preferences.DefaultRetryUntil);
			this.maxRetries = this.Obfuscator.GetValue<long>(Preferences.MaximumRetries, Preferences.DefaultMaxRetries);
			this.retryCount = this.Obfuscator.GetValue<long>(Preferences.RetryCount, Preferences.DefaultRetryCount);
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

            set
            {
                this.lastResponseTime = PolicyExtensions.GetCurrentMilliseconds();
                this.lastResponse = value;
                this.Obfuscator.PutValue(Preferences.LastResponse, this.lastResponse);
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
        ///   The max retries value (GR) as received from the server
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
				this.Obfuscator.PutValue(Preferences.MaximumRetries, this.maxRetries);
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

            set
            {
                this.retryCount = value;
				this.Obfuscator.PutValue(Preferences.RetryCount, this.retryCount);
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

            set
            {
                this.retryUntil = value;
				this.Obfuscator.PutValue(Preferences.RetryUntil, this.retryUntil);
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

            set
            {
                this.validityTimestamp = value;
				this.Obfuscator.PutValue(Preferences.ValidityTimestamp, this.validityTimestamp);
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
        public virtual bool AllowAccess()
        {
            bool allowed = false;

            long ts = PolicyExtensions.GetCurrentMilliseconds();
            if (this.LastResponse == PolicyServerResponse.Licensed)
            {
                // Check if the LICENSED response occurred within the validity timeout and is still valid.
                allowed = ts <= this.ValidityTimestamp;
            }
            else if (this.LastResponse == PolicyServerResponse.Retry
				&& ts < this.LastResponseTime + PolicyExtensions.MillisPerMinute)
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
		public virtual void ProcessServerResponse(PolicyServerResponse response, ResponseData rawData)
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
					
					// If no response or not parseable, expire in one minute.
					this.ValidityTimestamp = PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute;
					
					foreach (var pair in extras)
					{
						this.ProcessResponseExtra(pair);
					}

                    break;
                case PolicyServerResponse.NotLicensed:
					this.ValidityTimestamp = Preferences.DefaultValidityTimestamp;
					this.RetryUntil = Preferences.DefaultRetryUntil;
					this.MaxRetries = Preferences.DefaultMaxRetries;
                    break;
            }

            this.LastResponse = response;

			this.Obfuscator.Commit();
        }

		/// <summary>
		/// The reset policy.
		/// </summary>
		public virtual void ResetPolicy()
		{
			this.LastResponse = PolicyServerResponse.Retry;
			this.LastResponseTime = 0;
			this.RetryUntil = 0;
			this.MaxRetries = 0;
			this.RetryCount = 0;
			this.ValidityTimestamp = 0;

			this.Obfuscator.Commit();
		}
        
        #endregion

        #region Methods

		/// <summary>
		/// Parse each extra in the response
		/// </summary>
		/// <param name="pair"></param>
		protected virtual void ProcessResponseExtra(KeyValuePair<string, string> pair)
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
		}

		#endregion

		/// <summary>
		/// The apk expansion preferences.
		/// </summary>
		private static class Preferences
		{
			#region Constants and Fields

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

			/// <summary>
			/// The default max retries.
			/// </summary>
			public const long DefaultMaxRetries = 0;

			/// <summary>
			/// The default retry count.
			/// </summary>
			public const long DefaultRetryCount = 0;

			/// <summary>
			/// The default retry until.
			/// </summary>
			public const long DefaultRetryUntil = 0;

			/// <summary>
			/// The default validity timestamp.
			/// </summary>
			public const long DefaultValidityTimestamp = 0;

			/// <summary>
			/// The default validity timestamp.
			/// </summary>
			public const PolicyServerResponse DefaultLastResponse = PolicyServerResponse.Retry;

			#endregion
		}
    }
}