using Android.Util;
using Java.Lang;
using Java.Security;
using System;

namespace LicenseVerificationLibrary
{
    /// <summary>
    /// Contains data related to a licensing request and methods to verify and process the response.
    /// </summary>
    internal class LicenseValidator
    {
        private const string Tag = "LicenseValidator";
        private const string SignatureAlgorithm = "SHA1withRSA";

        private readonly ILicenseCheckerCallback _licenseCheckerCallback;
        private readonly IDeviceLimiter _deviceLimiter;
        private readonly int _numberUsedOnce;
        private readonly string _packageName;
        private readonly IPolicy _policy;
        private readonly string _versionCode;

        internal LicenseValidator(IPolicy policy, IDeviceLimiter deviceLimiter, ILicenseCheckerCallback callback, 
                                  int nonce, string packageName, string versionCode)
        {
            _policy = policy;
            _deviceLimiter = deviceLimiter;
            _licenseCheckerCallback = callback;
            _numberUsedOnce = nonce;
            _packageName = packageName;
            _versionCode = versionCode;
        }

        public ILicenseCheckerCallback GetCallback()
        {
            return _licenseCheckerCallback;
        }

        public int GetNumberUsedOnce()
        {
            return _numberUsedOnce;
        }

        public string GetPackageName()
        {
            return _packageName;
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
            System.Diagnostics.Debug.WriteLine(Tag + ".Verify");

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
                    Signature sig = Signature.GetInstance(SignatureAlgorithm);
                    sig.InitVerify(publicKey);
                    sig.Update(new Java.Lang.String(signedData).GetBytes());

                    if (!sig.Verify(Convert.FromBase64String(signature)))
                    {
                        System.Diagnostics.Debug.WriteLine(Tag + " : " + "Signature verification failed.");
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
                catch (FormatException)
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : " + "Could not Base64-decode signature.");
                    HandleInvalidResponse();
                    return;
                }

                // Parse and validate response.
                try
                {
                    data = ResponseData.Parse(signedData);
                }
                catch (IllegalArgumentException)
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : " + "Could not parse response.");
                    HandleInvalidResponse();
                    return;
                }

                if (data.ResponseCode != responseCode)
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : " + "Response codes don't match.");
                    HandleInvalidResponse();
                    return;
                }

                if (data.NumberUsedOnce != _numberUsedOnce)
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : " + "NumberUsedOnce doesn't match.");
                    HandleInvalidResponse();
                    return;
                }

                if (data.PackageName != (_packageName))
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : " + "Package name doesn't match.");
                    HandleInvalidResponse();
                    return;
                }

                if (data.VersionCode != (_versionCode))
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : " + "Version codes don't match.");
                    HandleInvalidResponse();
                    return;
                }

                // Application-specific user identifier.
                userId = data.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : " + "User identifier is empty.");
                    HandleInvalidResponse();
                    return;
                }
            }

            switch (responseCode)
            {
                case ServerResponseCode.Licensed:
                case ServerResponseCode.LicensedOldKey:
                    PolicyServerResponse limiterResponse = _deviceLimiter.IsDeviceAllowed(userId);
                    HandleResponse(limiterResponse, data);
                    break;
                case ServerResponseCode.NotLicensed:
                    HandleResponse(PolicyServerResponse.NotLicensed, data);
                    break;
                case ServerResponseCode.ErrorContactingServer:
                    Log.Warn(Tag, "Error contacting licensing server.");
                    HandleResponse(PolicyServerResponse.Retry, data);
                    break;
                case ServerResponseCode.ServerFailure:
                    Log.Warn(Tag, "An error has occurred on the licensing server.");
                    HandleResponse(PolicyServerResponse.Retry, data);
                    break;
                case ServerResponseCode.OverQuota:
                    Log.Warn(Tag, "Licensing server is refusing to talk to this device, over quota.");
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
                    System.Diagnostics.Debug.WriteLine(Tag + " : " + "Unknown response code for license check.");
                    HandleInvalidResponse();
                    break;
            }
        }

        /// <summary>
        /// Confers with policy and calls appropriate callback method.
        /// </summary>
        private void HandleResponse(PolicyServerResponse response, ResponseData rawData)
        {
            System.Diagnostics.Debug.WriteLine(Tag + " : " + "LicenseValidator.handleResponse-"+response);
            // Update policy data and increment retry counter (if needed)
            _policy.ProcessServerResponse(response, rawData);

            // Given everything we know, including cached data, ask the policy if we
            // should grant
            // access.
            if (_policy.AllowAccess())
            {
                _licenseCheckerCallback.Allow(response);
            }
            else
            {
                _licenseCheckerCallback.DontAllow(response);
            }
        }

        private void HandleApplicationError(CallbackErrorCode code)
        {
            _licenseCheckerCallback.ApplicationError(code);
        }

        private void HandleInvalidResponse()
        {
            _licenseCheckerCallback.DontAllow(PolicyServerResponse.NotLicensed);
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