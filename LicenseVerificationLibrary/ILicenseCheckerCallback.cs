namespace LicenseVerificationLibrary
{
    /// <summary>
    /// The i license checker callback.
    /// </summary>
    public interface ILicenseCheckerCallback
    {
        #region Public Methods and Operators

        /// <summary>
        /// Allow use. App should proceed as normal.
        /// </summary>
        /// <param name="reason">
        /// <see cref="PolicyServerResponse.Licensed"/> or 
        /// <see cref="PolicyServerResponse.Retry"/> typically.
        /// (although in theory the policy can return 
        /// <see cref="PolicyServerResponse.NotLicensed"/> here as well) 
        /// </param>
        void Allow(PolicyServerResponse reason);

        /// <summary>
        /// Error in application code. Caller did not call or set up license 
        /// checker correctly. 
        /// Should be considered fatal.
        /// </summary>
        /// <param name="errorCode">
        /// </param>
        void ApplicationError(CallbackErrorCode errorCode);

        /// <summary>
        /// Don't allow use. App should inform user and take appropriate action.
        /// </summary>
        /// <param name="reason">
        /// <see cref="PolicyServerResponse.NotLicensed"/> or 
        /// <see cref="PolicyServerResponse.Retry"/>. 
        /// (although in theory the policy can return 
        /// <see cref="PolicyServerResponse.Licensed"/> here as well - perhaps 
        /// the call to the LVL took too long, for example)
        /// </param>
        void DontAllow(PolicyServerResponse reason);

        #endregion
    }
}