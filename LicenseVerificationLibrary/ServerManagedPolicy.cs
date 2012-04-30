using System;
using System.Collections.Generic;
using Android.Content;
using Android.Util;

namespace LicenseVerificationLibrary
{
    ///<summary>
    ///  Default policy. All policy decisions are based off of response data received
    ///  from the licensing service. Specifically, the licensing server sends the
    ///  following information: response validity period, error retry period, and
    ///  error retry count.
    ///
    ///  These values will vary based on the the way the application is configured in
    ///  the Android Market publishing console, such as whether the application is
    ///  marked as free or is within its refund period, as well as how often an
    ///  application is checking with the licensing service.
    ///
    ///  Developers who need more fine grained control over their application's
    ///  licensing policy should implement a custom IPolicy.
    ///</summary>
    public class ServerManagedPolicy : IPolicy
    {
        private const string Tag = "ServerManagedPolicy";
        private const string PrefsFile = "com.android.vending.licensing.ServerManagedPolicy";
        private const string PrefLastResponse = "lastResponse";
        private const string PrefValidityTimestamp = "validityTimestamp";
        private const string PrefRetryUntil = "retryUntil";
        private const string PrefMaxRetries = "maxRetries";
        private const string PrefRetryCount = "retryCount";
        private const string DefaultValidityTimestamp = "0";
        private const string DefaultRetryUntil = "0";
        private const string DefaultMaxRetries = "0";
        private const string DefaultRetryCount = "0";

        private readonly PreferenceObfuscator _preferences;

        private PolicyServerResponse _lastResponse;
        private long _lastResponseTime;
        private long _maxRetries;
        private long _retryCount;
        private long _retryUntil;
        private long _validityTimestamp;

        /// <summary>
        /// </summary>
        /// <param name = "context">The context for the current application</param>
        /// <param name = "obfuscator">An obfuscator to be used with preferences.</param>
        public ServerManagedPolicy(Context context, IObfuscator obfuscator)
        {
            // Import old values
            ISharedPreferences sp = context.GetSharedPreferences(PrefsFile, FileCreationMode.Private);
            _preferences = new PreferenceObfuscator(sp, obfuscator);
            string lastResponse = _preferences.GetString(PrefLastResponse, ((int) PolicyServerResponse.Retry).ToString());
            LastResponse = (PolicyServerResponse) Enum.Parse(typeof (PolicyServerResponse), lastResponse);
            ValidityTimestamp = long.Parse(_preferences.GetString(PrefValidityTimestamp, DefaultValidityTimestamp));
            RetryUntil = long.Parse(_preferences.GetString(PrefRetryUntil, DefaultRetryUntil));
            MaxRetries = long.Parse(_preferences.GetString(PrefMaxRetries, DefaultMaxRetries));
            RetryCount = long.Parse(_preferences.GetString(PrefRetryCount, DefaultRetryCount));
        }

        /// <summary>
        ///   Set the last license response received from the server and add to
        ///   preferences. You must manually call PreferenceObfuscator.commit() to
        ///   commit these changes to disk.
        /// </summary>
        public PolicyServerResponse LastResponse
        {
            get { return _lastResponse; }
            private set
            {
                _lastResponseTime = PolicyExtensions.GetCurrentMilliseconds();
                _lastResponse = value;
                _preferences.PutString(PrefLastResponse, _lastResponse.ToString());
            }
        }


        /// <summary>
        ///   Set the current retry count and add to preferences. You must manually
        ///   call PreferenceObfuscator.commit() to commit these changes to disk.
        /// </summary>
        public long RetryCount
        {
            get { return _retryCount; }
            private set
            {
                _retryCount = value;
                _preferences.PutString(PrefRetryCount, _retryCount.ToString());
            }
        }

        /// <summary>
        ///   The last validity timestamp (VT) received from the server
        /// </summary>
        public long ValidityTimestamp
        {
            get { return _validityTimestamp; }
            private set
            {
                _validityTimestamp = value;
                _preferences.PutString(PrefValidityTimestamp, _validityTimestamp.ToString());
            }
        }

        /// <summary>
        ///   The retry until timestamp (GT) received from the server.
        /// </summary>
        public long RetryUntil
        {
            get { return _retryUntil; }
            private set
            {
                _retryUntil = value;
                _preferences.PutString(PrefRetryUntil, _retryUntil.ToString());
            }
        }

        /// <summary>
        ///   The max retries value (GR) as received from the server
        /// </summary>
        public long MaxRetries
        {
            get { return _maxRetries; }
            private set
            {
                _maxRetries = value;
                _preferences.PutString(PrefMaxRetries, _maxRetries.ToString());
            }
        }

