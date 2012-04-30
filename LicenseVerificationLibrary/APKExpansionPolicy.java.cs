using System;
using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Java.Net;

namespace LicenseVerificationLibrary
{
    public class APKExpansionPolicy : IPolicy
    {
        public const int MAIN_FILE_URL_INDEX = 0;
        public const int PATCH_FILE_URL_INDEX = 1;
        private static string TAG = "APKExpansionPolicy";
        private static string PREFS_FILE = "com.android.vending.licensing.APKExpansionPolicy";
        private static string PREF_LAST_RESPONSE = "lastResponse";
        private static string PREF_VALIDITY_TIMESTAMP = "validityTimestamp";
        private static string PREF_RETRY_UNTIL = "retryUntil";
        private static string PREF_MAX_RETRIES = "maxRetries";
        private static string PREF_RETRY_COUNT = "retryCount";
        private static string DEFAULT_VALIDITY_TIMESTAMP = "0";
        private static string DEFAULT_RETRY_UNTIL = "0";
        private static string DEFAULT_MAX_RETRIES = "0";
        private static string DEFAULT_RETRY_COUNT = "0";

        private readonly string[] mExpansionFileNames = new string[] {null, null};
        private readonly long[] mExpansionFileSizes = new long[] {-1, -1};
        private readonly string[] mExpansionURLs = new string[] {null, null};
        private readonly PreferenceObfuscator mPreferences;
        private PolicyServerResponse mLastResponse;
        private long mLastResponseTime;
        private long mMaxRetries;
        private long mRetryCount;
        private long mRetryUntil;
        private long mValidityTimestamp;

        /**
     * The design of the protocol supports n files. Currently the market can
     * only deliver two files. To accommodate this, we have these two constants,
     * but the order is the only relevant thing here.
     */

        /**
     * @param context
     *            The context for the current application
     * @param obfuscator
     *            An obfuscator to be used with preferences.
     */

        public APKExpansionPolicy(Context context, IObfuscator obfuscator)
        {
            // Import old values
            ISharedPreferences sp = context.GetSharedPreferences(PREFS_FILE, FileCreationMode.Private);
            mPreferences = new PreferenceObfuscator(sp, obfuscator);
            string lastResponse = mPreferences.GetString(PREF_LAST_RESPONSE, PolicyServerResponse.Retry.ToString());
            mLastResponse = (PolicyServerResponse) Enum.Parse(typeof (PolicyServerResponse), lastResponse);
            mValidityTimestamp = long.Parse(mPreferences.GetString(PREF_VALIDITY_TIMESTAMP, DEFAULT_VALIDITY_TIMESTAMP));
            mRetryUntil = long.Parse(mPreferences.GetString(PREF_RETRY_UNTIL, DEFAULT_RETRY_UNTIL));
            mMaxRetries = long.Parse(mPreferences.GetString(PREF_MAX_RETRIES, DEFAULT_MAX_RETRIES));
            mRetryCount = long.Parse(mPreferences.GetString(PREF_RETRY_COUNT, DEFAULT_RETRY_COUNT));
        }

        /**
     * We call this to guarantee that we fetch a fresh policy from the server.
     * This is to be used if the URL is invalid.
     */

        /**
     * Process a new response from the license server.
     * <p>
     * This data will be used for computing future policy decisions. The
     * following parameters are processed:
     * <ul>
     * <li>VT: the timestamp that the client should consider the response valid
     * until
     * <li>GT: the timestamp that the client should ignore retry errors until
     * <li>GR: the number of retry errors that the client should ignore
     * </ul>
     * 
     * @param response
     *            the result from validating the server response
     * @param rawData
     *            the raw server response data
     */

        #region IPolicy Members

