namespace LicenseVerificationLibrary.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Android.App;
    using Android.OS;
    using Android.Views;
    using Android.Widget;

    [Activity(Label = "LicenseVerificationLibrary.Tests", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(WindowFeatures.IndeterminateProgress);
            SetContentView(Resource.Layout.main);

            var runTests = FindViewById<Button>(Resource.Id.RunTests);
            runTests.Click += delegate { ThreadPool.QueueUserWorkItem(OnRunTestsOnClick); };
        }

        private void OnRunTestsOnClick(object state)
        {
            var statusText = FindViewById<TextView>(Resource.Id.status_text);

            var errors = string.Empty;

            var tests = new TestCase[] // the tests to run
                { // _
                    new ServerManagedPolicyTest(this), // cached license
                    new StrictPolicyTest(this), // no caching
                    new ApkExpansionPolicyTest(this), // apk server expansion result 
                    new ObfuscatedPreferencesTest(this), // obfuscated prefs
                    new AesObfuscatorTest(this) // obfuscator
                };

            foreach (var test in tests)
            {
                TestCase test1 = test;

                // log each test start
                RunOnUiThread(delegate { statusText.Text = test1.GetType().Name; });

                // log each test start
                test1.SetUp();

                var methods = test1.GetType().GetMethods();
                foreach (var method in methods.Where(x => x.Name.StartsWith("Test")))
                {
                    MethodInfo method1 = method;

                    try
                    {
                        RunOnUiThread(delegate { statusText.Text += "\nRunning: " + method1.Name; });

                        method1.Invoke(test1, null);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);

                        errors += "\n" + method1.Name + ex.Message;
                    }
                }

                // log each test start
                test1.CleanUp();
            }

            // log error
            RunOnUiThread(delegate { statusText.Text = errors; });

            RunOnUiThread(delegate { statusText.Text += "\n\nFinished!"; });
        }
    }
}