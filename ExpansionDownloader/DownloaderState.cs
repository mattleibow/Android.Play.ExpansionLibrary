namespace ExpansionDownloader
{
    /// <summary>
    /// The downloader client state.
    /// </summary>
    public enum DownloaderState
    {
        /// <summary>
        /// The unknown.
        /// </summary>
        Unknown = -1, 

        /// <summary>
        /// The idle.
        /// </summary>
        Idle = 1, 

        /// <summary>
        /// The fetching url.
        /// </summary>
        FetchingUrl = 2, 

        /// <summary>
        /// The connecting.
        /// </summary>
        Connecting = 3, 

        /// <summary>
        /// The downloading.
        /// </summary>
        Downloading = 4, 

        /// <summary>
        /// The completed.
        /// </summary>
        Completed = 5, 

        /// <summary>
        /// The paused network unavailable.
        /// </summary>
        PausedNetworkUnavailable = 6, 

        /// <summary>
        /// The paused by request.
        /// </summary>
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

        /// <summary>
        /// The paused roaming.
        /// </summary>
        PausedRoaming = 10, 

        /// <summary>
        /// We were on a network that redirected us to another website
        /// that delivered us the wrong file.
        /// </summary>
        PausedNetworkSetupFailure = 11, 

        /// <summary>
        /// The paused sd card unavailable.
        /// </summary>
        PausedSdCardUnavailable = 12, 

        /// <summary>
        /// The failed unlicensed.
        /// </summary>
        FailedUnlicensed = 13, 

        /// <summary>
        /// The failed fetching url.
        /// </summary>
        FailedFetchingUrl = 14, 

        /// <summary>
        /// The failed sd card full.
        /// </summary>
        FailedSdCardFull = 15, 

        /// <summary>
        /// The failed canceled.
        /// </summary>
        FailedCanceled = 16, 

        /// <summary>
        /// The failed.
        /// </summary>
        Failed = 17
    }
}