        public void ProcessServerResponse(PolicyServerResponse response, ResponseData rawData)
        {
            // Update retry counter
            if (response != PolicyServerResponse.Retry)
            {
                setRetryCount(0);
            }
            else
            {
                setRetryCount(mRetryCount + 1);
            }

            if (response == PolicyServerResponse.Licensed)
            {
                // Update server policy data
                var extras = new Dictionary<string, string>();
                try
                {
                    extras = PolicyExtensions.DecodeExtras(rawData.Extra);
                }
                catch (URISyntaxException e)
                {
                    Log.Warn(TAG, "Invalid syntax error while decoding extras data from server.");
                }
                mLastResponse = response;
                setValidityTimestamp(PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute);
                foreach (string key in extras.Keys)
                {
                    switch (key)
                    {
                        case "VT":
                            setValidityTimestamp(extras[key]);
                            break;
                        case "GT":
                            setRetryUntil(extras[key]);
                            break;
                        case "GR":
                            setMaxRetries(extras[key]);
                            break;
                        default:
                            const string fileUrl = "FILE_URL";
                            const string fileName = "FILE_NAME";
                            const string fileSize = "FILE_SIZE";
                            if (key.StartsWith(fileUrl))
                            {
                                int index = int.Parse(key.Substring(fileUrl.Length)) - 1;
                                setExpansionURL(index, extras[key]);
                            }
                            else if (key.StartsWith(fileName))
                            {
                                int index = int.Parse(key.Substring(fileName.Length)) - 1;
                                setExpansionFileName(index, extras[key]);
                            }
                            else if (key.StartsWith(fileSize))
                            {
                                int index = int.Parse(key.Substring(fileSize.Length)) - 1;
                                setExpansionFileSize(index, long.Parse(extras[key]));
                            }
                            break;
                    }
                }
            }
            else if (response == PolicyServerResponse.NotLicensed)
            {
                // Clear out stale policy data
                setValidityTimestamp(DEFAULT_VALIDITY_TIMESTAMP);
                setRetryUntil(DEFAULT_RETRY_UNTIL);
                setMaxRetries(DEFAULT_MAX_RETRIES);
            }

            setLastResponse(response);
            mPreferences.Commit();
        }

        public bool AllowAccess()
        {
            long ts = PolicyExtensions.GetCurrentMilliseconds();
            if (mLastResponse == PolicyServerResponse.Licensed)
            {
                // Check if the LICENSED response occurred within the validity
                // timeout.
                if (ts <= mValidityTimestamp)
                {
                    // Cached LICENSED response is still valid.
                    return true;
                }
            }
            else if (mLastResponse == PolicyServerResponse.Retry
                     && ts < mLastResponseTime + PolicyExtensions.MillisPerMinute)
            {
                // Only allow access if we are within the retry period or we haven't
                // used up our
                // max retries.
                return (ts <= mRetryUntil || mRetryCount <= mMaxRetries);
            }
            return false;
        }

        #endregion

        public void resetPolicy()
        {
            mPreferences.PutString(PREF_LAST_RESPONSE, PolicyServerResponse.Retry.ToString());
            setRetryUntil(DEFAULT_RETRY_UNTIL);
            setMaxRetries(DEFAULT_MAX_RETRIES);
            setRetryCount(long.Parse(DEFAULT_RETRY_COUNT));
            setValidityTimestamp(DEFAULT_VALIDITY_TIMESTAMP);
            mPreferences.Commit();
        }

        private void setValidityTimestamp(long validityTimestamp)
        {
            setValidityTimestamp(validityTimestamp.ToString());
        }

        /**
     * Set the last license response received from the server and add to
     * preferences. You must manually call PreferenceObfuscator.commit() to
     * commit these changes to disk.
     * 
     * @param l
     *            the response
     */

        private void setLastResponse(PolicyServerResponse l)
        {
            mLastResponseTime = PolicyExtensions.GetCurrentMilliseconds();
            mLastResponse = l;
            mPreferences.PutString(PREF_LAST_RESPONSE, l.ToString());
        }

        /**
     * Set the current retry count and add to preferences. You must manually
     * call PreferenceObfuscator.commit() to commit these changes to disk.
     * 
     * @param c
     *            the new retry count
     */

        private void setRetryCount(long c)
        {
            mRetryCount = c;
            mPreferences.PutString(PREF_RETRY_COUNT, c.ToString());
        }

        public long getRetryCount()
        {
            return mRetryCount;
        }

        /**
     * Set the last validity timestamp (VT) received from the server and add to
     * preferences. You must manually call PreferenceObfuscator.commit() to
     * commit these changes to disk.
     * 
     * @param validityTimestamp
     *            the VT string received
     */

