namespace LicenseVerificationLibrary
{
    /// <summary>
    /// Application error codes.
    /// </summary>
    public enum CallbackErrorCode
    {
        /// <summary>
        /// The invalid package name.
        /// </summary>
        InvalidPackageName = 1, 

        /// <summary>
        /// The error non matching uid.
        /// </summary>
        ErrorNonMatchingUid = 2, 

        /// <summary>
        /// The not market managed.
        /// </summary>
        NotMarketManaged = 3, 

        /// <summary>
        /// The check in progress.
        /// </summary>
        CheckInProgress = 4, 

        /// <summary>
        /// The invalid public key.
        /// </summary>
        InvalidPublicKey = 5, 

        /// <summary>
        /// The missing permission.
        /// </summary>
        MissingPermission = 6
    }
}