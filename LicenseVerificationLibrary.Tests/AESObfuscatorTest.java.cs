using System.Linq;
using Android.Content;
using Android.Util;
using Java.Lang;
using LicenseVerificationLibrary;

public class AESObfuscatorTest : TestCase
{
    private static string TAG = "AESObfuscatorTest";

    private static readonly byte[] SALT = new byte[]
                                              {
                                                  104, 12, 112, 82, 85, 10, 11, 61, 15, 54, 44, 66, 117, 89, 64, 110, 53, 123, 33
                                              };

    private static string PACKAGE = "package";
    private static string DEVICE = "device";

    private Obfuscator mObfuscator;

    public AESObfuscatorTest(Context context)
        : base(context)
    {
    }

    public override void SetUp()
    {
        mObfuscator = new AESObfuscator(SALT, PACKAGE, DEVICE);
    }

    public override void RunTests()
    {
        testObfuscateUnobfuscate();
        testObfuscate_same();
        testUnobfuscate_avoidBadPaddingException();
        testUnobfuscate_differentDevice();
        testUnobfuscate_differentKey();
        testUnobfuscate_differentPackage();
        testUnobfuscate_differentSalt();
        testUnobfuscate_invalid();
    }

    public void testObfuscateUnobfuscate()
    {
        testInvertible(null);
        testInvertible("");
        testInvertible("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*-=/\\|~`,.;:()[]{}<>\u00F6");
    }

    public void testUnobfuscate_invalid()
    {
        try
        {
            mObfuscator.unobfuscate("invalid", "testKey");
            Fail("Should have thrown ValidationException");
        }
        catch (ValidationException)
        {
        }
    }

    public void testUnobfuscate_differentSalt()
    {
        string obfuscated = mObfuscator.obfuscate("test", "testKey");
        Obfuscator differentSalt = new AESObfuscator(new byte[] {1}, PACKAGE, DEVICE);
        try
        {
            differentSalt.unobfuscate(obfuscated, "testKey");
            Fail("Should have thrown ValidationException");
        }
        catch (ValidationException)
        {
        }
    }

    public void testUnobfuscate_avoidBadPaddingException()
    {
        // Length should be equal to the cipher block size, to make sure that all padding lengths
        // are accounted for.
        for (int length = 0; length < 255; length++)
        {
            char[] data = Enumerable.Repeat('0', length).ToArray();
            string input = String.ValueOf(data);
            Log.Debug(TAG, "Input: (" + length + ")" + input);
            string obfuscated = mObfuscator.obfuscate(input, "testKey");
            Obfuscator differentSalt = new AESObfuscator(new byte[] {1}, PACKAGE, DEVICE);
            try
            {
                differentSalt.unobfuscate(obfuscated, "testKey");
                Fail("Should have thrown ValidationException");
            }
            catch (ValidationException)
            {
            }
        }
    }

    public void testUnobfuscate_differentDevice()
    {
        string obfuscated = mObfuscator.obfuscate("test", "testKey");
        Obfuscator differentDevice = new AESObfuscator(SALT, PACKAGE, "device2");
        try
        {
            differentDevice.unobfuscate(obfuscated, "testKey");
            Fail("Should have thrown ValidationException");
        }
        catch (ValidationException)
        {
        }
    }

    public void testUnobfuscate_differentPackage()
    {
        string obfuscated = mObfuscator.obfuscate("test", "testKey");
        Obfuscator differentPackage = new AESObfuscator(SALT, "package2", DEVICE);
        try
        {
            differentPackage.unobfuscate(obfuscated, "testKey");
            Fail("Should have thrown ValidationException");
        }
        catch (ValidationException)
        {
        }
    }

    public void testUnobfuscate_differentKey()
    {
        string obfuscated = mObfuscator.obfuscate("test", "testKey");
        Obfuscator differentPackage = new AESObfuscator(SALT, "package2", DEVICE);
        try
        {
            differentPackage.unobfuscate(obfuscated, "notMyKey");
            Fail("Should have thrown ValidationException");
        }
        catch (ValidationException)
        {
        }
    }

    public void testObfuscate_same()
    {
        string obfuscated = mObfuscator.obfuscate("test", "testKey");
        AssertEquals(obfuscated, mObfuscator.obfuscate("test", "testKey"));

        Obfuscator same = new AESObfuscator(SALT, PACKAGE, DEVICE);
        AssertEquals(obfuscated, same.obfuscate("test", "testKey"));
        AssertEquals("test", same.unobfuscate(obfuscated, "testKey"));
    }

    private void testInvertible(string original)
    {
        AssertEquals(original, mObfuscator.unobfuscate(mObfuscator.obfuscate(original, original + "Key"), original + "Key"));
    }
}