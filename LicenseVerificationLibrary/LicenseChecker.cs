using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Util;
using Java.Lang;
using Java.Security;
using Java.Security.Spec;

namespace LicenseVerificationLibrary
{
    ///<summary>
    ///  Client library for Android Market license verifications.
    ///
    ///  The LicenseChecker is configured via a <see cref = "IPolicy" /> which contains the
    ///  logic to determine whether a user should have access to the application. For
    ///  example, the IPolicy can define a threshold for allowable number of server or
    ///  client failures before the library reports the user as not having access.
    ///
    ///  Must also provide the Base64-encoded RSA public key associated with your
    ///  developer account. The public key is obtainable from the publisher site.
    ///</summary>
    public class LicenseChecker : Java.Lang.Object, IServiceConnection
    {
        private const string KeyFactoryAlgorithm = "RSA";
        private const string Tag = "LicenseChecker";
        // Timeout value (in milliseconds) for calls to service.
        private const int TimeoutMs = 10*1000;

        private static readonly SecureRandom Random;

        private readonly HashSet<LicenseValidator> _checksInProgress;
        private readonly Context _context;
        // A handler for running tasks on a background thread. We don't want license processing to block the UI thread.
        private readonly Handler _handler;
        private readonly object _locker;
        private readonly string _packageName;
        private readonly Queue<LicenseValidator> _pendingChecks;
        private readonly IPolicy _policy;
        private readonly IPublicKey _publicKey;
        private readonly string _versionCode;

        private ILicensingService _licensingService;

        static LicenseChecker()
        {
            Random = new SecureRandom();
        }

        /// <summary>
        /// </summary>
        /// <param name = "context">a Context</param>
        /// <param name = "policy">implementation of IPolicy</param>
        /// <param name = "encodedPublicKey">Base64-encoded RSA public key</param>
        /// <exception cref = "IllegalArgumentException">if encodedPublicKey is invalid</exception>
        public LicenseChecker(Context context, IPolicy policy, string encodedPublicKey)
        {
            _locker = new object();
            _checksInProgress = new HashSet<LicenseValidator>();
            _pendingChecks = new Queue<LicenseValidator>();
            _context = context;
            _policy = policy;
            _publicKey = GeneratePublicKey(encodedPublicKey);
            _packageName = _context.PackageName;
            _versionCode = GetVersionCode(context, _packageName);
            var handlerThread = new HandlerThread("background thread");
            handlerThread.Start();
            _handler = new Handler(handlerThread.Looper);
        }

        /// <summary>
        ///   Note: For best security, we recommend obfuscating this string that is passed 
        ///   into BindService using another method of your own devising:
        /// 
        ///   Source String: "com.android.vending.licensing.ILicensingService"
        /// </summary>
        private static string LicensingServiceIntentString
        {
            get { return "com.android.vending.licensing.ILicensingService"; }
        }

        #region IServiceConnection Members

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            lock (_locker)
            {
                _licensingService = LicensingServiceStub.AsInterface(service);
                RunChecks();
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            lock (_locker)
            {
                // Called when the connection with the service has been
                // unexpectedly disconnected. That is, Market crashed.
                // If there are any checks in progress, the timeouts will handle them.
                Log.Warn(Tag, "Service unexpectedly disconnected.");
                _licensingService = null;
            }
        }

        #endregion

