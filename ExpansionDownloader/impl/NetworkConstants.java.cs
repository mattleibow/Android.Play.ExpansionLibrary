namespace ExpansionDownloader.impl
{
    /// <summary>
    /// the following NETWORK_* constants are used to indicates specific reasons for disallowing a
    /// download from using a network, since specific causes can require special handling.
    /// </summary>
    public class NetworkConstants
    {
        /**
     * The network is usable for the given download.
     */
        public const int NETWORK_OK = 1;

        /**
     * There is no network connectivity.
     */
        public const int NETWORK_NO_CONNECTION = 2;

        /**
     * The download exceeds the maximum size for this network.
     */
        public const int NETWORK_UNUSABLE_DUE_TO_SIZE = 3;

        /**
     * The download exceeds the recommended maximum size for this network, the
     * user must confirm for this download to proceed without WiFi.
     */
        public const int NETWORK_RECOMMENDED_UNUSABLE_DUE_TO_SIZE = 4;

        /**
     * The current connection is roaming, and the download can't proceed over a
     * roaming connection.
     */
        public const int NETWORK_CANNOT_USE_ROAMING = 5;

        /**
     * The app requesting the download specific that it can't use the current
     * network connection.
     */
        public const int NETWORK_TYPE_DISALLOWED_BY_REQUESTOR = 6;
    }
}