namespace LicenseVerificationLibrary
{
    public class NullDeviceLimiter : DeviceLimiter
    {
        #region DeviceLimiter Members

        public PolicyServerResponse isDeviceAllowed(string userId)
        {
            return PolicyServerResponse.Licensed;
        }

        #endregion
    }
}