        /// <summary>
        ///   Generates a PublicKey instance from a string containing the Base64-encoded public key.
        /// </summary>
        /// <param name = "encodedPublicKey">Base64-encoded public key</param>
        /// <returns></returns>
        /// <exception cref = "IllegalArgumentException">if encodedPublicKey is invalid</exception>
        private static IPublicKey GeneratePublicKey(string encodedPublicKey)
        {
            try
            {
                byte[] decodedKey = Base64.Decode(encodedPublicKey, Base64.Default);
                KeyFactory keyFactory = KeyFactory.GetInstance(KeyFactoryAlgorithm);

                return keyFactory.GeneratePublic(new X509EncodedKeySpec(decodedKey));
            }
            catch (NoSuchAlgorithmException ex)
            {
                // This won't happen in an Android-compatible environment.
                throw new RuntimeException(ex);
            }
            catch (IllegalArgumentException)
            {
                System.Diagnostics.Debug.WriteLine(Tag + " : Could not decode from Base64.");
                throw;
            }
            catch (InvalidKeySpecException exx)
            {
                System.Diagnostics.Debug.WriteLine(Tag + " : Invalid key specification.");
                throw new IllegalArgumentException(exx);
            }
        }

        /// <summary>
        ///   Checks if the user should have access to the app. Binds the service if necessary.
        /// </summary>
        public void CheckAccess(ILicenseCheckerCallback callback)
        {
            lock (_locker)
            {
                // If we have a valid recent LICENSED response, we can skip asking Market/Play.
                if (_policy.AllowAccess())
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : Using cached license response");
                    callback.Allow(PolicyServerResponse.Licensed);
                }
                else
                {
                    var validator = new LicenseValidator(_policy,
                                                         new NullDeviceLimiter(),
                                                         callback,
                                                         GenerateNumberUsedOnce(),
                                                         _packageName,
                                                         _versionCode);

                    if (_licensingService == null)
                    {
                        System.Diagnostics.Debug.WriteLine(Tag + " : Binding to licensing service.");
                        try
                        {
                            var i = new Intent(LicensingServiceIntentString);
                            bool bindResult = _context.BindService(i, this, Bind.AutoCreate);

                            if (bindResult)
                            {
                                _pendingChecks.Enqueue(validator);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine(Tag + " : Could not bind to service.");
                                HandleServiceConnectionError(validator);
                            }
                        }
                        catch (SecurityException)
                        {
                            callback.ApplicationError(CallbackErrorCode.MissingPermission);
                        }
                        catch (IllegalArgumentException ex)
                        {
                            ex.PrintStackTrace();
                        }
                    }
                    else
                    {
                        _pendingChecks.Enqueue(validator);
                        RunChecks();
                    }
                }
            }
        }