        #region IPolicy Members

        /// <summary>
        ///   Process a new response from the license server.
        /// 
        ///   This data will be used for computing future policy decisions. The
        ///   following parameters are processed:
        ///   <ul>
        ///     <li>VT: the timestamp that the client should consider the response valid until</li>
        ///     <li>GT: the timestamp that the client should ignore retry errors until</li>
        ///     <li>GR: the number of retry errors that the client should ignore</li>
        ///   </ul>
        /// </summary>
        /// <param name = "response">the result from validating the server response</param>
        /// <param name = "rawData">the raw server response data</param>
        public void ProcessServerResponse(PolicyServerResponse response, ResponseData rawData)
        {
            // Update retry counter
            RetryCount = response == PolicyServerResponse.Retry ? RetryCount + 1 : 0;

            switch (response)
            {
                case PolicyServerResponse.Licensed:
                    // Update server policy data
                    Dictionary<string, string> extras;
                    if (!PolicyExtensions.TryDecodeExtras(rawData.Extra, out extras))
                    {
                        Log.Warn(Tag, "Invalid syntax error while decoding extras data from server.");
                    }
                    else
                    {
                        SetValidityTimestamp(extras["VT"]);
                        SetRetryUntil(extras["GT"]);
                        SetMaxRetries(extras["GR"]);
                    }
                    break;
                case PolicyServerResponse.NotLicensed:
                    SetValidityTimestamp(DefaultValidityTimestamp);
                    SetRetryUntil(DefaultRetryUntil);
                    SetMaxRetries(DefaultMaxRetries);
                    break;
            }

            LastResponse = response;

            _preferences.Commit();
        }


        /// <summary>
        ///   This implementation allows access if either:
        ///   <ol>
        ///     <li>a LICENSED response was received within the validity period</li>
        ///     <li>a RETRY response was received in the last minute, and we are under
        ///       the RETRY count or in the RETRY period.</li>
        ///   </ol>
        /// </summary>
        public bool AllowAccess()
        {
            bool allowed = false;

            long ts = PolicyExtensions.GetCurrentMilliseconds();
            if (LastResponse == PolicyServerResponse.Licensed)
            {
                // Check if the LICENSED response occurred within the validity timeout and is still valid.
                allowed = ts <= ValidityTimestamp;
            }
            if (LastResponse == PolicyServerResponse.Retry && ts < _lastResponseTime + PolicyExtensions.MillisPerMinute)
            {
                // Only allow access if we are within the retry period or we haven't used up our max retries.
                allowed = ts <= RetryUntil || RetryCount <= MaxRetries;
            }

            return allowed;
        }

        #endregion

        /// <summary>
        ///   Set the last validity timestamp (VT) received from the server and add to
        ///   preferences. You must manually call PreferenceObfuscator.commit() to
        ///   commit these changes to disk.
        /// </summary>
        /// <param name = "validityTimestamp">the VT string received</param>
        private void SetValidityTimestamp(string validityTimestamp)
        {
            long timestamp;
            if (!long.TryParse(validityTimestamp, out timestamp))
            {
                // No response or not parsable, expire in one minute.
                Log.Warn(Tag, "License validity timestamp (VT) missing, caching for a minute");
                timestamp = PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute;
            }
            ValidityTimestamp = timestamp;
        }

        /// <summary>
        ///   Set the retry until timestamp (GT) received from the server and add to
        ///   preferences. You must manually call PreferenceObfuscator.commit() to
        ///   commit these changes to disk.
        /// </summary>
        /// <param name = "retryUntil">the GT string received</param>
        private void SetRetryUntil(string retryUntil)
        {
            long lRetryUntil;
            if (!long.TryParse(retryUntil, out lRetryUntil))
            {
                // No response or not parsable, expire immediately
                Log.Warn(Tag, "License retry timestamp (GT) missing, grace period disabled");
            }
            RetryUntil = lRetryUntil;
        }

        /// <summary>
        ///   Set the max retries value (GR) as received from the server and add to
        ///   preferences. You must manually call PreferenceObfuscator.commit() to
        ///   commit these changes to disk.
        /// </summary>
        /// <param name = "maxRetries">the GR string received</param>
        private void SetMaxRetries(string maxRetries)
        {
            long lMaxRetries;
            if (!long.TryParse(maxRetries, out lMaxRetries))
            {
                // No response or not parsable, expire immediately
                Log.Warn(Tag, "Licence retry count (GR) missing, grace period disabled");
            }
            MaxRetries = lMaxRetries;
        }
    }
}