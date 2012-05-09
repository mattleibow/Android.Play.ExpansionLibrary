namespace LicenseVerificationLibrary
{
    /// <summary>
    /// The null device limiter.
    /// </summary>
    public class NullDeviceLimiter : IDeviceLimiter
    {
        #region Public Methods and Operators

        /// <summary>
        /// The is device allowed.
        /// </summary>
        /// <param name="userId">
        /// The user id.
        /// </param>
        /// <returns>
        /// </returns>
        public PolicyServerResponse IsDeviceAllowed(string userId)
        {
            return PolicyServerResponse.Licensed;
        }

        #endregion
    }
}