namespace LicenseVerificationLibrary
{
    /**
     * Policy used by {@link LicenseChecker} to determine whether a user should have
     * access to the application.
     */
    public interface Policy
    {
        /**
         * Provide results from contact with the license server. Retry counts are
         * incremented if the current value of response is RETRY. Results will be
         * used for any future policy decisions.
         * 
         * @param response
         *            the result from validating the server response
         * @param rawData
         *            the raw server response data, can be null for RETRY
         */
        void processServerResponse(PolicyLicenseResponse response, ResponseData rawData);

        /**
         * Check if the user should be allowed access to the application.
         */
        bool allowAccess();
    }

    /**
     * Change these values to make it more difficult for tools to automatically
     * strip LVL protection from your APK.
     */
    public enum PolicyLicenseResponse
    {
        /**
         * LICENSED means that the server returned back a valid license response
         */
        LICENSED = 0x0100,
        /**
         * NOT_LICENSED means that the server returned back a valid license response
         * that indicated that the user definitively is not licensed
         */
        NOT_LICENSED = 0x0231,
        /**
         * RETRY means that the license response was unable to be determined ---
         * perhaps as a result of faulty networking
         */
        RETRY = 0x0123
    }
}