        private void setValidityTimestamp(string validityTimestamp)
        {
            long lValidityTimestamp;
            if (!long.TryParse(validityTimestamp, out lValidityTimestamp))
            {
                // No response or not parseable, expire in one minute.
                Log.Warn(TAG, "License validity timestamp (VT) missing, caching for a minute");
                lValidityTimestamp = PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute;
                validityTimestamp = lValidityTimestamp.ToString();
            }

            mValidityTimestamp = lValidityTimestamp;
            mPreferences.PutString(PREF_VALIDITY_TIMESTAMP, validityTimestamp);
        }

        public long getValidityTimestamp()
        {
            return mValidityTimestamp;
        }

        /**
     * Set the retry until timestamp (GT) received from the server and add to
     * preferences. You must manually call PreferenceObfuscator.commit() to
     * commit these changes to disk.
     * 
     * @param retryUntil
     *            the GT string received
     */

        private void setRetryUntil(string retryUntil)
        {
            long lRetryUntil;
            if (!long.TryParse(retryUntil, out lRetryUntil))
            {
                // No response or not parseable, expire immediately
                Log.Warn(TAG, "License retry timestamp (GT) missing, grace period disabled");
                retryUntil = "0";
                lRetryUntil = 0L;
            }

            mRetryUntil = lRetryUntil;
            mPreferences.PutString(PREF_RETRY_UNTIL, retryUntil);
        }

        public long getRetryUntil()
        {
            return mRetryUntil;
        }

        /**
     * Set the max retries value (GR) as received from the server and add to
     * preferences. You must manually call PreferenceObfuscator.commit() to
     * commit these changes to disk.
     * 
     * @param maxRetries
     *            the GR string received
     */

        private void setMaxRetries(string maxRetries)
        {
            long lMaxRetries;
            if (!long.TryParse(maxRetries, out lMaxRetries))
            {
                // No response or not parseable, expire immediately
                Log.Warn(TAG, "Licence retry count (GR) missing, grace period disabled");
                maxRetries = "0";
                lMaxRetries = 0L;
            }

            mMaxRetries = lMaxRetries;
            mPreferences.PutString(PREF_MAX_RETRIES, maxRetries);
        }

        public long getMaxRetries()
        {
            return mMaxRetries;
        }

        /**
     * Gets the count of expansion URLs. Since expansionURLs are not committed
     * to preferences, this will return zero if there has been no LVL fetch in
     * the current session.
     * 
     * @return the number of expansion URLs. (0,1,2)
     */

        public int getExpansionURLCount()
        {
            return mExpansionURLs.Length;
        }

        /**
     * Gets the expansion URL. Since these URLs are not committed to
     * preferences, this will always return null if there has not been an LVL
     * fetch in the current session.
     * 
     * @param index
     *            the index of the URL to fetch. This value will be either
     *            MAIN_FILE_URL_INDEX or PATCH_FILE_URL_INDEX
     * @param URL
     *            the URL to set
     */

        public string getExpansionURL(int index)
        {
            if (index < mExpansionURLs.Length)
            {
                return mExpansionURLs[index];
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
     *            MAIN_FILE_URL_INDEX or PATCH_FILE_URL_INDEX
     * @param URL
     *            the URL to set
     */

        public void setExpansionURL(int index, string URL)
        {
            mExpansionURLs[index] = URL;
        }

        public string getExpansionFileName(int index)
        {
            if (index < mExpansionFileNames.Length)
            {
                return mExpansionFileNames[index];
            }
            return null;
        }

        public void setExpansionFileName(int index, string name)
        {
            mExpansionFileNames[index] = name;
        }

        public long getExpansionFileSize(int index)
        {
            if (index < mExpansionFileSizes.Length)
            {
                return mExpansionFileSizes[index];
            }
            return -1;
        }

        public void setExpansionFileSize(int index, long size)
        {
            mExpansionFileSizes[index] = size;
        }

        /**
     * {@inheritDoc} This implementation allows access if either:<br>
     * <ol>
     * <li>a LICENSED response was received within the validity period
     * <li>a RETRY response was received in the last minute, and we are under
     * the RETRY count or in the RETRY period.
     * </ol>
     */
    }
}