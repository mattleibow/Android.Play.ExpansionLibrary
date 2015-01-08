namespace LicenseVerificationLibrary.Sample
{
    using System;

    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Provider;
    using Android.Views;
    using Android.Widget;

    using LicenseVerificationLibrary.Obfuscator;
    using LicenseVerificationLibrary.Policy;

    using Uri = Android.Net.Uri;

    /// <summary>
    /// <para>
    /// The first thing you need to do is get your hands on your public key.
    /// Update the Base64PublicKey constant below with your encoded public 
    /// key, which you can find on the
    /// <a href="http://market.android.com/publish/editProfile">Edit 
    /// Profile</a> page of the Market publisher site.
    /// </para>
    /// <para>
    /// Log in with the same account on your Cupcake (1.5) or higher phone or
    /// your Froyo (2.2) emulator with the Google add-ons installed.
    /// Change the test response on the Edit Profile page, press Save, and see 
    /// how this application responds when you check your license.
    /// </para>
    /// <para>
    /// After you get this sample running, peruse the
    /// <a href="http://developer.android.com/guide/publishing/licensing.html">
    /// licensing documentation.</a>
    /// </para>
    /// </summary>
    [Activity(Label = "LicenseVerificationLibrary.Sample", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, ILicenseCheckerCallback
    {
        /// <summary>
        /// Your Base 64 public key
        /// </summary>
        private const string Base64PublicKey =
			"MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA96TCUr/Rhx/fcIVcCrWTz0FKvI+hZ" + 
			"ICb/yXaxNPhSWeo7TROB+Op5wKhdmjsaSvbi/v75RgyikS/HrSKvQCqwix6b3IgjIu8iGYYZz" + 
			"2ieoFMVt39WFP20fSfjNoBr0KJOsoIAso6zF845ZtIE+3vJFg4z/tTe/jPgi73AYJS6RnUO2p" + 
			"C2tzeGVe+TQemhPUfFWAczunpAoT8ioBCYzK1FzTc1uyAFMh8riijrKDXbQd42nByJq3SSjJi" + 
			"yx/5pcMMj2kWvuJjD5ugk0X10jEfwptVQytXOAvMPhbyvJ2yNN6Ha9ZUHIawXC+JyCr9bvMAo" +
			"KIFTqzqLYfpX10feYTDsQIDAQAB";

        // Generate your own 20 random bytes, and put them here.
        private static readonly byte[] Salt = new byte[]
            { 46, 65, 30, 128, 103, 57, 74, 64, 51, 88, 95, 45, 77, 117, 36, 113, 11, 32, 64, 89 };

        private Button checkLicenseButton;
        private Button resetLicenseButton;
        private LicenseChecker checker;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.IndeterminateProgress);
            SetContentView(Resource.Layout.Main);

            // Try to use more data here. ANDROID_ID is a single point of attack.
            string deviceId = Settings.Secure.GetString(ContentResolver, Settings.Secure.AndroidId);

            // Construct the LicenseChecker with a policy.
            var obfuscator = new AesObfuscator(Salt, this.PackageName, deviceId);
            var policy = new ServerManagedPolicy(this, obfuscator);
            this.checker = new LicenseChecker(this, policy, Base64PublicKey);

			this.checkLicenseButton = FindViewById<Button>(Resource.Id.checkButton);
			this.checkLicenseButton.Click += delegate { this.DoCheck(); };

			this.resetLicenseButton = FindViewById<Button>(Resource.Id.resetButton);
			this.resetLicenseButton.Click += delegate { policy.ResetPolicy(); };

            this.DoCheck();
        }

        protected override Dialog OnCreateDialog(int id)
        {
            bool retry = id == 1;
            
            EventHandler<DialogClickEventArgs> eventHandler = delegate
                {
                    if (retry)
                    {
                        this.DoCheck();
                    }
                    else
                    {
                        Uri uri = Uri.Parse("http://market.android.com/details?id=" + this.PackageName);
                        var marketIntent = new Intent(Intent.ActionView, uri);
                        this.StartActivity(marketIntent);
                    }
                };

            var message = retry ? Resource.String.unlicensed_dialog_retry_body : Resource.String.unlicensed_dialog_body;
            var ok = retry ? Resource.String.retry_button : Resource.String.buy_button;

            return new AlertDialog.Builder(this) // builder
                .SetTitle(Resource.String.unlicensed_dialog_title) // title
                .SetMessage(message) // message
                .SetPositiveButton(ok, eventHandler) // ok
                .SetNegativeButton(Resource.String.quit_button, delegate { Finish(); }) // cancel
                .Create(); // create dialog now
        }

        private void DoCheck()
        {
            this.checkLicenseButton.Enabled = false;
			this.resetLicenseButton.Enabled = false;
            this.SetProgressBarIndeterminateVisibility(true);
            this.checkLicenseButton.SetText(Resource.String.checking_license);
            this.checker.CheckAccess(this);
        }

        private void DisplayResult(string result)
        {
            this.RunOnUiThread(
                delegate
                    {
                        this.checkLicenseButton.Text = result;
                        this.SetProgressBarIndeterminateVisibility(false);
                        this.checkLicenseButton.Enabled = true;
						this.resetLicenseButton.Enabled = true;
                    });
        }

        private void DisplayDialog(bool showRetry)
        {
            this.RunOnUiThread(
                delegate
                    {
                        SetProgressBarIndeterminateVisibility(false);
                        ShowDialog(showRetry ? 1 : 0);
                        this.checkLicenseButton.Enabled = true;
						this.resetLicenseButton.Enabled = true;
                    });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.checker.OnDestroy();
        }

        /// <summary>
        /// Should allow user access. 
        /// </summary>
        /// <param name="response"></param>
        public void Allow(PolicyServerResponse response)
        {
            if (this.IsFinishing)
            {
                // Don't update UI if Activity is finishing.
                return;
            }
            
            this.DisplayResult(this.GetString(Resource.String.allow));
        }

        /// <summary>
        /// Should not allow access.
        /// <br />
        /// In most cases, the app should assume the user has access unless it 
        /// encounters this. If it does, the app should inform the user of 
        /// their unlicensed ways and then either shut down the app or limit 
        /// the user to a restricted set of features.
        /// <br />
        /// In this example, we show a dialog that takes the user to Play.
        /// If the reason for the lack of license is that the service is
        /// unavailable or there is another problem, we display a
        /// retry button on the dialog and a different message. 
        /// </summary>
        /// <param name="response"></param>
        public void DontAllow(PolicyServerResponse response)
        {
            if (this.IsFinishing)
            {
                // Don't update UI if Activity is finishing.
                return;
            }

            this.DisplayResult(this.GetString(Resource.String.dont_allow));
            this.DisplayDialog(response == PolicyServerResponse.Retry);
        }

        /// <summary>
        /// This is a polite way of saying the developer made a mistake
        /// while setting up or calling the license checker library.
        /// Please examine the error code and fix the error.
        /// </summary>
        /// <param name="errorCode"></param>
        public void ApplicationError(CallbackErrorCode errorCode)
        {
            if (this.IsFinishing)
            {
                // Don't update UI if Activity is finishing.
                return;
            }
            
            var errorString = this.GetString(Resource.String.application_error);
            this.DisplayResult(string.Format(errorString, errorCode));
        }
    }
}