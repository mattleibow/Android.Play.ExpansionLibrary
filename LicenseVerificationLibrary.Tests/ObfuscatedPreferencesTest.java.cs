using Android.Content;
using Android.Provider;

namespace LicenseVerificationLibrary.Tests
{
    public class ObfuscatedPreferencesTest : TestCase
    {
        private static string filename = "com.android.vending.licnese.test.ObfuscatedPreferencePopulatedTest";
        private PreferenceObfuscator op;
        private ISharedPreferences sp;

        public ObfuscatedPreferencesTest(Context context) : base(context)
        {
        }

        public override void SetUp()
        {
            var SALT = new byte[] {104, 12, 112, 82, 85, 10, 11, 61, 15, 54, 44, 66, 117, 89, 64, 110, 53, 123, 33};

            // Prepare PreferenceObfuscator instance
            sp = Context.GetSharedPreferences(filename, FileCreationMode.Private);
            string deviceId = Settings.Secure.GetString(Context.ContentResolver, Settings.Secure.AndroidId);
            IObfuscator o = new AesObfuscator(SALT, Context.PackageName, deviceId);
            op = new PreferenceObfuscator(sp, o);

            // Populate with test data
            op.PutString("testString", "Hello world");
            op.Commit();
        }

        public override void CleanUp()
        {
            // Manually clear out any saved preferences
            ISharedPreferencesEditor spe = sp.Edit();
            spe.Clear();
            spe.Commit();
        }

        public override void RunTests()
        {
            testCorruptDataRetunsDefaultString();
            testGetDefaultNullString();
            testGetDefaultString();
            testGetString();
        }

        public void testGetString()
        {
            AssertEquals("Hello world", op.GetString("testString", "fail"));
        }

        public void testGetDefaultString()
        {
            AssertEquals("Android rocks", op.GetString("noExist", "Android rocks"));
        }

        public void testGetDefaultNullString()
        {
            AssertEquals(null, op.GetString("noExist", null));
        }

        public void testCorruptDataRetunsDefaultString()
        {
            // Insert non-obfuscated string
            ISharedPreferencesEditor spe = sp.Edit();
            spe.PutString("corruptData", "foo");
            spe.Commit();

            // Read back contents
            AssertEquals("Android rocks", op.GetString("corruptdata", "Android rocks"));
        }
    }
}