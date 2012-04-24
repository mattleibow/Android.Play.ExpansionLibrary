using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Java.Lang;
using Java.Net;
using Enum = System.Enum;

namespace LicenseVerificationLibrary
{
    /**
     * Default policy. All policy decisions are based off of response data received
     * from the licensing service. Specifically, the licensing server sends the
     * following information: response validity period, error retry period, and
     * error retry count.
     * <p>
     * These values will vary based on the the way the application is configured in
     * the Android Market publishing console, such as whether the application is
     * marked as free or is within its refund period, as well as how often an
     * application is checking with the licensing service.
     * <p>
     * Developers who need more fine grained control over their application's
     * licensing policy should implement a custom IPolicy.
     */

    public class ServerManagedPolicy : IPolicy
    {
        private static string TAG = "ServerManagedPolicy";
        private static string PREFS_FILE = "com.android.vending.licensing.ServerManagedPolicy";
        private static string PREF_LAST_RESPONSE = "lastResponse";
        private static string PREF_VALIDITY_TIMESTAMP = "validityTimestamp";
        private static string PREF_RETRY_UNTIL = "retryUntil";
        private static string PREF_MAX_RETRIES = "maxRetries";
        private static string PREF_RETRY_COUNT = "retryCount";
        private static string DEFAULT_VALIDITY_TIMESTAMP = "0";
        private static string DEFAULT_RETRY_UNTIL = "0";
        private static string DEFAULT_MAX_RETRIES = "0";
        private static string DEFAULT_RETRY_COUNT = "0";
        private readonly PreferenceObfuscator mPreferences;
        private PolicyServerResponse mLastResponse;
        private long mLastResponseTime;

        private long mMaxRetries;
        private long mRetryCount;
        private long mRetryUntil;
        private long mValidityTimestamp;

        /**
     * @param context
     *            The context for the current application
     * @param obfuscator
     *            An obfuscator to be used with preferences.
     */

        public ServerManagedPolicy(Context context, Obfuscator obfuscator)
        {
            // Import old values
            ISharedPreferences sp = context.GetSharedPreferences(PREFS_FILE, FileCreationMode.Private);
            mPreferences = new PreferenceObfuscator(sp, obfuscator);
            string lastResponse = mPreferences.getString(PREF_LAST_RESPONSE, ((int) PolicyServerResponse.Retry).ToString());
            mLastResponse = (PolicyServerResponse) Enum.Parse(typeof (PolicyServerResponse), lastResponse);
            mValidityTimestamp = long.Parse(mPreferences.getString(PREF_VALIDITY_TIMESTAMP, DEFAULT_VALIDITY_TIMESTAMP));
            mRetryUntil = long.Parse(mPreferences.getString(PREF_RETRY_UNTIL, DEFAULT_RETRY_UNTIL));
            mMaxRetries = long.Parse(mPreferences.getString(PREF_MAX_RETRIES, DEFAULT_MAX_RETRIES));
            mRetryCount = long.Parse(mPreferences.getString(PREF_RETRY_COUNT, DEFAULT_RETRY_COUNT));
        }

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
                    extras = PolicyExtensions.DecodeExtras(rawData.extra);
                }
                catch (URISyntaxException e)
                {
                    Log.Warn(TAG, "Invalid syntax error while decoding extras data from server.");
                }
                mLastResponse = response;
                setValidityTimestamp(extras["VT"]);
                setRetryUntil(extras["GT"]);
                setMaxRetries(extras["GR"]);
            }
            else if (response == PolicyServerResponse.NotLicensed)
            {
                // Clear out stale policy data
                setValidityTimestamp(DEFAULT_VALIDITY_TIMESTAMP);
                setRetryUntil(DEFAULT_RETRY_UNTIL);
                setMaxRetries(DEFAULT_MAX_RETRIES);
            }

            setLastResponse(response);
            mPreferences.commit();
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
            mPreferences.putString(PREF_LAST_RESPONSE, l.ToString());
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
            mPreferences.putString(PREF_RETRY_COUNT, c.ToString());
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
            try
            {
                lValidityTimestamp = long.Parse(validityTimestamp);
            }
            catch (NumberFormatException e)
            {
                // No response or not parsable, expire in one minute.
                Log.Warn(TAG, "License validity timestamp (VT) missing, caching for a minute");
                lValidityTimestamp = PolicyExtensions.GetCurrentMilliseconds() + PolicyExtensions.MillisPerMinute;
                validityTimestamp = lValidityTimestamp.ToString();
            }

            mValidityTimestamp = lValidityTimestamp;
            mPreferences.putString(PREF_VALIDITY_TIMESTAMP, validityTimestamp);
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
            try
            {
                lRetryUntil = long.Parse(retryUntil);
            }
            catch (NumberFormatException e)
            {
                // No response or not parsable, expire immediately
                Log.Warn(TAG, "License retry timestamp (GT) missing, grace period disabled");
                retryUntil = "0";
                lRetryUntil = 0L;
            }

            mRetryUntil = lRetryUntil;
            mPreferences.putString(PREF_RETRY_UNTIL, retryUntil);
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
            try
            {
                lMaxRetries = long.Parse(maxRetries);
            }
            catch (NumberFormatException e)
            {
                // No response or not parsable, expire immediately
                Log.Warn(TAG, "Licence retry count (GR) missing, grace period disabled");
                maxRetries = "0";
                lMaxRetries = 0L;
            }

            mMaxRetries = lMaxRetries;
            mPreferences.putString(PREF_MAX_RETRIES, maxRetries);
        }

        public long getMaxRetries()
        {
            return mMaxRetries;
        }

        /**
     * {@inheritDoc}
     * 
     * This implementation allows access if either:<br>
     * <ol>
     * <li>a LICENSED response was received within the validity period
     * <li>a RETRY response was received in the last minute, and we are under
     * the RETRY count or in the RETRY period.
     * </ol>
     */
    }
}