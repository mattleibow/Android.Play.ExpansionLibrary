using System.Text;
using Java.IO;
using Java.Lang;
using Java.Security;
using Java.Security.Spec;
using Javax.Crypto;
using Javax.Crypto.Spec;
using System;

namespace LicenseVerificationLibrary
{
    /// <summary>
    ///   An Obfuscator that uses AES to encrypt data.
    ///   todo: dodgy translation
    /// </summary>
    public class AesObfuscator : IObfuscator
    {
        private static readonly byte[] Iv = new byte[] { 16, 74, 71, 80, 32, 101, 47, 72, 117, 14, 0, 29, 70, 65, 12, 74 };
        private const string KeygenAlgorithm = "PBEWITHSHAAND256BITAES-CBC-BC";
        private const string CipherAlgorithm = "AES/CBC/PKCS5Padding";
        private const string Header = "com.android.vending.licensing.AESObfuscator-1|";

        private readonly Cipher _decryptor;
        private readonly Cipher _encryptor;

        /// <summary>
        /// </summary>
        /// <param name="salt">an array of random bytes to use for each (un)obfuscation</param>
        /// <param name="applicationId">application identifier, e.g. the package name</param>
        /// <param name="deviceId">
        /// device identifier. Use as many sources as possible to 
        /// create this unique identifier.
        /// </param>
        public AesObfuscator(byte[] salt, string applicationId, string deviceId)
        {
            try
            {
                SecretKeyFactory factory = SecretKeyFactory.GetInstance(KeygenAlgorithm);
                IKeySpec keySpec = new PBEKeySpec((applicationId + deviceId).ToCharArray(), salt, 1024, 256);
                ISecretKey tmp = factory.GenerateSecret(keySpec);
                ISecretKey secret = new SecretKeySpec(tmp.GetEncoded(), "AES");
                _encryptor = Cipher.GetInstance(CipherAlgorithm);
                _encryptor.Init(CipherMode.EncryptMode, secret, new IvParameterSpec(Iv));
                _decryptor = Cipher.GetInstance(CipherAlgorithm);
                _decryptor.Init(CipherMode.DecryptMode, secret, new IvParameterSpec(Iv));
            }
            catch (GeneralSecurityException e)
            {
                // This can't happen on a compatible Android device.
                throw new RuntimeException("Invalid environment", e);
            }
        }

        #region Obfuscator Members

        public string Obfuscate(string original, string key)
        {
            if (original == null)
            {
                return null;
            }

            try
            {
                // Header is appended as an integrity check
                return Convert.ToBase64String(_encryptor.DoFinal(Encoding.UTF8.GetBytes(Header + key + original)));
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

        public string Unobfuscate(string obfuscated, string key)
        {
            if (obfuscated == null)
            {
                return null;
            }

            try
            {
                var result = Encoding.UTF8.GetString(_decryptor.DoFinal(Convert.FromBase64String(obfuscated)));
                // Check for presence of header. This serves as an integrity check, 
                // for cases where the block size is correct during decryption.
                var headerAndKey = Header + key;
                if (!result.StartsWith(headerAndKey))
                {
                    throw new ValidationException("Header not found (invalid data or key)" + ":" + obfuscated);
                }
                return result.Substring(headerAndKey.Length);
            }
            catch (FormatException e)
            {
                throw new ValidationException(e.Message + ":" + obfuscated);
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