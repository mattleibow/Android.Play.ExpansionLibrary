namespace LicenseVerificationLibrary
{
    /// <summary>
    /// The i policy.
    /// </summary>
    public interface IPolicy
    {
        #region Public Methods and Operators

        /// <summary>
        /// Check if the user should be allowed access to the application.
        /// </summary>
        /// <returns>
        /// The allow access.
        /// </returns>
        bool AllowAccess();

        /// <summary>
        /// Provide results from contact with the license server. 
        /// Retry counts are incremented if the current value of response is 
        /// <see cref="PolicyServerResponse.Retry"/>. 
        /// Results will be used for any future policy decisions.
        /// </summary>
        /// <param name="response">
        /// The result from validating the server response
        /// </param>
        /// <param name="rawData">
        /// The raw server response data, can be null for 
        /// <see cref="PolicyServerResponse.Retry"/>
        /// </param>
        void ProcessServerResponse(PolicyServerResponse response, ResponseData rawData);

        #endregion
    }
}