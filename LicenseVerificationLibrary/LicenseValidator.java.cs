using Android.Util;
using Java.Lang;
using Java.Security;

namespace LicenseVerificationLibrary
{
    /// <summary>
    /// Contains data related to a licensing request and methods to verify and process the response.
    /// </summary>
    internal class LicenseValidator
    {
        private static string TAG = "LicenseValidator";
        private static string SIGNATURE_ALGORITHM = "SHA1withRSA";

        private readonly ILicenseCheckerCallback mCallback;
        private readonly DeviceLimiter mDeviceLimiter;
        private readonly int mNonce;
        private readonly string mPackageName;
        private readonly IPolicy mPolicy;
        private readonly string mVersionCode;

        internal LicenseValidator(IPolicy policy, DeviceLimiter deviceLimiter, ILicenseCheckerCallback callback, 
                                  int nonce, string packageName, string versionCode)
        {
            mPolicy = policy;
            mDeviceLimiter = deviceLimiter;
            mCallback = callback;
            mNonce = nonce;
            mPackageName = packageName;
            mVersionCode = versionCode;
        }

        public ILicenseCheckerCallback GetCallback()
        {
            return mCallback;
        }

        public int GetNumberUsedOnce()
        {
            return mNonce;
        }

        public string GetPackageName()
        {
            return mPackageName;
        }

        /// <summary>
        /// Verifies the response from server and calls appropriate callback method.
        /// </summary>
        /// <param name="publicKey">public key associated with the developer account</param>
        /// <param name="responseCode">server response code</param>
        /// <param name="signedData">signed data from server</param>
        /// <param name="signature">server signature</param>
        public void Verify(IPublicKey publicKey, ServerResponseCode responseCode, string signedData, string signature)
        {
            Log.Info("LicenseChecker", "LicenseValidator.verify()");
            string userId = null;
            // Skip signature check for unsuccessful requests
            ResponseData data = null;
            if (responseCode == ServerResponseCode.Licensed ||
                responseCode == ServerResponseCode.NotLicensed ||
                responseCode == ServerResponseCode.LicensedOldKey)
            {
                // Verify signature.
                try
                {
                    Signature sig = Signature.GetInstance(SIGNATURE_ALGORITHM);
                    sig.InitVerify(publicKey);
                    sig.Update(new String(signedData).GetBytes());

                    if (!sig.Verify(Base64.Decode(signature, Base64.Default)))
                    {
                        System.Diagnostics.Debug.WriteLine(TAG + " : " + "Signature verification failed.");
                        HandleInvalidResponse();
                        return;
                    }
                }
                catch (NoSuchAlgorithmException e)
                {
                    // This can't happen on an Android compatible device.
                    throw new RuntimeException(e);
                }
                catch (InvalidKeyException)
                {
                    HandleApplicationError(CallbackErrorCode.InvalidPublicKey);
                    return;
                }
                catch (SignatureException e)
                {
                    throw new RuntimeException(e);
                }
                catch (IllegalArgumentException)
                {
                    System.Diagnostics.Debug.WriteLine(TAG + " : " + "Could not Base64-decode signature.");
                    HandleInvalidResponse();
                    return;
                }

                // Parse and validate response.
                try
                {
                    data = ResponseData.parse(signedData);
                }
                catch (IllegalArgumentException)
                {
                    System.Diagnostics.Debug.WriteLine(TAG + " : " + "Could not parse response.");
                    HandleInvalidResponse();
                    return;
                }

                if (data.responseCode != responseCode)
                {
                    System.Diagnostics.Debug.WriteLine(TAG + " : " + "Response codes don't match.");
                    HandleInvalidResponse();
                    return;
                }

                if (data.nonce != mNonce)
                {
                    System.Diagnostics.Debug.WriteLine(TAG + " : " + "Nonce doesn't match.");
                    HandleInvalidResponse();
                    return;
                }

                if (data.packageName != (mPackageName))
                {
                    System.Diagnostics.Debug.WriteLine(TAG + " : " + "Package name doesn't match.");
                    HandleInvalidResponse();
                    return;
                }

                if (data.versionCode != (mVersionCode))
                {
                    System.Diagnostics.Debug.WriteLine(TAG + " : " + "Version codes don't match.");
                    HandleInvalidResponse();
                    return;
                }

                // Application-specific user identifier.
                userId = data.userId;
                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine(TAG + " : " + "User identifier is empty.");
                    HandleInvalidResponse();
                    return;
                }
            }

            switch (responseCode)
            {
                case ServerResponseCode.Licensed:
                case ServerResponseCode.LicensedOldKey:
                    PolicyServerResponse limiterResponse = mDeviceLimiter.isDeviceAllowed(userId);
                    HandleResponse(limiterResponse, data);
                    break;
                case ServerResponseCode.NotLicensed:
                    HandleResponse(PolicyServerResponse.NotLicensed, data);
                    break;
                case ServerResponseCode.ErrorContactingServer:
                    Log.Warn(TAG, "Error contacting licensing server.");
                    HandleResponse(PolicyServerResponse.Retry, data);
                    break;
                case ServerResponseCode.ServerFailure:
                    Log.Warn(TAG, "An error has occurred on the licensing server.");
                    HandleResponse(PolicyServerResponse.Retry, data);
                    break;
                case ServerResponseCode.OverQuota:
                    Log.Warn(TAG, "Licensing server is refusing to talk to this device, over quota.");
                    HandleResponse(PolicyServerResponse.Retry, data);
                    break;
                case ServerResponseCode.InvalidPackageName:
                    HandleApplicationError(CallbackErrorCode.InvalidPackageName);
                    break;
                case ServerResponseCode.NonMatchingUid:
                    HandleApplicationError(CallbackErrorCode.ErrorNonMatchingUid);
                    break;
                case ServerResponseCode.NotMarketManaged:
                    HandleApplicationError(CallbackErrorCode.NotMarketManaged);
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine(TAG + " : " + "Unknown response code for license check.");
                    HandleInvalidResponse();
                    break;
            }
        }

        /// <summary>
        /// Confers with policy and calls appropriate callback method.
        /// </summary>
        private void HandleResponse(PolicyServerResponse response, ResponseData rawData)
        {
            System.Diagnostics.Debug.WriteLine(TAG + " : " + "LicenseValidator.handleResponse()");
            // Update policy data and increment retry counter (if needed)
            mPolicy.ProcessServerResponse(response, rawData);

            // Given everything we know, including cached data, ask the policy if we
            // should grant
            // access.
            if (mPolicy.AllowAccess())
            {
                mCallback.Allow(response);
            }
            else
            {
                mCallback.DontAllow(response);
            }
        }

        private void HandleApplicationError(CallbackErrorCode code)
        {
            mCallback.ApplicationError(code);
        }

        private void HandleInvalidResponse()
        {
            mCallback.DontAllow(PolicyServerResponse.NotLicensed);
        }
    }

    /// <summary>
    ///   Server response codes.
    /// </summary>
    public enum ServerResponseCode
    {
        Licensed = 0x0,
        NotLicensed = 0x1,
        LicensedOldKey = 0x2,
        NotMarketManaged = 0x3,
        ServerFailure = 0x4,
        OverQuota = 0x5,

        ErrorContactingServer = 0x101,
        InvalidPackageName = 0x102,
        NonMatchingUid = 0x103
    }
}