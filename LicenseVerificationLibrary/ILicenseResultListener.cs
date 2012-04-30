using Android.OS;

namespace LicenseVerificationLibrary
{
    public interface ILicenseResultListener : IInterface
    {
        void VerifyLicense(ServerResponseCode responseCode, string signedData, string signature);
    }
}