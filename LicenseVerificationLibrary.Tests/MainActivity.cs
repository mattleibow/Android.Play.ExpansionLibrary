using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Uri = Android.Net.Uri;

namespace LicenseVerificationLibrary.Tests
{
    /**
 * Welcome to the world of Android Market licensing. We're so glad to have you
 * onboard!
 * <p>
 * The first thing you need to do is get your hands on your public key.
 * Update the BASE64_PUBLIC_KEY constant below with your encoded public key,
 * which you can find on the
 * <a href="http://market.android.com/publish/editProfile">Edit Profile</a>
 * page of the Market publisher site.
 * <p>
 * Log in with the same account on your Cupcake (1.5) or higher phone or
 * your FroYo (2.2) emulator with the Google add-ons installed. Change the
 * test response on the Edit Profile page, press Save, and see how this
 * application responds when you check your license.
 * <p>
 * After you get this sample running, peruse the
 * <a href="http://developer.android.com/guide/publishing/licensing.html">
 * licensing documentation.</a>
 */

    [Activity(Label = "LicenseVerificationLibrary.Tests", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private static string BASE64_PUBLIC_KEY =
            "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqSEPO6frjPZ/qdSTT80dCBjsHZouZGadBRwlg9g34ueC6j4F348dy0Xgo4NdKX39pSX1RNl0kGaxX6sg04bp4qx6RfwVyD1CPSEYdWldkuAQ9aNaQZ/yq6V+lmrqaKfJJuh1olqtsK8VVnvJ48Q+VwkIaT5CXhqeRAyZRXMEmEGPTNybSYVf5P90CxdSRwpae/w3S9rzuXOnfUhLKc9WmovRLQ8GzXYzhbNBzbWrK0NE+iXdxDGOZPDQPiLEaU2KliaWOBGO+2Cx5MSXZ3Xlm7e0Yo3F4x8BpMDQHs+3RSYTEaMvQk/t4sfMbA4xCzAP57cl6Ae6SbWU46mk+lqDeQIDAQAB";

        // Generate your own 20 random bytes, and put them here.
        private static readonly byte[] SALT = new byte[] {46, 65, 30, 128, 103, 57, 74, 64, 51, 88, 95, 45, 77, 117, 36, 113, 11, 32, 64, 89};

        private Button mCheckLicenseButton;

        private LicenseChecker mChecker;
        // A handler on the UI thread.
        private Handler mHandler;
        private ILicenseCheckerCallback mLicenseCheckerCallback;
        private TextView mStatusText;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.IndeterminateProgress);
            SetContentView(Resource.Layout.main);

            mStatusText = FindViewById<TextView>(Resource.Id.status_text);
            mCheckLicenseButton = FindViewById<Button>(Resource.Id.check_license_button);
            mCheckLicenseButton.Click += delegate {
                doCheck();
            };

            var runTests = FindViewById<Button>(Resource.Id.RunTests);
            runTests.Click += delegate {
                new ServerManagedPolicyTest(this).Execute();
                new StrictPolicyTest(this).Execute();
                new APKExpansionPolicyTest(this).Execute();
                new ObfuscatedPreferencesTest(this).Execute();
                new AesObfuscatorTest(this).Execute();
            };

            mHandler = new Handler();

            // Try to use more data here. ANDROID_ID is a single point of attack.
            string deviceId = Settings.Secure.GetString(ContentResolver, Settings.Secure.AndroidId);

            // Library calls this when it's done.
            mLicenseCheckerCallback = new MyLicenseCheckerCallback(this);
            // Construct the LicenseChecker with a policy.
            mChecker = new LicenseChecker(this,
                                          new ServerManagedPolicy(this, 
                                                                  new AesObfuscator(SALT, PackageName, deviceId)),
                                          BASE64_PUBLIC_KEY);
            doCheck();
        }

        protected Dialog onCreateDialog(int id)
        {
            bool bRetry = id == 1;
            EventHandler<DialogClickEventArgs> eventHandler = delegate
                                                                  {
                                                                      if (bRetry)
                                                                      {
                                                                          doCheck();
                                                                      }
                                                                      else
                                                                      {
                                                                          Uri uri = Uri.Parse("http://market.android.com/details?id=" + PackageName);
                                                                          var marketIntent = new Intent(Intent.ActionView, uri);
                                                                          StartActivity(marketIntent);
                                                                      }
                                                                  };

            return new AlertDialog.Builder(this)
                .SetTitle(Resource.String.unlicensed_dialog_title)
                .SetMessage(bRetry ? Resource.String.unlicensed_dialog_retry_body : Resource.String.unlicensed_dialog_body)
                .SetPositiveButton(bRetry ? Resource.String.retry_button : Resource.String.buy_button, eventHandler)
                .SetNegativeButton(Resource.String.quit_button, delegate { Finish(); })
                .Create();
        }

        private void doCheck()
        {
            mCheckLicenseButton.Enabled = false;
            SetProgressBarIndeterminateVisibility(true);
            mStatusText.SetText(Resource.String.checking_license);
            mChecker.CheckAccess(mLicenseCheckerCallback);
        }

        private void displayResult(string result)
        {
            mHandler.Post(new Runnable(delegate
                                           {
                                               mStatusText.Text = result;
                                               SetProgressBarIndeterminateVisibility(false);
                                               mCheckLicenseButton.Enabled = true;
                                           }
                              ));
        }

        private void displayDialog(bool showRetry)
        {
            mHandler.Post(new Runnable(delegate
                                           {
                                               SetProgressBarIndeterminateVisibility(false);
                                               ShowDialog(showRetry ? 1 : 0);
                                               mCheckLicenseButton.Enabled = true;
                                           }));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            mChecker.OnDestroy();
        }

        #region Nested type: MyLicenseCheckerCallback

        private class MyLicenseCheckerCallback : ILicenseCheckerCallback
        {
            private readonly MainActivity _mainActivity;

            public MyLicenseCheckerCallback(MainActivity mainActivity)
            {
                _mainActivity = mainActivity;
            }

            #region LicenseCheckerCallback Members

            public void Allow(PolicyServerResponse policyReason)
            {
                if (_mainActivity.IsFinishing)
                {
                    // Don't update UI if Activity is finishing.
                    return;
                }
                // Should allow user access.
                _mainActivity.displayResult(_mainActivity.GetString(Resource.String.allow));
            }

            public void DontAllow(PolicyServerResponse policyReason)
            {
                if (_mainActivity.IsFinishing)
                {
                    // Don't update UI if Activity is finishing.
                    return;
                }
                _mainActivity.displayResult(_mainActivity.GetString(Resource.String.dont_allow));
                // Should not allow access. In most cases, the app should assume
                // the user has access unless it encounters this. If it does,
                // the app should inform the user of their unlicensed ways
                // and then either shut down the app or limit the user to a
                // restricted set of features.
                // In this example, we show a dialog that takes the user to Market.
                // If the reason for the lack of license is that the service is
                // unavailable or there is another problem, we display a
                // retry button on the dialog and a different message.
                _mainActivity.displayDialog(policyReason == PolicyServerResponse.Retry);
            }

            public void ApplicationError(CallbackErrorCode errorCode)
            {
                if (_mainActivity.IsFinishing)
                {
                    // Don't update UI if Activity is finishing.
                    return;
                }
                // This is a polite way of saying the developer made a mistake
                // while setting up or calling the license checker library.
                // Please examine the error code and fix the erroResource.
                string result = string.Format(_mainActivity.GetString(Resource.String.application_error), errorCode);
                _mainActivity.displayResult(result);
            }

            #endregion
        }

        #endregion
    }
}