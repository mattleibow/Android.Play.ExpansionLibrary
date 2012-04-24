using Android.Content;
using Android.Provider;
using LicenseVerificationLibrary;

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
        Obfuscator o = new AESObfuscator(SALT, Context.PackageName, deviceId);
        op = new PreferenceObfuscator(sp, o);

        // Populate with test data
        op.putString("testString", "Hello world");
        op.commit();
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
        AssertEquals("Hello world", op.getString("testString", "fail"));
    }

    public void testGetDefaultString()
    {
        AssertEquals("Android rocks", op.getString("noExist", "Android rocks"));
    }

    public void testGetDefaultNullString()
    {
        AssertEquals(null, op.getString("noExist", null));
    }

    public void testCorruptDataRetunsDefaultString()
    {
        // Insert non-obfuscated string
        ISharedPreferencesEditor spe = sp.Edit();
        spe.PutString("corruptData", "foo");
        spe.Commit();

        // Read back contents
        AssertEquals("Android rocks", op.getString("corruptdata", "Android rocks"));
    }
}