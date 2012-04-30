namespace LicenseVerificationLibrary
{
    /// <summary>
    ///   Server response codes.
    /// </summary>
    public enum ServerResponseCode
    {
        Licensed = 0x0,
        NotLicensed = 0x1,
        LicensedOldKey = 0x2,
        NotMarketManaged = 0x3,
        ServerFailure = 0x4,
        OverQuota = 0x5,

        ErrorContactingServer = 0x101,
        InvalidPackageName = 0x102,
        NonMatchingUid = 0x103
    }
}