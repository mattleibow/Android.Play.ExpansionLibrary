namespace LicenseVerificationLibrary
{
    public interface ILicenseCheckerCallback
    {
        /// <summary>
        ///    Allow use. App should proceed as normal.
        /// </summary>
        /// <param name="reason">
        ///    PolicyServerResponse.LICENSED or PolicyServerResponse.RETRY typically.
        ///    (although in theory the policy can return PolicyServerResponse.NOT_LICENSED here as well) 
        /// </param>
        void Allow(PolicyServerResponse reason);

        /// <summary>
        ///    Don't allow use. App should inform user and take appropriate action.
        /// </summary>
        /// <param name="reason">
        ///    PolicyServerResponse.NOT_LICENSED or PolicyServerResponse.RETRY. (although in theory the
        ///    policy can return PolicyServerResponse.LICENSED here as well - perhaps the
        ///    call to the LVL took too long, for example)
        /// </param>
        void DontAllow(PolicyServerResponse reason);

        /// <summary>
        /// Error in application code. Caller did not call or set up license checker correctly. Should be considered fatal.
        /// </summary>
        /// <param name="errorCode"></param>
        void ApplicationError(CallbackErrorCode errorCode);
    }

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