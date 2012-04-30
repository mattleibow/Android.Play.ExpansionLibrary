namespace LicenseVerificationLibrary
{
    public class NullDeviceLimiter : IDeviceLimiter
    {
        #region DeviceLimiter Members

        public PolicyServerResponse IsDeviceAllowed(string userId)
        {
            return PolicyServerResponse.Licensed;
        }

        #endregion
    }
}