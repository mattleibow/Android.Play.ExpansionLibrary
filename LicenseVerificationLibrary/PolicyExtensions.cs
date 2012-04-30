using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace LicenseVerificationLibrary
{
    public static class PolicyExtensions
    {
        private const char ParameterSeparator = '&';
        private const char NameValueSeparator = '=';
        public static long MillisPerMinute = TimeSpan.FromMinutes(1).Milliseconds;

        private static readonly DateTime Jan1St970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public static bool TryDecodeExtras(string rawData, out Dictionary<string, string> extras)
        {
            bool result = false;

            try
            {
                extras = DecodeExtras(rawData);
                result = true;
            }
            catch
            {
                extras = new Dictionary<string, string>();
            }

            return result;
        }

        public static Dictionary<string, string> DecodeExtras(string extras)
        {
            var results = new Dictionary<string, string>();

            IEnumerable<KeyValuePair<string, string>> parameters = GetParameters(extras);
            foreach (KeyValuePair<string, string> item in parameters)
            {
                int count = results.Keys.Count(x => x == item.Key);
                string name = item.Key + (count != 0 ? count.ToString() : string.Empty);
                results.Add(name, item.Value);
            }

            return results;
        }

        public static long GetCurrentMilliseconds()
        {
            return (long) (DateTime.UtcNow - Jan1St970).TotalMilliseconds;
        }

        private static IEnumerable<KeyValuePair<string, string>> GetParameters(string uri)
        {
            return GetParameters(uri, Encoding.UTF8);
        }

        private static IEnumerable<KeyValuePair<string, string>> GetParameters(string uri, Encoding encoding)
        {
            var result = new List<KeyValuePair<string, string>>();

            IEnumerable<string[]> parameters = uri.Split(ParameterSeparator).Select(p => p.Split(NameValueSeparator));
            foreach (string[] nameValue in parameters)
            {
                if (nameValue.Length < 1 || nameValue.Length > 2)
                {
                    throw new ArgumentException("uri");
                }
                string name = HttpUtility.UrlDecode(nameValue[0], encoding);
                string value = nameValue.Length == 2
                                   ? HttpUtility.UrlDecode(nameValue[1], encoding)
                                   : string.Empty;

                result.Add(new KeyValuePair<string, string>(name, value));
            }

            return result;
        }
    }
}