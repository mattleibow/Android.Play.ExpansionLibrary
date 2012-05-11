namespace LicenseVerificationLibrary.Tests
{
    using System.Linq;
    
    using Android.Content;

    public class AesObfuscatorTest : TestCase
    {
        private static readonly byte[] Salt = new byte[]
            { 104, 12, 112, 82, 85, 10, 11, 61, 15, 54, 44, 66, 117, 89, 64, 110, 53, 123, 33 };

        private const string Package = "package";
        private const string Device = "device";

        private IObfuscator obfuscator;

        public AesObfuscatorTest(Context context)
            : base(context)
        {
        }

        public override void SetUp()
        {
            this.obfuscator = new AesObfuscator(Salt, Package, Device);
        }

        public void TestObfuscateUnobfuscate()
        {
            this.TestInvertible(null);
            this.TestInvertible(string.Empty);
            this.TestInvertible("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*-=/\\|~`,.;:()[]{}<>\u00F6");
        }

        public void TestUnobfuscateInvalid()
        {
            try
            {
                this.obfuscator.Unobfuscate("invalid", "testKey");
                Fail("Should have thrown ValidationException");
            }
            catch (ValidationException)
            {
            }
        }

        public void TestUnobfuscateDifferentSalt()
        {
            string obfuscated = this.obfuscator.Obfuscate("test", "testKey");
            IObfuscator differentSalt = new AesObfuscator(new byte[] { 1 }, Package, Device);
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
                string input = Java.Lang.String.ValueOf(data);
                string obfuscated = this.obfuscator.Obfuscate(input, "testKey");
                IObfuscator differentSalt = new AesObfuscator(new byte[] { 1 }, Package, Device);
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
            string obfuscated = this.obfuscator.Obfuscate("test", "testKey");
            IObfuscator differentDevice = new AesObfuscator(Salt, Package, "device2");
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
            string obfuscated = this.obfuscator.Obfuscate("test", "testKey");
            IObfuscator differentPackage = new AesObfuscator(Salt, "package2", Device);
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
            string obfuscated = this.obfuscator.Obfuscate("test", "testKey");
            IObfuscator differentPackage = new AesObfuscator(Salt, "package2", Device);
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
            string obfuscated = this.obfuscator.Obfuscate("test", "testKey");
            AssertEquals(obfuscated, this.obfuscator.Obfuscate("test", "testKey"));

            IObfuscator same = new AesObfuscator(Salt, Package, Device);
            AssertEquals(obfuscated, same.Obfuscate("test", "testKey"));
            AssertEquals("test", same.Unobfuscate(obfuscated, "testKey"));
        }

        private void TestInvertible(string original)
        {
            AssertEquals(original, this.obfuscator.Unobfuscate(this.obfuscator.Obfuscate(original, original + "Key"), original + "Key"));
        }
    }
}