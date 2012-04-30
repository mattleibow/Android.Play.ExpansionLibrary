namespace ExpansionDownloader
{
    public enum DownloaderClientState
    {
        Unknown = -1,

        Idle = 1,
        FetchingUrl = 2,
        Connecting = 3,
        Downloading = 4,
        Completed = 5,

        PausedNetworkUnavailable = 6,
        PausedByRequest = 7,

        /**
         * Both PausedWifiDisabledNeedCellularPermission and
         * PausedNeedCellularPermission imply that Wi-Fi is unavailable and
         * cellular permission will restart the service. 
         * 
         * Wi-Fi disabled means that
         * the Wi-Fi manager is returning that Wi-Fi is not enabled, while in
         * the other case Wi-Fi is enabled but not available.
         **/
        PausedWifiDisabledNeedCellularPermission = 8,
        PausedNeedCellularPermission = 9,
        PausedRoaming = 10,

        /**
         * Scary case.
         * 
         * We were on a network that redirected us to another website
         * that delivered us the wrong file.
         **/
        STATE_PAUSED_NETWORK_SETUP_FAILURE = 11,

        STATE_PAUSED_SDCARD_UNAVAILABLE = 12,

        STATE_FAILED_UNLICENSED = 13,
        STATE_FAILED_FETCHING_URL = 14,
        STATE_FAILED_SDCARD_FULL = 15,
        STATE_FAILED_CANCELED = 16,

        STATE_FAILED = 17
    }
}