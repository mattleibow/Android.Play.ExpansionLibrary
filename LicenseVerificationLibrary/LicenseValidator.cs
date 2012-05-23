namespace LicenseVerificationLibrary
{
    using System;

    using Java.Lang;
    using Java.Security;

    using LicenseVerificationLibrary.DeviceLimiter;
    using LicenseVerificationLibrary.Policy;

    /// <summary>
    /// Contains data related to a licensing request and methods to verify and process the response.
    /// </summary>
    internal class LicenseValidator
    {
        #region Constants and Fields

        /// <summary>
        /// The signature algorithm.
        /// </summary>
        private const string SignatureAlgorithm = "SHA1withRSA";

        /// <summary>
        /// The device limiter.
        /// </summary>
        private readonly IDeviceLimiter deviceLimiter;

        /// <summary>
        /// The license checker callback.
        /// </summary>
        private readonly ILicenseCheckerCallback licenseCheckerCallback;

        /// <summary>
        /// The number used once.
        /// </summary>
        private readonly int numberUsedOnce;

        /// <summary>
        /// The package name.
        /// </summary>
        private readonly string packageName;

        /// <summary>
        /// The policy.
        /// </summary>
        private readonly IPolicy policy;

        /// <summary>
        /// The version code.
        /// </summary>
        private readonly string versionCode;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseValidator"/> class.
        /// </summary>
        /// <param name="policy">
        /// The policy.
        /// </param>
        /// <param name="deviceLimiter">
        /// The device limiter.
        /// </param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        /// <param name="nonce">
        /// The nonce.
        /// </param>
        /// <param name="packageName">
        /// The package name.
        /// </param>
        /// <param name="versionCode">
        /// The version code.
        /// </param>
        internal LicenseValidator(
            IPolicy policy, 
            IDeviceLimiter deviceLimiter, 
            ILicenseCheckerCallback callback, 
            int nonce, 
            string packageName, 
            string versionCode)
        {
            this.policy = policy;
            this.deviceLimiter = deviceLimiter;
            this.licenseCheckerCallback = callback;
            this.numberUsedOnce = nonce;
            this.packageName = packageName;
            this.versionCode = versionCode;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get callback.
        /// </summary>
        /// <returns>
        /// </returns>
        public ILicenseCheckerCallback GetCallback()
        {
            return this.licenseCheckerCallback;
        }

        /// <summary>
        /// The get number used once.
        /// </summary>
        /// <returns>
        /// The get number used once.
        /// </returns>
        public int GetNumberUsedOnce()
        {
            return this.numberUsedOnce;
        }

        /// <summary>
        /// The get package name.
        /// </summary>
        /// <returns>
        /// The get package name.
        /// </returns>
        public string GetPackageName()
        {
            return this.packageName;
        }

        /// <summary>
        /// Verifies the response from server and calls appropriate callback method.
        /// </summary>
        /// <param name="publicKey">
        /// public key associated with the developer account
        /// </param>
        /// <param name="responseCode">
        /// server response code
        /// </param>
        /// <param name="signedData">
        /// signed data from server
        /// </param>
        /// <param name="signature">
        /// server signature
        /// </param>
        public void Verify(IPublicKey publicKey, ServerResponseCode responseCode, string signedData, string signature)
        {
            string userId = null;

            // Skip signature check for unsuccessful requests
            ResponseData data = null;
            if (responseCode == ServerResponseCode.Licensed || responseCode == ServerResponseCode.NotLicensed
                || responseCode == ServerResponseCode.LicensedOldKey)
            {
                // Verify signature.
                try
                {
                    Signature sig = Signature.GetInstance(SignatureAlgorithm);
                    sig.InitVerify(publicKey);
                    sig.Update(new Java.Lang.String(signedData).GetBytes());

                    if (!sig.Verify(Convert.FromBase64String(signature)))
                    {
                        System.Diagnostics.Debug.WriteLine("Signature verification failed.");
                        this.HandleInvalidResponse();
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
                    this.HandleApplicationError(CallbackErrorCode.InvalidPublicKey);
                    return;
                }
                catch (SignatureException e)
                {
                    throw new RuntimeException(e);
                }
                catch (FormatException)
                {
                    System.Diagnostics.Debug.WriteLine("Could not Base64-decode signature.");
                    this.HandleInvalidResponse();
                    return;
                }

                // Parse and validate response.
                try
                {
                    data = ResponseData.Parse(signedData);
                }
                catch (IllegalArgumentException)
                {
                    System.Diagnostics.Debug.WriteLine("Could not parse response.");
                    this.HandleInvalidResponse();
                    return;
                }

                if (data.ResponseCode != responseCode)
                {
                    System.Diagnostics.Debug.WriteLine("Response codes don't match.");
                    this.HandleInvalidResponse();
                    return;
                }

                if (data.NumberUsedOnce != this.numberUsedOnce)
                {
                    System.Diagnostics.Debug.WriteLine("NumberUsedOnce doesn't match.");
                    this.HandleInvalidResponse();
                    return;
                }

                if (data.PackageName != this.packageName)
                {
                    System.Diagnostics.Debug.WriteLine("Package name doesn't match.");
                    this.HandleInvalidResponse();
                    return;
                }

                if (data.VersionCode != this.versionCode)
                {
                    System.Diagnostics.Debug.WriteLine("Version codes don't match.");
                    this.HandleInvalidResponse();
                    return;
                }

                // Application-specific user identifier.
                userId = data.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    System.Diagnostics.Debug.WriteLine("User identifier is empty.");
                    this.HandleInvalidResponse();
                    return;
                }
            }

            switch (responseCode)
            {
                case ServerResponseCode.Licensed:
                case ServerResponseCode.LicensedOldKey:
                    PolicyServerResponse limiterResponse = this.deviceLimiter.IsDeviceAllowed(userId);
                    this.HandleResponse(limiterResponse, data);
                    break;
                case ServerResponseCode.NotLicensed:
                    this.HandleResponse(PolicyServerResponse.NotLicensed, data);
                    break;
                case ServerResponseCode.ErrorContactingServer:
                    System.Diagnostics.Debug.WriteLine("Error contacting licensing server.");
                    this.HandleResponse(PolicyServerResponse.Retry, data);
                    break;
                case ServerResponseCode.ServerFailure:
                    System.Diagnostics.Debug.WriteLine("An error has occurred on the licensing server.");
                    this.HandleResponse(PolicyServerResponse.Retry, data);
                    break;
                case ServerResponseCode.OverQuota:
                    System.Diagnostics.Debug.WriteLine(
                        "Licensing server is refusing to talk to this device, over quota.");
                    this.HandleResponse(PolicyServerResponse.Retry, data);
                    break;
                case ServerResponseCode.InvalidPackageName:
                    this.HandleApplicationError(CallbackErrorCode.InvalidPackageName);
                    break;
                case ServerResponseCode.NonMatchingUid:
                    this.HandleApplicationError(CallbackErrorCode.ErrorNonMatchingUid);
                    break;
                case ServerResponseCode.NotMarketManaged:
                    this.HandleApplicationError(CallbackErrorCode.NotMarketManaged);
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unknown response code for license check.");
                    this.HandleInvalidResponse();
                    break;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The handle application error.
        /// </summary>
        /// <param name="code">
        /// The code.
        /// </param>
        private void HandleApplicationError(CallbackErrorCode code)
        {
            this.licenseCheckerCallback.ApplicationError(code);
        }

        /// <summary>
        /// The handle invalid response.
        /// </summary>
        private void HandleInvalidResponse()
        {
            this.licenseCheckerCallback.DontAllow(PolicyServerResponse.NotLicensed);
        }

        /// <summary>
        /// Confers with policy and calls appropriate callback method.
        /// </summary>
        /// <param name="response">
        /// The response.
        /// </param>
        /// <param name="rawData">
        /// The raw Data.
        /// </param>
        private void HandleResponse(PolicyServerResponse response, ResponseData rawData)
        {
            // Update policy data and increment retry counter (if needed)
            this.policy.ProcessServerResponse(response, rawData);

            // Given everything we know, including cached data, ask the policy if we
            // should grant
            // access.
            if (this.policy.AllowAccess())
            {
                this.licenseCheckerCallback.Allow(response);
            }
            else
            {
                this.licenseCheckerCallback.DontAllow(response);
            }
        }

        #endregion
    }
}