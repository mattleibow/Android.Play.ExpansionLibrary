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

        /// <summary>
        /// Implies that Wi-Fi is unavailable and cellular permission will 
        /// restart the service (Wi-Fi manager is returning that Wi-Fi is not 
        /// enabled).
        /// </summary>
        PausedWifiDisabledNeedCellularPermission = 8,

        /// <summary>
        /// Implies that Wi-Fi is unavailable and cellular permission will 
        /// restart the service (Wi-Fi is enabled but not available).
        /// </summary>
        PausedNeedCellularPermission = 9,

        PausedRoaming = 10,

        /// <summary>
        /// We were on a network that redirected us to another website
        /// that delivered us the wrong file.
        /// </summary>
        PausedNetworkSetupFailure = 11,

        PausedSdCardUnavailable = 12,

        FailedUnlicensed = 13,
        FailedFetchingUrl = 14,
        FailedSdCardFull = 15,
        FailedCanceled = 16,

        Failed = 17
    }
}