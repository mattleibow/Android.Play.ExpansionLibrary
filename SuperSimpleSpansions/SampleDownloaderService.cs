namespace SuperSimpleSpansions
{
    using Android.App;

    using ExpansionDownloader.Service;

    [Service]
    public class SampleDownloaderService : DownloaderService
    {
        protected override string PublicKey
        {
            get
            {
                return "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqSEPO6frjPZ/qdSTT80dCBjsHZouZGadBRwlg9g34ueC6j4F348"
                       + "dy0Xgo4NdKX39pSX1RNl0kGaxX6sg04bp4qx6RfwVyD1CPSEYdWldkuAQ9aNaQZ/yq6V+lmrqaKfJJuh1olqtsK8VVnvJ"
                       + "48Q+VwkIaT5CXhqeRAyZRXMEmEGPTNybSYVf5P90CxdSRwpae/w3S9rzuXOnfUhLKc9WmovRLQ8GzXYzhbNBzbWrK0NE+"
                       + "iXdxDGOZPDQPiLEaU2KliaWOBGO+2Cx5MSXZ3Xlm7e0Yo3F4x8BpMDQHs+3RSYTEaMvQk/t4sfMbA4xCzAP57cl6Ae6S"
                       + "bWU46mk+lqDeQIDAQAB";
            }
        }

        protected override byte[] Salt
        {
            get
            {
                return new byte[] { 1, 43, 12, 1, 54, 98, 100, 12, 43, 2, 8, 4, 9, 5, 106, 108, 33, 45, 1, 84 };
            }
        }

        protected override string AlarmReceiverClassName
        {
            get
            {
                return "expansiondownloader.sample.SampleAlarmReceiver";
            }
        }
    }
}
