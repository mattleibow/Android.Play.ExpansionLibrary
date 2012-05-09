namespace LicenseVerificationLibrary
{
    /// <summary>
    /// The i obfuscator.
    /// </summary>
    public interface IObfuscator
    {
        #region Public Methods and Operators

        /// <summary>
        /// Obfuscate a string that is being stored into shared preferences.
        /// </summary>
        /// <param name="original">
        /// The data that is to be obfuscated.
        /// </param>
        /// <param name="key">
        /// The key for the data that is to be obfuscated.
        /// </param>
        /// <returns>
        /// A transformed version of the original data.
        /// </returns>
        string Obfuscate(string original, string key);

        /// <summary>
        /// Undo the transformation applied to data by the 
        /// <see cref="Obfuscate"/> method.
        /// </summary>
        /// <param name="obfuscated">
        /// The data that is to be obfuscated.
        /// </param>
        /// <param name="key">
        /// The key for the data that is to be obfuscated.
        /// </param>
        /// <returns>
        /// A transformed version of the original data.
        /// </returns>
        /// <exception cref="ValidationException">
        /// Optionally thrown if a data integrity check fails.
        /// </exception>
        string Unobfuscate(string obfuscated, string key);

        #endregion
    }
}