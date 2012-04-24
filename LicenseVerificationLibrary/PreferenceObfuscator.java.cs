using Android.Content;
using Android.Util;

/**
 * An wrapper for SharedPreferences that transparently performs data
 * obfuscation.
 */

namespace LicenseVerificationLibrary
{
    public class PreferenceObfuscator
    {
        private static string TAG = "PreferenceObfuscator";

        private readonly Obfuscator mObfuscator;
        private readonly ISharedPreferences mPreferences;
        private ISharedPreferencesEditor mEditor;

        /**
     * Constructor.
     * 
     * @param sp
     *            A SharedPreferences instance provided by the system.
     * @param o
     *            The Obfuscator to use when reading or writing data.
     */

        public PreferenceObfuscator(ISharedPreferences sp, Obfuscator o)
        {
            mPreferences = sp;
            mObfuscator = o;
            mEditor = null;
        }

        public void putString(string key, string value)
        {
            if (mEditor == null)
            {
                mEditor = mPreferences.Edit();
            }
            string obfuscatedValue = mObfuscator.obfuscate(value, key);
            mEditor.PutString(key, obfuscatedValue);
        }

        public string getString(string key, string defValue)
        {
            string result;
            string value = mPreferences.GetString(key, null);
            if (value != null)
            {
                try
                {
                    result = mObfuscator.unobfuscate(value, key);
                }
                catch (ValidationException ex)
                {
                    // Unable to unobfuscate, data corrupt or tampered
                    Log.Warn(TAG, "Validation error while reading preference: " + key);
                    result = defValue;
                }
            }
            else
            {
                // Preference not found
                result = defValue;
            }
            return result;
        }

        public void commit()
        {
            if (mEditor != null)
            {
                mEditor.Commit();
                mEditor = null;
            }
        }
    }
}