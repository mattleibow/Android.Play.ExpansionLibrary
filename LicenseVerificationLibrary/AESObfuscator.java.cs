using Android.Util;
using Java.IO;
using Java.Lang;
using Java.Security;
using Java.Security.Spec;
using Javax.Crypto;
using Javax.Crypto.Spec;

namespace LicenseVerificationLibrary
{
    /// <summary>
    ///   An Obfuscator that uses AES to encrypt data.
    ///   todo: dodgy translation
    /// </summary>
    public class AESObfuscator : Obfuscator
    {
        private static string UTF8 = "UTF-8";
        private static string KEYGEN_ALGORITHM = "PBEWITHSHAAND256BITAES-CBC-BC";
        private static string CIPHER_ALGORITHM = "AES/CBC/PKCS5Padding";
        private static readonly byte[] IV = new byte[] {16, 74, 71, 80, 32, 101, 47, 72, 117, 14, 0, 29, 70, 65, 12, 74};
        private static string header = "com.android.vending.licensing.AESObfuscator-1|";

        private readonly Cipher mDecryptor;
        private readonly Cipher mEncryptor;

        /**
     * @param salt
     *            an array of random bytes to use for each (un)obfuscation
     * @param applicationId
     *            application identifier, e.g. the package name
     * @param deviceId
     *            device identifier. Use as many sources as possible to create
     *            this unique identifier.
     */

        public AESObfuscator(byte[] salt, string applicationId, string deviceId)
        {
            try
            {
                SecretKeyFactory factory = SecretKeyFactory.GetInstance(KEYGEN_ALGORITHM);
                IKeySpec keySpec = new PBEKeySpec((applicationId + deviceId).ToCharArray(), salt, 1024, 256);
                ISecretKey tmp = factory.GenerateSecret(keySpec);
                ISecretKey secret = new SecretKeySpec(tmp.GetEncoded(), "AES");
                mEncryptor = Cipher.GetInstance(CIPHER_ALGORITHM);
                mEncryptor.Init(Cipher.EncryptMode, secret, new IvParameterSpec(IV));
                mDecryptor = Cipher.GetInstance(CIPHER_ALGORITHM);
                mDecryptor.Init(Cipher.DecryptMode, secret, new IvParameterSpec(IV));
            }
            catch (GeneralSecurityException e)
            {
                // This can't happen on a compatible Android device.
                throw new RuntimeException("Invalid environment", e);
            }
        }

        #region Obfuscator Members

        public string obfuscate(string original, string key)
        {
            if (original == null)
            {
                return null;
            }
            try
            {
                // Header is appended as an integrity check
                return Base64.EncodeToString(mEncryptor.DoFinal(new String(header + key + original).GetBytes(UTF8)), Base64.Default);
            }
            catch (UnsupportedEncodingException e)
            {
                throw new RuntimeException("Invalid environment", e);
            }
            catch (GeneralSecurityException e)
            {
                throw new RuntimeException("Invalid environment", e);
            }
        }

        public string unobfuscate(string obfuscated, string key)
        {
            if (obfuscated == null)
            {
                return null;
            }
            try
            {
                var result = new String(mDecryptor.DoFinal(Base64.Decode(obfuscated, Base64.Default)), UTF8);
                // Check for presence of header. This serves as a  integrity
                // check, for cases
                // where the block size is correct during decryption.
                int headerIndex = result.IndexOf(header + key);
                if (headerIndex != 0)
                {
                    throw new ValidationException("Header not found (invalid data or key)" + ":" + obfuscated);
                }
                return result.Substring(header.Length + key.Length, result.Length());
            }
            catch (IllegalArgumentException e)
            {
                throw new ValidationException(e.Message + ":" + obfuscated);
            }
            catch (IllegalBlockSizeException e)
            {
                throw new ValidationException(e.Message + ":" + obfuscated);
            }
            catch (BadPaddingException e)
            {
                throw new ValidationException(e.Message + ":" + obfuscated);
            }
            catch (UnsupportedEncodingException e)
            {
                throw new RuntimeException("Invalid environment", e);
            }
        }

        #endregion
    }
}