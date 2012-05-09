namespace LicenseVerificationLibrary
{
    using System;
    using System.Diagnostics;

    using Android.Content;

    /// <summary>
    /// An wrapper for SharedPreferences that transparently performs data
    ///   obfuscation.
    /// </summary>
    public class PreferenceObfuscator
    {
        #region Constants and Fields

        /// <summary>
        /// The obfuscator.
        /// </summary>
        private readonly IObfuscator obfuscator;

        /// <summary>
        /// The preferences.
        /// </summary>
        private readonly ISharedPreferences preferences;

        /// <summary>
        /// The editor.
        /// </summary>
        private ISharedPreferencesEditor editor;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PreferenceObfuscator"/> class. 
        /// Constructor.
        /// </summary>
        /// <param name="sp">
        /// A SharedPreferences instance provided by the system.
        /// </param>
        /// <param name="o">
        /// The Obfuscator to use when reading or writing data.
        /// </param>
        public PreferenceObfuscator(ISharedPreferences sp, IObfuscator o)
        {
            this.preferences = sp;
            this.obfuscator = o;
            this.editor = null;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The commit.
        /// </summary>
        public void Commit()
        {
            if (this.editor != null)
            {
                this.editor.Commit();
                this.editor = null;
            }
        }

        /// <summary>
        /// The get string.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="defValue">
        /// The def value.
        /// </param>
        /// <returns>
        /// The get string.
        /// </returns>
        public string GetString(string key, string defValue)
        {
            string result = defValue;
            string value = this.preferences.GetString(key, null);

            if (value != null)
            {
                try
                {
                    result = this.obfuscator.Unobfuscate(value, key);
                }
                catch (ValidationException ex)
                {
                    // Unable to unobfuscate, data corrupt or tampered
                    Debug.WriteLine("Validation error while reading preference: " + key);
                    Debug.WriteLine(ex.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// The get value.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="defValue">
        /// The def value.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public T GetValue<T>(string key, T defValue)
        {
            return (T)Convert.ChangeType(this.GetString(key, defValue.ToString()), typeof(T));
        }

        /// <summary>
        /// The put string.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public void PutString(string key, string value)
        {
            if (this.editor == null)
            {
                this.editor = this.preferences.Edit();
            }

            this.editor.PutString(key, this.obfuscator.Obfuscate(value, key));
        }

        /// <summary>
        /// The put value.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        public void PutValue<T>(string key, T value)
        {
            this.PutString(key, value.ToString());
        }

        #endregion
    }
}