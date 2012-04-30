namespace LicenseVerificationLibrary
{
    public interface IPolicy
    {
        /// <summary>
        ///   Provide results from contact with the license server. Retry counts are
        ///   incremented if the current value of response is RETRY. Results will be
        ///   used for any future policy decisions.
        /// </summary>
        /// <param name = "response">The result from validating the server response</param>
        /// <param name = "rawData">The raw server response data, can be null for RETRY</param>
        void ProcessServerResponse(PolicyServerResponse response, ResponseData rawData);

        /// <summary>
        ///   Check if the user should be allowed access to the application.
        /// </summary>
        bool AllowAccess();
    }
}