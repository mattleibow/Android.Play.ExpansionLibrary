namespace LicenseVerificationLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Android.Content;
    using Android.Content.PM;
    using Android.OS;
    using Android.Provider;

    using Java.Security;
    using Java.Security.Spec;

    using Debug = System.Diagnostics.Debug;

    /// <summary>
    /// <para>
    /// Client library for Android Market license verifications.
    /// </para>
    /// <para>
    /// The LicenseChecker is configured via a <see cref="IPolicy"/> which contains the
    ///  logic to determine whether a user should have access to the application. For
    ///  example, the IPolicy can define a threshold for allowable number of server or
    ///  client failures before the library reports the user as not having access.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Must also provide the Base64-encoded RSA public key associated with your
    ///  developer account. The public key is obtainable from the publisher site.
    /// </remarks>
    public class LicenseChecker : Java.Lang.Object, IServiceConnection
    {
        #region Constants and Fields

        /// <summary>
        /// The key factory algorithm.
        /// </summary>
        private const string KeyFactoryAlgorithm = "RSA";

        /// <summary>
        ///   Note: For best security, we recommend obfuscating this string that is passed 
        ///   into BindService using another method of your own devising:
        ///   Source String: "com.android.vending.licensing.ILicensingService"
        /// </summary>
        private const string LicensingServiceIntentString = "com.android.vending.licensing.ILicensingService";

        /// <summary>
        /// Timeout value (in milliseconds) for calls to service.
        /// The timeout ms.
        /// </summary>
        private const int TimeoutMs = 10 * 1000;

        /// <summary>
        /// The random.
        /// </summary>
        private static readonly SecureRandom Random;

        /// <summary>
        /// The _checks in progress.
        /// </summary>
        private readonly HashSet<LicenseValidator> checksInProgress;

        /// <summary>
        /// The _context.
        /// </summary>
        private readonly Context context;

        /// <summary>
        /// A handler for running tasks on a background thread. We don't want license processing to block the UI thread.
        /// </summary>
        private readonly Handler handler;

        /// <summary>
        /// The _locker.
        /// </summary>
        private readonly object locker;

        /// <summary>
        /// The _package name.
        /// </summary>
        private readonly string packageName;

        /// <summary>
        /// The _pending checks.
        /// </summary>
        private readonly Queue<LicenseValidator> pendingChecks;

        /// <summary>
        /// The _policy.
        /// </summary>
        private readonly IPolicy policy;

        /// <summary>
        /// The _public key.
        /// </summary>
        private readonly IPublicKey publicKey;

        /// <summary>
        /// The _version code.
        /// </summary>
        private readonly string versionCode;

        /// <summary>
        /// The _licensing service.
        /// </summary>
        private ILicensingService licensingService;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes static members of the <see cref="LicenseChecker"/> class.
        /// </summary>
        static LicenseChecker()
        {
            Random = new SecureRandom();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseChecker"/> class. 
        /// The license checker.
        /// </summary>
        /// <param name="context">
        /// a Context
        /// </param>
        /// <param name="policy">
        /// implementation of IPolicy
        /// </param>
        /// <param name="encodedPublicKey">
        /// Base64-encoded RSA public key
        /// </param>
        /// <exception cref="ArgumentException">
        /// if encodedPublicKey is invalid
        /// </exception>
        public LicenseChecker(Context context, IPolicy policy, string encodedPublicKey)
        {
            this.locker = new object();
            this.checksInProgress = new HashSet<LicenseValidator>();
            this.pendingChecks = new Queue<LicenseValidator>();
            this.context = context;
            this.policy = policy;
            this.publicKey = GeneratePublicKey(encodedPublicKey);
            this.packageName = this.context.PackageName;
            this.versionCode = GetVersionCode(context, this.packageName);
            var handlerThread = new HandlerThread("background thread");
            handlerThread.Start();
            this.handler = new Handler(handlerThread.Looper);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks if the user should have access to the app. Binds the service if necessary.
        /// </summary>
        /// <param name="callback">
        /// The callback.
        /// </param>
        public void CheckAccess(ILicenseCheckerCallback callback)
        {
            lock (this.locker)
            {
                // If we have a valid recent LICENSED response, we can skip asking Market/Play.
                /*
                if (_policy.AllowAccess())
                {
                    System.Diagnostics.Debug.WriteLine("Using cached license response");
                    callback.Allow(PolicyServerResponse.Licensed);
                }
                else
                */
                {
                    var validator = new LicenseValidator(
                        this.policy, new NullDeviceLimiter(), callback, GenerateNumberUsedOnce(), this.packageName, this.versionCode);

                    if (this.licensingService == null)
                    {
                        try
                        {
                            var i = new Intent(LicensingServiceIntentString);

                            if (this.context.BindService(i, this, Bind.AutoCreate))
                            {
                                this.pendingChecks.Enqueue(validator);
                            }
                            else
                            {
                                Debug.WriteLine("Could not bind to service.");
                                this.HandleServiceConnectionError(validator);
                            }
                        }
                        catch (Java.Lang.SecurityException)
                        {
                            callback.ApplicationError(CallbackErrorCode.MissingPermission);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                    else
                    {
                        this.pendingChecks.Enqueue(validator);
                        this.RunChecks();
                    }
                }
            }
        }

        /// <summary>
        /// Inform the library that the context is about to be destroyed, so that any
        ///  open connections can be cleaned up.
        ///  Failure to call this method can result in a crash under certain
        ///  circumstances, such as during screen rotation if an Activity requests the
        ///  license check or when the user exits the application.
        /// </summary>
        public void OnDestroy()
        {
            lock (this.locker)
            {
                this.CleanupService();
                this.handler.Looper.Quit();
            }
        }

        /// <summary>
        /// The on service connected.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="service">
        /// The service.
        /// </param>
        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            lock (this.locker)
            {
                this.licensingService = LicensingServiceStub.AsInterface(service);
                this.RunChecks();
            }
        }

        /// <summary>
        /// The on service disconnected.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public void OnServiceDisconnected(ComponentName name)
        {
            lock (this.locker)
            {
                // Called when the connection with the service has been
                // unexpectedly disconnected. That is, Market crashed.
                // If there are any checks in progress, the timeouts will handle them.
                Debug.WriteLine("Service unexpectedly disconnected.");
                this.licensingService = null;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Generates a nonce (number used once).
        /// </summary>
        /// <returns>
        /// The generate number used once.
        /// </returns>
        private static int GenerateNumberUsedOnce()
        {
            return Random.NextInt();
        }

        /// <summary>
        /// Generates a PublicKey instance from a string containing the Base64-encoded public key.
        /// </summary>
        /// <param name="encodedPublicKey">
        /// Base64-encoded public key
        /// </param>
        /// <returns>
        /// An IPublicKey that is used to verify the server data.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// if encodedPublicKey is invalid
        /// </exception>
        private static IPublicKey GeneratePublicKey(string encodedPublicKey)
        {
            try
            {
                byte[] decodedKey = Convert.FromBase64String(encodedPublicKey);
                KeyFactory keyFactory = KeyFactory.GetInstance(KeyFactoryAlgorithm);

                return keyFactory.GeneratePublic(new X509EncodedKeySpec(decodedKey));
            }
            catch (NoSuchAlgorithmException ex)
            {
                // This won't happen in an Android-compatible environment.
                throw new Exception(ex.Message);
            }
            catch (FormatException)
            {
                Debug.WriteLine("Could not decode public key from Base64.");
                throw;
            }
            catch (InvalidKeySpecException exx)
            {
                Debug.WriteLine("Invalid public key specification.");
                throw new ArgumentException(exx.Message);
            }
        }

        /// <summary>
        /// Get version code for the application package name.
        /// </summary>
        /// <param name="context">
        /// The context used to find the package version code.
        /// </param>
        /// <param name="packageName">
        /// application package name
        /// </param>
        /// <returns>
        /// the version code or an empty string if package was not found
        /// </returns>
        private static string GetVersionCode(Context context, string packageName)
        {
            try
            {
                return context.PackageManager.GetPackageInfo(packageName, 0).VersionCode.ToString();
            }
            catch (PackageManager.NameNotFoundException)
            {
                Debug.WriteLine("Package not found. Could not get version code.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Unbinds service if necessary and removes reference to it.
        /// </summary>
        private void CleanupService()
        {
            if (this.licensingService != null)
            {
                try
                {
                    this.context.UnbindService(this);
                }
                catch
                {
                    // Somehow we've already been unbound. This is a non-fatal error.
                    Debug.WriteLine("Unable to unbind from licensing service (already unbound).");
                }

                this.licensingService = null;
            }
        }

        /// <summary>
        /// The finish check.
        /// </summary>
        /// <param name="validator">
        /// The validator.
        /// </param>
        private void FinishCheck(LicenseValidator validator)
        {
            lock (this.locker)
            {
                this.checksInProgress.Remove(validator);
                if (this.checksInProgress.Any())
                {
                    this.CleanupService();
                }
            }
        }

        /// <summary>
        /// Generates policy response for service connection errors, as a result of disconnections or timeouts.
        /// </summary>
        /// <param name="validator">
        /// The validator.
        /// </param>
        private void HandleServiceConnectionError(LicenseValidator validator)
        {
            lock (this.locker)
            {
                this.policy.ProcessServerResponse(PolicyServerResponse.Retry, null);

                if (this.policy.AllowAccess())
                {
                    validator.GetCallback().Allow(PolicyServerResponse.Retry);
                }
                else
                {
                    validator.GetCallback().DontAllow(PolicyServerResponse.Retry);
                }
            }
        }

        /// <summary>
        /// The run checks.
        /// </summary>
        private void RunChecks()
        {
            while (this.pendingChecks.Any())
            {
                LicenseValidator validator = this.pendingChecks.Dequeue();
                try
                {
                    Debug.WriteLine("Calling CheckLicense on service for " + validator.GetPackageName());
                    this.licensingService.CheckLicense(validator.GetNumberUsedOnce(), validator.GetPackageName(), new ResultListener(validator, this));
                    this.checksInProgress.Add(validator);
                }
                catch (RemoteException e)
                {
                    Debug.WriteLine("RemoteException in CheckLicense call. " + e.Message);
                    this.HandleServiceConnectionError(validator);
                }
            }
        }

        #endregion

        /// <summary>
        /// The result listener.
        /// </summary>
        private class ResultListener : LicenseResultListenerStub
        {
            #region Constants and Fields

            /// <summary>
            /// The _checker.
            /// </summary>
            private readonly LicenseChecker checker;

            /// <summary>
            /// The _license validator.
            /// </summary>
            private readonly LicenseValidator licenseValidator;

            /// <summary>
            /// The _on timeout.
            /// </summary>
            private readonly Action onTimeout;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ResultListener"/> class.
            /// </summary>
            /// <param name="validator">
            /// The validator.
            /// </param>
            /// <param name="checker">
            /// The checker.
            /// </param>
            public ResultListener(LicenseValidator validator, LicenseChecker checker)
            {
                this.checker = checker;
                this.licenseValidator = validator;
                this.onTimeout = delegate
                    {
                        Debug.WriteLine("License check timed out.");

                        this.checker.HandleServiceConnectionError(this.licenseValidator);
                        this.checker.FinishCheck(this.licenseValidator);
                    };
                this.StartTimeout();
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// Runs in IPC thread pool. Post it to the Handler, so we can guarantee
            ///   either this or the timeout runs.
            /// </summary>
            /// <param name="responseCode">
            /// The response code from the server.
            /// </param>
            /// <param name="signedData">
            /// The data from the server.
            /// </param>
            /// <param name="signature">
            /// The signature to use to verify the data.
            /// </param>
            public override void VerifyLicense(ServerResponseCode responseCode, string signedData, string signature)
            {
                this.checker.handler.Post(
                    delegate
                        {
                            Debug.WriteLine("Received license response.");

                            // Make sure it hasn't already timed out.
                            if (this.checker.checksInProgress.Contains(this.licenseValidator))
                            {
                                this.ClearTimeout();
                                this.licenseValidator.Verify(this.checker.publicKey, responseCode, signedData, signature);
                                this.checker.FinishCheck(this.licenseValidator);
                            }

                            // Write debug data to the log
                            this.DebugServerResponseCode(responseCode);
                        });
            }

            #endregion

            #region Methods

            /// <summary>
            /// The clear timeout.
            /// </summary>
            private void ClearTimeout()
            {
                Debug.WriteLine("Clearing license checker timeout.");
                this.checker.handler.RemoveCallbacks(this.onTimeout);
            }

            /// <summary>
            /// The debug server response code.
            /// </summary>
            /// <param name="responseCode">
            /// The response code.
            /// </param>
            [Conditional("DEBUG")]
            private void DebugServerResponseCode(ServerResponseCode responseCode)
            {
                bool logResponse;
                string stringError = null;
                switch (responseCode)
                {
                    case ServerResponseCode.ErrorContactingServer:
                        logResponse = true;
                        stringError = "ERROR_CONTACTING_SERVER";
                        break;
                    case ServerResponseCode.InvalidPackageName:
                        logResponse = true;
                        stringError = "ERROR_INVALID_PACKAGE_NAME";
                        break;
                    case ServerResponseCode.NonMatchingUid:
                        logResponse = true;
                        stringError = "ERROR_NON_MATCHING_UID";
                        break;
                    default:
                        logResponse = false;
                        break;
                }

                if (logResponse)
                {
                    string androidId = Settings.Secure.GetString(this.checker.context.ContentResolver, Settings.Secure.AndroidId);
                    Debug.WriteLine("License Server Failure: " + stringError);
                    Debug.WriteLine("Android ID: " + androidId);
                    Debug.WriteLine("Time: " + DateTime.Now);
                }
            }

            /// <summary>
            /// The start timeout.
            /// </summary>
            private void StartTimeout()
            {
                Debug.WriteLine("Start monitoring license checker timeout.");
                this.checker.handler.PostDelayed(this.onTimeout, TimeoutMs);
            }

            #endregion
        }
    }
}