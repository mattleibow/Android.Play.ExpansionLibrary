using System;
using System.Collections.Generic;
using Android.Content;

namespace LicenseVerificationLibrary
{
    /// <summary>
    /// The design of the protocol supports n files. Currently the market can
    /// only deliver two files. To accommodate this, we have these two constants,
    /// but the order is the only relevant thing here.
    /// </summary>
    public class ExpansionFileType
    {
        public const long MainFile = 0;
        public const long PatchFile = 1;
    }

    public class ApkExpansionPreferences
    {
        public const string File = "com.android.vending.licensing.APKExpansionPolicy";

        public const string LastResponse = "lastResponse";
        public const string ValidityTimestamp = "validityTimestamp";
        public const string RetryUntil = "retryUntil";
        public const string MaximumRetries = "maxRetries";
        public const string RetryCount = "retryCount";

        public const long DefaultValidityTimestamp = 0L;
        public const long DefaultRetryUntil = 0L;
        public const long DefaultMaximumRetries = 0L;
        public const long DefaultRetryCount = 0L;
    }

    public class ApkExpansionPolicy : IPolicy
    {
        private readonly string[] _expansionFileNames = new string[] {null, null};
        private readonly long[] _expansionFileSizes = new long[] {-1, -1};
        private readonly string[] _expansionUrls = new string[] {null, null};
        private readonly PreferenceObfuscator _preferenceObfuscator;
        private PolicyServerResponse _lastResponse;
        private long _lastResponseTime;
        private long _maxRetries;
        private long _retryCount;
        private long _retryUntil;
        private long _validityTimestamp;
        
        /// <summary>
        /// </summary>
        /// <param name="context">The context for the current application</param>
        /// <param name="obfuscator">An obfuscator to be used with preferences.</param>
        public ApkExpansionPolicy(Context context, IObfuscator obfuscator)
        {
            // Import old values
            ISharedPreferences sp = context.GetSharedPreferences(ApkExpansionPreferences.File, FileCreationMode.Private);
            _preferenceObfuscator = new PreferenceObfuscator(sp, obfuscator);
            string lastResponse = _preferenceObfuscator.GetValue(ApkExpansionPreferences.LastResponse,
                                                                 PolicyServerResponse.Retry.ToString());
            _lastResponse = (PolicyServerResponse) Enum.Parse(typeof (PolicyServerResponse), lastResponse);
            _validityTimestamp = _preferenceObfuscator.GetValue(ApkExpansionPreferences.ValidityTimestamp,
                                                                ApkExpansionPreferences.DefaultValidityTimestamp);
            _retryUntil = _preferenceObfuscator.GetValue(ApkExpansionPreferences.RetryUntil,
                                                         ApkExpansionPreferences.DefaultRetryUntil);
            _maxRetries = _preferenceObfuscator.GetValue(ApkExpansionPreferences.MaximumRetries,
                                                         ApkExpansionPreferences.DefaultMaximumRetries);
            _retryCount = _preferenceObfuscator.GetValue(ApkExpansionPreferences.RetryCount,
                                                         ApkExpansionPreferences.DefaultRetryCount);
        }

        #region IPolicy Members

