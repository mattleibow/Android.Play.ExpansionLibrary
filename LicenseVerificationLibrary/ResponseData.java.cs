using Android.Text;
using Java.Lang;
using Java.Util.Regex;
using Enum = System.Enum;

namespace LicenseVerificationLibrary
{
    /**
     * ResponseData from licensing server.
     */

    public class ResponseData
    {
        public string extra;
        public int nonce;
        public string packageName;
        public ServerResponseCode responseCode;
        public long timestamp;
        public string userId;
        public string versionCode;
        /** Response-specific data. */

        /**
     * Parses response string into ResponseData.
     * 
     * @param responseData
     *            response data string
     * @throws IllegalArgumentException
     *             upon parsing error
     * @return ResponseData object
     */

        public static ResponseData parse(string responseData)
        {
            // Must parse out main response data and response-specific data.
            int index = responseData.IndexOf(':');
            string mainData, extraData;
            if (-1 == index)
            {
                mainData = responseData;
                extraData = "";
            }
            else
            {
                mainData = responseData.Substring(0, index);
                extraData = index >= responseData.Length ? "" : responseData.Substring(index + 1);
            }

            string[] fields = TextUtils.Split(mainData, Pattern.Quote("|"));
            if (fields.Length < 6)
            {
                throw new IllegalArgumentException("Wrong number of fields.");
            }

            var data = new ResponseData();
            data.extra = extraData;
            data.responseCode = (ServerResponseCode) Enum.Parse(typeof (ServerResponseCode), fields[0]);
            data.nonce = int.Parse(fields[1]);
            data.packageName = fields[2];
            data.versionCode = fields[3];
            // Application-specific user identifier.
            data.userId = fields[4];
            data.timestamp = long.Parse(fields[5]);

            return data;
        }

        public override string ToString()
        {
            return string.Join("|", new object[] {responseCode, nonce, packageName, versionCode, userId, timestamp});
        }
    }
}