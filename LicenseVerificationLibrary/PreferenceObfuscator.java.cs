using System.Diagnostics;
using Android.Content;

namespace LicenseVerificationLibrary
{
    /// <summary>
    ///   An wrapper for SharedPreferences that transparently performs data
    ///   obfuscation.
    /// </summary>
    public class PreferenceObfuscator
    {
        private readonly IObfuscator _obfuscator;
        private readonly ISharedPreferences _preferences;
        private ISharedPreferencesEditor _editor;

        /// <summary>
        ///   Constructor.
        /// </summary>
        /// <param name = "sp">A SharedPreferences instance provided by the system.</param>
        /// <param name = "o">The Obfuscator to use when reading or writing data.</param>
        public PreferenceObfuscator(ISharedPreferences sp, IObfuscator o)
        {
            _preferences = sp;
            _obfuscator = o;
            _editor = null;
        }

        public void PutString(string key, string value)
        {
            if (_editor == null)
            {
                _editor = _preferences.Edit();
            }
            _editor.PutString(key, _obfuscator.Obfuscate(value, key));
        }

        public string GetString(string key, string defValue)
        {
            string result = defValue;
            string value = _preferences.GetString(key, null);

            if (value != null)
            {
                try
                {
                    result = _obfuscator.Unobfuscate(value, key);
                }
                catch (ValidationException)
                {
                    // Unable to unobfuscate, data corrupt or tampered
                    Debug.WriteLine("Validation error while reading preference: " + key);
                }
            }

            return result;
        }

        public void Commit()
        {
            if (_editor != null)
            {
                _editor.Commit();
                _editor = null;
            }
        }
    }
}