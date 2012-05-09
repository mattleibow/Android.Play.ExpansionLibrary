namespace LicenseVerificationLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Web;

    /// <summary>
    /// The policy extensions.
    /// </summary>
    public static class PolicyExtensions
    {
        #region Constants and Fields

        /// <summary>
        /// The millis per minute.
        /// </summary>
        public static readonly long MillisPerMinute = TimeSpan.FromMinutes(1).Milliseconds;

        /// <summary>
        /// The name value separator.
        /// </summary>
        private const char NameValueSeparator = '=';

        /// <summary>
        /// The parameter separator.
        /// </summary>
        private const char ParameterSeparator = '&';

        /// <summary>
        /// The jan 1 st 970.
        /// </summary>
        private static readonly DateTime Jan1St970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The decode extras.
        /// </summary>
        /// <param name="extras">
        /// The extras.
        /// </param>
        /// <returns>
        /// </returns>
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

        /// <summary>
        /// The get current milliseconds.
        /// </summary>
        /// <returns>
        /// The get current milliseconds.
        /// </returns>
        public static long GetCurrentMilliseconds()
        {
            return (long)(DateTime.UtcNow - Jan1St970).TotalMilliseconds;
        }

        /// <summary>
        /// The try decode extras.
        /// </summary>
        /// <param name="rawData">
        /// The raw data.
        /// </param>
        /// <param name="extras">
        /// The extras.
        /// </param>
        /// <returns>
        /// The try decode extras.
        /// </returns>
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

        #endregion

        #region Methods

        /// <summary>
        /// The get parameters.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <returns>
        /// </returns>
        private static IEnumerable<KeyValuePair<string, string>> GetParameters(string uri)
        {
            return GetParameters(uri, Encoding.UTF8);
        }

        /// <summary>
        /// The get parameters.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="encoding">
        /// The encoding.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="ArgumentException">
        /// </exception>
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
                string value = nameValue.Length == 2 ? HttpUtility.UrlDecode(nameValue[1], encoding) : string.Empty;

                result.Add(new KeyValuePair<string, string>(name, value));
            }

            return result;
        }

        #endregion
    }
}