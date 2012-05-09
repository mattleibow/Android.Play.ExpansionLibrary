namespace LicenseVerificationLibrary
{
    using Android.OS;

    /// <summary>
    /// The i licensing service.
    /// </summary>
    public interface ILicensingService : IInterface
    {
        #region Public Methods and Operators

        /// <summary>
        /// The check license.
        /// </summary>
        /// <param name="nonce">
        /// The nonce.
        /// </param>
        /// <param name="packageName">
        /// The package name.
        /// </param>
        /// <param name="listener">
        /// The listener.
        /// </param>
        void CheckLicense(long nonce, string packageName, ILicenseResultListener listener);

        #endregion
    }
}