        private void RunChecks()
        {
            while (_pendingChecks.Any())
            {
                LicenseValidator validator = _pendingChecks.Dequeue();
                try
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : Calling CheckLicense on service for " + validator.GetPackageName());
                    _licensingService.CheckLicense(validator.GetNumberUsedOnce(), validator.GetPackageName(), new ResultListener(validator, this));
                    _checksInProgress.Add(validator);
                }
                catch (RemoteException e)
                {
                    System.Diagnostics.Debug.WriteLine(Tag + " : RemoteException in CheckLicense call. " + e.Message);
                    HandleServiceConnectionError(validator);
                }
            }
        }

        private void FinishCheck(LicenseValidator validator)
        {
            lock (_locker)
            {
                _checksInProgress.Remove(validator);
                if (_checksInProgress.Any())
                {
                    CleanupService();
                }
            }
        }

        /// <summary>
        ///   Generates policy response for service connection errors, as a result of disconnections or timeouts.
        /// </summary>
        private void HandleServiceConnectionError(LicenseValidator validator)
        {
            lock (_locker)
            {
                _policy.ProcessServerResponse(PolicyServerResponse.Retry, null);

                if (_policy.AllowAccess())
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
        ///   Unbinds service if necessary and removes reference to it.
        /// </summary>
        private void CleanupService()
        {
            if (_licensingService != null)
            {
                try
                {
                    _context.UnbindService(this);
                }
                catch (IllegalArgumentException)
                {
                    // Somehow we've already been unbound. This is a non-fatal error.
                    System.Diagnostics.Debug.WriteLine(Tag + " : Unable to unbind from licensing service (already unbound)");
                }
                _licensingService = null;
            }
        }

        ///<summary>
        ///  Inform the library that the context is about to be destroyed, so that any
        ///  open connections can be cleaned up.
        ///
        ///  Failure to call this method can result in a crash under certain
        ///  circumstances, such as during screen rotation if an Activity requests the
        ///  license check or when the user exits the application.
        ///</summary>
        public void OnDestroy()
        {
            lock (_locker)
            {
                CleanupService();
                _handler.Looper.Quit();
            }
        }

        /// <summary>
        ///   Generates a nonce (number used once).
        /// </summary>
        private static int GenerateNumberUsedOnce()
        {
            return Random.NextInt();
        }

        /// <summary>
        ///   Get version code for the application package name.
        /// </summary>
        /// <param name = "context"></param>
        /// <param name = "packageName">application package name</param>
        /// <returns>the version code or an empty string if package was not found</returns>
        private static string GetVersionCode(Context context, string packageName)
        {
            try
            {
                return context.PackageManager.GetPackageInfo(packageName, 0).VersionCode.ToString();
            }
            catch (PackageManager.NameNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine(Tag + " : Package not found. could not get version code.");
                return string.Empty;
            }
        }

        #region Nested type: ResultListener

        private class ResultListener : LicenseResultListenerStub
        {
            private readonly LicenseChecker _checker;
            private readonly LicenseValidator _licenseValidator;
            private readonly Runnable _onTimeout;

            public ResultListener(LicenseValidator validator, LicenseChecker checker)
            {
                System.Diagnostics.Debug.WriteLine(Tag + " : LicenseValidator.ResultListener.ctor()");
                _checker = checker;
                _licenseValidator = validator;
                _onTimeout = new Runnable(delegate
                                              {
                                                  System.Diagnostics.Debug.WriteLine(Tag + " : Check timed out.");

                                                  _checker.HandleServiceConnectionError(_licenseValidator);
                                                  _checker.FinishCheck(_licenseValidator);
                                              });
                StartTimeout();
            }
            
            /// <summary>
            /// Runs in IPC thread pool. Post it to the Handler, so we can guarantee
            /// either this or the timeout runs.
            /// </summary>
            /// <param name="responseCode"></param>
            /// <param name="signedData"></param>
            /// <param name="signature"></param>
            public override void VerifyLicense(ServerResponseCode responseCode, string signedData, string signature)
            {
                _checker._handler.Post(new Runnable(() => GetValue(responseCode, signedData, signature)));
            }

            private void GetValue(ServerResponseCode responseCode, string signedData, string signature)
            {
                System.Diagnostics.Debug.WriteLine(Tag + " : Received response.");

                // Make sure it hasn't already timed out.
                if (_checker._checksInProgress.Contains(_licenseValidator))
                {
                    ClearTimeout();
                    _licenseValidator.Verify(_checker._publicKey, responseCode, signedData, signature);
                    _checker.FinishCheck(_licenseValidator);
                }

                // Write debug data to the log
                DebugServerResponseCode(responseCode);
            }

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
                    string androidId = Settings.Secure.GetString(_checker._context.ContentResolver, Settings.Secure.AndroidId);
                    System.Diagnostics.Debug.WriteLine(Tag + " : Server Failure: " + stringError);
                    System.Diagnostics.Debug.WriteLine(Tag + " : Android ID: " + androidId);
                    System.Diagnostics.Debug.WriteLine(Tag + " : Time: " + DateTime.Now);
                }
            }

            private void StartTimeout()
            {
                System.Diagnostics.Debug.WriteLine(Tag + " : Start monitoring timeout.");
                _checker._handler.PostDelayed(_onTimeout, TimeoutMs);
            }

            private void ClearTimeout()
            {
                System.Diagnostics.Debug.WriteLine(Tag + " : Clearing timeout.");
                _checker._handler.RemoveCallbacks(_onTimeout);
            }
        }

        #endregion
    }
}