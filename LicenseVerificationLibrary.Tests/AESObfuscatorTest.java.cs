using System.Linq;
using Android.Content;
using Java.Lang;

namespace LicenseVerificationLibrary.Tests
{
    public class AesObfuscatorTest : TestCase
    {
        private static readonly byte[] SALT = new byte[] {104, 12, 112, 82, 85, 10, 11, 61, 15, 54, 44, 66, 117, 89, 64, 110, 53, 123, 33};

        private const string Package = "package";
        private const string Device = "device";

        private IObfuscator _obfuscator;

        public AesObfuscatorTest(Context context)
            : base(context)
        {
        }

        public override void SetUp()
        {
            _obfuscator = new AesObfuscator(SALT, Package, Device);
        }

        public override void RunTests()
        {
            TestObfuscateUnobfuscate();
            TestObfuscateSame();
            TestUnobfuscateAvoidBadPaddingException();
            TestUnobfuscateDifferentDevice();
            TestUnobfuscateDifferentKey();
            TestUnobfuscateDifferentPackage();
            TestUnobfuscateDifferentSalt();
            TestUnobfuscateInvalid();
        }

        public void TestObfuscateUnobfuscate()
        {
            TestInvertible(null);
            TestInvertible("");
            TestInvertible("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*-=/\\|~`,.;:()[]{}<>\u00F6");
        }

        public void TestUnobfuscateInvalid()
        {
            try
            {
                _obfuscator.Unobfuscate("invalid", "testKey");
                Fail("Should have thrown ValidationException");
            }
            catch (ValidationException)
            {
            }
        }

        public void TestUnobfuscateDifferentSalt()
        {
            string obfuscated = _obfuscator.Obfuscate("test", "testKey");
            IObfuscator differentSalt = new AesObfuscator(new byte[] {1}, Package, Device);
            try
            {
                differentSalt.Unobfuscate(obfuscated, "testKey");
                Fail("Should have thrown ValidationException");
            }
            catch (ValidationException)
            {
            }
        }

        public void TestUnobfuscateAvoidBadPaddingException()
        {
            // Length should be equal to the cipher block size, to make sure that all padding lengths
            // are accounted for.
            for (int length = 0; length < 255; length++)
            {
                char[] data = Enumerable.Repeat('0', length).ToArray();
                string input = String.ValueOf(data);
                string obfuscated = _obfuscator.Obfuscate(input, "testKey");
                IObfuscator differentSalt = new AesObfuscator(new byte[] {1}, Package, Device);
                try
                {
                    differentSalt.Unobfuscate(obfuscated, "testKey");
                    Fail("Should have thrown ValidationException");
                }
                catch (ValidationException)
                {
                }
            }
        }

        public void TestUnobfuscateDifferentDevice()
        {
            string obfuscated = _obfuscator.Obfuscate("test", "testKey");
            IObfuscator differentDevice = new AesObfuscator(SALT, Package, "device2");
            try
            {
                differentDevice.Unobfuscate(obfuscated, "testKey");
                Fail("Should have thrown ValidationException");
            }
            catch (ValidationException)
            {
            }
        }

        public void TestUnobfuscateDifferentPackage()
        {
            string obfuscated = _obfuscator.Obfuscate("test", "testKey");
            IObfuscator differentPackage = new AesObfuscator(SALT, "package2", Device);
            try
            {
                differentPackage.Unobfuscate(obfuscated, "testKey");
                Fail("Should have thrown ValidationException");
            }
            catch (ValidationException)
            {
            }
        }

        public void TestUnobfuscateDifferentKey()
        {
            string obfuscated = _obfuscator.Obfuscate("test", "testKey");
            IObfuscator differentPackage = new AesObfuscator(SALT, "package2", Device);
            try
            {
                differentPackage.Unobfuscate(obfuscated, "notMyKey");
                Fail("Should have thrown ValidationException");
            }
            catch (ValidationException)
            {
            }
        }

        public void TestObfuscateSame()
        {
            string obfuscated = _obfuscator.Obfuscate("test", "testKey");
            AssertEquals(obfuscated, _obfuscator.Obfuscate("test", "testKey"));

            IObfuscator same = new AesObfuscator(SALT, Package, Device);
            AssertEquals(obfuscated, same.Obfuscate("test", "testKey"));
            AssertEquals("test", same.Unobfuscate(obfuscated, "testKey"));
        }

        private void TestInvertible(string original)
        {
            AssertEquals(original, _obfuscator.Unobfuscate(_obfuscator.Obfuscate(original, original + "Key"), original + "Key"));
        }
    }
}