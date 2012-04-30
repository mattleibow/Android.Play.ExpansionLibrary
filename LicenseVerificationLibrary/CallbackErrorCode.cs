namespace LicenseVerificationLibrary
{
    /// <summary>
    /// Application error codes.
    /// </summary>
    public enum CallbackErrorCode
    {
        InvalidPackageName = 1,
        ErrorNonMatchingUid = 2,
        NotMarketManaged = 3,
        CheckInProgress = 4,
        InvalidPublicKey = 5,
        MissingPermission = 6
    }
}