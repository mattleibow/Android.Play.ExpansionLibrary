namespace LicenseVerificationLibrary
{
    using Android.OS;

    /// <summary>
    /// The i license result listener.
    /// </summary>
    public interface ILicenseResultListener : IInterface
    {
        #region Public Methods and Operators

        /// <summary>
        /// The verify license.
        /// </summary>
        /// <param name="responseCode">
        /// The response code.
        /// </param>
        /// <param name="signedData">
        /// The signed data.
        /// </param>
        /// <param name="signature">
        /// The signature.
        /// </param>
        void VerifyLicense(ServerResponseCode responseCode, string signedData, string signature);

        #endregion
    }
}