using ExpansionDownloader.impl;
using Android.App;

namespace ExpansionDownloader.Sample
{
    [Service]
    public class SampleDownloaderService : DownloaderService
    {
        // stuff for LVL --- MODIFY FOR YOUR APPLICATION!
        private static string BASE64_PUBLIC_KEY =
            "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqSEPO6frjPZ/qdSTT80dCBjsHZouZGadBRwlg9g34ueC6j4F348dy0Xgo4NdKX39pSX1RNl0kGaxX6sg04bp4qx6RfwVyD1CPSEYdWldkuAQ9aNaQZ/yq6V+lmrqaKfJJuh1olqtsK8VVnvJ48Q+VwkIaT5CXhqeRAyZRXMEmEGPTNybSYVf5P90CxdSRwpae/w3S9rzuXOnfUhLKc9WmovRLQ8GzXYzhbNBzbWrK0NE+iXdxDGOZPDQPiLEaU2KliaWOBGO+2Cx5MSXZ3Xlm7e0Yo3F4x8BpMDQHs+3RSYTEaMvQk/t4sfMbA4xCzAP57cl6Ae6SbWU46mk+lqDeQIDAQAB";

        // used by the preference obfuscater
        private static readonly byte[] SALT = new byte[]
                                                  {
                                                      1, 43, 12, 1, 54, 98, 100, 12, 43, 2, 8, 4, 9, 5, 106, 108, 33, 45, 1, 84
                                                  };

        /**
     * This public key comes from your Android Market publisher account, and it
     * used by the LVL to validate responses from Market on your behalf.
     */

        public override string getPublicKey()
        {
            return BASE64_PUBLIC_KEY;
        }

        /**
     * This is used by the preference obfuscater to make sure that your
     * obfuscated preferences are different than the ones used by other
     * applications.
     */

        public override byte[] getSALT()
        {
            return SALT;
        }

        /**
     * Fill this in with the class name for your alarm receiver. We do this
     * because receivers must be unique across all of Android (it's a good idea
     * to make sure that your receiver is in your unique package)
     */

        public override string getAlarmReceiverClassName()
        {
            return typeof (SampleAlarmReceiver).Name;
        }
    }
}