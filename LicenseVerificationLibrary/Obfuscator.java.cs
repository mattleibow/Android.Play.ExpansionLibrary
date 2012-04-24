namespace LicenseVerificationLibrary
{
    public interface Obfuscator
    {
        /**
     * Obfuscate a string that is being stored into shared preferences.
     * 
     * @param original
     *            The data that is to be obfuscated.
     * @param key
     *            The key for the data that is to be obfuscated.
     * @return A transformed version of the original data.
     */
        string obfuscate(string original, string key);

        /**
     * Undo the transformation applied to data by the obfuscate() method.
     * 
     * @param original
     *            The data that is to be obfuscated.
     * @param key
     *            The key for the data that is to be obfuscated.
     * @return A transformed version of the original data.
     * @throws ValidationException
     *             Optionally thrown if a data integrity check fails.
     */
        string unobfuscate(string obfuscated, string key);
    }
}