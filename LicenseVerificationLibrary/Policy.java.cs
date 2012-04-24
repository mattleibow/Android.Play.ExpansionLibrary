using System;
using System.Collections.Generic;
using Java.Net;
using Java.Util;

namespace LicenseVerificationLibrary
{
    public interface IPolicy
    {
        /// <summary>
        /// Provide results from contact with the license server. Retry counts are
        /// incremented if the current value of response is RETRY. Results will be
        /// used for any future policy decisions.
        /// </summary>
        /// <param name="response">The result from validating the server response</param>
        /// <param name="rawData">The raw server response data, can be null for RETRY</param>
        void ProcessServerResponse(PolicyServerResponse response, ResponseData rawData);

        /// <summary>
        /// Check if the user should be allowed access to the application.
        /// </summary>
        bool AllowAccess();
    }

    public static class PolicyExtensions
    {
        private const string ParameterSeparator = "&";
        private const char NameValueSeparator = '=';
        public static long MillisPerMinute = TimeSpan.FromMinutes(1).Milliseconds;

        private static readonly DateTime Jan1St970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public static Dictionary<string, string> DecodeExtras(string extras)
        {
            var results = new Dictionary<string, string>();

            IEnumerable<KeyValuePair<string, string>> extraList = Parse(new URI("?" + extras), "UTF-8");
            foreach (KeyValuePair<string, string> item in extraList)
            {
                string name = item.Key;
                int i = 0;
                while (results.ContainsKey(name))
                {
                    name = item.Key + ++i;
                }
                results.Add(name, item.Value);
            }

            return results;
        }

        public static long GetCurrentMilliseconds()
        {
            return (long) (DateTime.UtcNow - Jan1St970).TotalMilliseconds;
        }

        private static IEnumerable<KeyValuePair<string, string>> Parse(URI uri, string encoding)
        {
            var result = new List<KeyValuePair<string, string>>();

            string query = uri.RawQuery;
            if (!String.IsNullOrEmpty(query))
            {
                var scanner = new Scanner(query);
                scanner.UseDelimiter(ParameterSeparator);
                while (scanner.HasNext)
                {
                    string[] nameValue = scanner.Next().Split(NameValueSeparator);
                    if (nameValue.Length < 1 || nameValue.Length > 2)
                    {
                        throw new ArgumentException("uri");
                    }
                    string name = URLDecoder.Decode(nameValue[0], encoding);
                    string value = String.Empty;
                    if (nameValue.Length == 2)
                    {
                        value = URLDecoder.Decode(nameValue[1], encoding);
                    }
                    result.Add(new KeyValuePair<string, string>(name, value));
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Change these values to make it more difficult for tools to automatically
    /// strip LVL protection from your APK.
    /// </summary>
    public enum PolicyServerResponse
    {
        /// <summary>
        /// The server returned back a valid license response
        /// </summary>
        Licensed = 0x0100,

        /// <summary>
        /// The server returned back a valid license response that indicated 
        /// that the user definitively is not licensed
        /// </summary>
        NotLicensed = 0x0231,

        /// <summary>
        /// The license response was unable to be determined 
        ///  - perhaps as a result of faulty networking
        /// </summary>
        Retry = 0x0123
    }
}