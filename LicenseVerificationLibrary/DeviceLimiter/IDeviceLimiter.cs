namespace LicenseVerificationLibrary.DeviceLimiter
{
    /// <summary>
    /// The i device limiter.
    /// </summary>
    public interface IDeviceLimiter
    {
        #region Public Methods and Operators

        /// <summary>
        /// Checks if this device is allowed to use the given user's license.
        /// </summary>
        /// <param name="userId">
        /// the user whose license the server responded with.
        /// </param>
        /// <returns>
        /// <see cref="PolicyServerResponse.Licensed"/> if the device is 
        /// allowed, <see cref="PolicyServerResponse.NotLicensed"/> if not, 
        /// <see cref="PolicyServerResponse.Retry"/> if an error occurs.
        /// </returns>
        PolicyServerResponse IsDeviceAllowed(string userId);

        #endregion
    }
}