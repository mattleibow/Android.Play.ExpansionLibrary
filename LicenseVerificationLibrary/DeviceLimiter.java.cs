namespace LicenseVerificationLibrary
{
    public interface DeviceLimiter
    {
        /**
        * Checks if this device is allowed to use the given user's license.
        * 
        * @param userId
        *            the user whose license the server responded with
        * @return LICENSED if the device is allowed, NOT_LICENSED if not, RETRY if
        *         an error occurs
        */
        PolicyServerResponse isDeviceAllowed(string userId);
    }
}