        /// <summary>
        /// We call this to guarantee that we fetch a fresh policy from the 
        /// server. This is to be used if the URL is invalid.
        ///
        /// Process a new response from the license server.
        /// 
        /// This data will be used for computing future policy decisions. 
        /// The following parameters are processed:
        /// <ul>
        /// <li>VT: the timestamp that the client should consider the response valid until</li>
        /// <li>GT: the timestamp that the client should ignore retry errors until</li>
        /// <li>GR: the number of retry errors that the client should ignore</li>
        /// </ul>
        /// </summary>
        /// <param name="response">the result from validating the server response</param>
        /// <param name="rawData">the raw server response data</param>
        public void ProcessServerResponse(PolicyServerResponse response, ResponseData rawData)
        {
            // Update retry counter
            RetryCount = response == PolicyServerResponse.Retry
                             ? RetryCount + 1
                             : 0;

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

                _lastResponse = response;
                ValidityTimestamp = PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute;

                const string fileUrl = "FILE_URL";
                const string fileName = "FILE_NAME";
                const string fileSize = "FILE_SIZE";

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

                        ValidityTimestamp = l;
                    }
                    else if (key == "GT")
                    {
                        if (l == 0)
                        {
                            // No response or not parseable, expire immediately.
                            System.Diagnostics.Debug.WriteLine("License retry timestamp (GT) missing, grace period disabled");
                        }

                        RetryUntil = l;
                    }
                    else if (key == "GR")
                    {
                        if (l == 0)
                        {
                            // No response or not parseable, immediately.
                            System.Diagnostics.Debug.WriteLine("Licence retry count (GR) missing, grace period disabled");
                        }

                        MaxRetries = l;
                    }
                    else if (key.StartsWith(fileUrl))
                    {
                        var index = int.Parse(key.Substring(fileUrl.Length));
                        SetExpansionUrl(index, value);
                    }
                    else if (key.StartsWith(fileName))
                    {
                        var index = int.Parse(key.Substring(fileName.Length));
                        SetExpansionFileName(index, value);
                    }
                    else if (key.StartsWith(fileSize))
                    {
                        var index = int.Parse(key.Substring(fileSize.Length));
                        SetExpansionFileSize(index, long.Parse(value));
                    }
                }
            }
            else if (response == PolicyServerResponse.NotLicensed)
            {
                // Clear out stale policy data
                ValidityTimestamp = ApkExpansionPreferences.DefaultValidityTimestamp;
                RetryUntil = ApkExpansionPreferences.DefaultRetryUntil;
                MaxRetries = ApkExpansionPreferences.DefaultMaximumRetries;
            }

            SetLastResponse(response);
            _preferenceObfuscator.Commit();
        }

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
        public bool AllowAccess()
        {
            long ts = PolicyExtensions.GetCurrentMilliseconds();
            if (_lastResponse == PolicyServerResponse.Licensed)
            {
                // Check if the LICENSED response occurred within the validity
                // timeout.
                if (ts <= _validityTimestamp)
                {
                    // Cached LICENSED response is still valid.
                    return true;
                }
            }
            else if (_lastResponse == PolicyServerResponse.Retry &&
                     ts < _lastResponseTime + PolicyExtensions.MillisPerMinute)
            {
                // Only allow access if we are within the retry period or we 
                // haven't used up our max retries.
                return ts <= _retryUntil || _retryCount <= _maxRetries;
            }
            return false;
        }

        #endregion

        public void ResetPolicy()
        {
            _preferenceObfuscator.PutString(ApkExpansionPreferences.LastResponse, PolicyServerResponse.Retry.ToString());

            RetryUntil = ApkExpansionPreferences.DefaultRetryUntil;
            MaxRetries = ApkExpansionPreferences.DefaultMaximumRetries;
            RetryCount = ApkExpansionPreferences.DefaultRetryCount;
            ValidityTimestamp = ApkExpansionPreferences.DefaultValidityTimestamp;

            _preferenceObfuscator.Commit();
        }

        /// <summary>
        /// Set the last license response received from the server and add to
        /// preferences. You must manually call PreferenceObfuscator.commit() to
        /// commit these changes to disk.
        /// </summary>
        private void SetLastResponse(PolicyServerResponse response)
        {
            _lastResponseTime = PolicyExtensions.GetCurrentMilliseconds();
            _lastResponse = response;
            _preferenceObfuscator.PutString(ApkExpansionPreferences.LastResponse, response.ToString());
        }

        /// <summary>
        /// Set the current retry count and add to preferences. You must 
        /// changes to disk.C
        /// </summary>
        public long RetryCount
        {
            get { return _retryCount; }
            set
            {
                _retryCount = value;
                _preferenceObfuscator.PutString(ApkExpansionPreferences.RetryCount, _retryCount.ToString());
            }
        }
        
        /// <summary>
        /// Set the retry until timestamp (GT) received from the server and add to
        /// preferences. You must manually call PreferenceObfuscator.commit() to
        /// commit these changes to disk.
        /// </summary>
        public long RetryUntil
        {
            get { return _retryUntil; }
            set
            {
                _retryUntil = value;
                _preferenceObfuscator.PutValue(ApkExpansionPreferences.RetryUntil, value);
            }
        }

        /// <summary>
        /// Set the last validity timestamp (VT) received from the server and add to
        /// preferences. You must manually call PreferenceObfuscator.commit() to
        /// commit these changes to disk.
        /// </summary>
        public long ValidityTimestamp
        {
            get { return _validityTimestamp; }
            set
            {
                _validityTimestamp = value;
                _preferenceObfuscator.PutValue(ApkExpansionPreferences.ValidityTimestamp, value);
            }
        }

        public long LastResponseTime
        {
            get { return _lastResponseTime; }
            set { _lastResponseTime = value; }
        }

        public PolicyServerResponse LastResponse
        {
            get { return _lastResponse; }
            set { _lastResponse = value; }
        }

        /// <summary>
        /// Set the max retries value (GR) as received from the server and add to
        /// preferences. You must manually call PreferenceObfuscator.commit() to
        /// commit these changes to disk.
        /// </summary>
        public long MaxRetries
        {
            get { return _maxRetries; }
            set
            {
                _maxRetries = value;
                _preferenceObfuscator.PutValue(ApkExpansionPreferences.MaximumRetries, value);
            }
        }

        /**
     * Gets the count of expansion URLs. Since expansionURLs are not committed
     * to preferences, this will return zero if there has been no LVL fetch in
     * the current session.
     * 
     * @return the number of expansion URLs. (0,1,2)
     */

        public int GetExpansionUrlCount()
        {
            return _expansionUrls.Length;
        }

        /**
     * Gets the expansion URL. Since these URLs are not committed to
     * preferences, this will always return null if there has not been an LVL
     * fetch in the current session.
     * 
     * @param index
     *            the index of the URL to fetch. This value will be either
     *            MainFile or PatchFile
     * @param URL
     *            the URL to set
     */

        public string GetExpansionUrl(int index)
        {
            var index0 = index;
            if (index0 < _expansionUrls.Length)
            {
                return _expansionUrls[index0];
            }
            return null;
        }

        /**
     * Sets the expansion URL. Expansion URL's are not committed to preferences,
     * but are instead intended to be stored when the license response is
     * processed by the front-end.
     * 
     * @param index
     *            the index of the expansion URL. This value will be either
     *            MainFile or PatchFile
     * @param URL
     *            the URL to set
     */

        public void SetExpansionUrl(int index, string url)
        {
            var index0 = (int) index;
            _expansionUrls[index0] = url;
        }

        public string GetExpansionFileName(int index)
        {
            var index0 = (int)index;
            if (index0 < _expansionFileNames.Length)
            {
                return _expansionFileNames[index0];
            }
            return null;
        }

        public void SetExpansionFileName(int index, string name)
        {
            var index0 = index;
            _expansionFileNames[index0] = name;
        }

        public long GetExpansionFileSize(int index)
        {
            var index0 = index;
            if (index0 < _expansionFileSizes.Length)
            {
                return _expansionFileSizes[index0];
            }
            return -1;
        }

        public void SetExpansionFileSize(int index, long size)
        {
            var index0 = index;
            _expansionFileSizes[index0] = size;
        }
    }
}