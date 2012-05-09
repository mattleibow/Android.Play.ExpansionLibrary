namespace LicenseVerificationLibrary
{
    /// <summary>
    /// Server response codes.
    /// </summary>
    public enum ServerResponseCode
    {
        /// <summary>
        /// The licensed.
        /// </summary>
        Licensed = 0x0, 

        /// <summary>
        /// The not licensed.
        /// </summary>
        NotLicensed = 0x1, 

        /// <summary>
        /// The licensed old key.
        /// </summary>
        LicensedOldKey = 0x2, 

        /// <summary>
        /// The not market managed.
        /// </summary>
        NotMarketManaged = 0x3, 

        /// <summary>
        /// The server failure.
        /// </summary>
        ServerFailure = 0x4, 

        /// <summary>
        /// The over quota.
        /// </summary>
        OverQuota = 0x5, 

        /// <summary>
        /// The error contacting server.
        /// </summary>
        ErrorContactingServer = 0x101, 

        /// <summary>
        /// The invalid package name.
        /// </summary>
        InvalidPackageName = 0x102, 

        /// <summary>
        /// The non matching uid.
        /// </summary>
        NonMatchingUid = 0x103
    }
}