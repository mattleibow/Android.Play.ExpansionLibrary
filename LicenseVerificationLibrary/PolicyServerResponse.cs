namespace LicenseVerificationLibrary
{
    /// <summary>
    /// Change these values to make it more difficult for tools to automatically
    ///   strip LVL protection from your APK.
    /// </summary>
    public enum PolicyServerResponse
    {
        /// <summary>
        ///   The server returned back a valid license response
        /// </summary>
        Licensed = 0x0100, 

        /// <summary>
        ///   The server returned back a valid license response that indicated 
        ///   that the user definitively is not licensed
        /// </summary>
        NotLicensed = 0x0231, 

        /// <summary>
        ///   The license response was unable to be determined 
        ///   - perhaps as a result of faulty networking
        /// </summary>
        Retry = 0x0123
    }
}