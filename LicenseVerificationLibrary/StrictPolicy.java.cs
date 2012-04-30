namespace LicenseVerificationLibrary
{
    /// <summary>
    /// Non-caching policy. All requests will be sent to the licensing service, and
    /// no local caching is performed.
    /// 
    /// Using a non-caching policy ensures that there is no local preference data for
    /// malicious users to tamper with. As a side effect, applications will not be
    /// permitted to run while offline. Developers should carefully weigh the risks
    /// of using this IPolicy over one which implements caching, such as
    /// ServerManagedPolicy.
    /// 
    /// Access to the application is only allowed if a LICESNED response is.
    /// received. All other responses (including RETRY) will deny access.
    ///  </summary>
    public class StrictPolicy : IPolicy
    {
        private PolicyServerResponse _lastResponse;

        public StrictPolicy()
        {
            // Set default policy. This will force the application to check the
            // policy on launch.
            _lastResponse = PolicyServerResponse.Retry;
        }

        #region IPolicy Members

        /// <summary>
        /// Process a new response from the license server. Since we aren't
        /// performing any caching, this equates to reading the LicenseResponse. Any
        /// ResponseData provided is ignored.
        /// </summary>
        /// <param name="response">the result from validating the server response</param>
        /// <param name="rawData">the raw server response data</param>
        public void ProcessServerResponse(PolicyServerResponse response, ResponseData rawData)
        {
            _lastResponse = response;
        }

        /// <summary>
        /// This implementation allows access if and only if a LICENSED response was
        /// received the last time the server was contacted.
        /// </summary>
        /// <returns></returns>
        public bool AllowAccess()
        {
            return _lastResponse == PolicyServerResponse.Licensed;
        }

        #endregion
    }
}