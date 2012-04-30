namespace LicenseVerificationLibrary
{
    public interface IDeviceLimiter
    {
        /// <summary>
        ///    Checks if this device is allowed to use the given user's license.
        /// </summary>
        /// <param name="userId">
        ///    the user whose license the server responded with.
        /// </param>
        /// <returns>
        ///    LICENSED if the device is allowed, NOT_LICENSED if not, RETRY if an error occurs.
        /// </returns>
        PolicyServerResponse IsDeviceAllowed(string userId);
    }
}