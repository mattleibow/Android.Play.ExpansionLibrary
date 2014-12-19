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
				return "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA96TCUr/Rhx/fcIVcCrWTz0FKvI+hZ" +
					   "ICb/yXaxNPhSWeo7TROB+Op5wKhdmjsaSvbi/v75RgyikS/HrSKvQCqwix6b3IgjIu8iGYYZz" +
					   "2ieoFMVt39WFP20fSfjNoBr0KJOsoIAso6zF845ZtIE+3vJFg4z/tTe/jPgi73AYJS6RnUO2p" +
					   "C2tzeGVe+TQemhPUfFWAczunpAoT8ioBCYzK1FzTc1uyAFMh8riijrKDXbQd42nByJq3SSjJi" +
					   "yx/5pcMMj2kWvuJjD5ugk0X10jEfwptVQytXOAvMPhbyvJ2yNN6Ha9ZUHIawXC+JyCr9bvMAo" + 
	                   "KIFTqzqLYfpX10feYTDsQIDAQAB";
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
