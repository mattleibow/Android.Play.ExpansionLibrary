using Android.OS;

namespace LicenseVerificationLibrary
{
    public interface ILicensingService : IInterface
    {
        void CheckLicense(long nonce, string packageName, ILicenseResultListener listener);
    }
}
