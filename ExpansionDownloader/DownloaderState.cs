// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloaderState.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The downloader client state.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

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

        PausedWifiDisabled = 10,

        PausedNeedWifi = 11, 

        /// <summary>
        /// The paused roaming.
        /// </summary>
        PausedRoaming = 12, 

        /// <summary>
        /// We were on a network that redirected us to another website
        /// that delivered us the wrong file.
        /// </summary>
        PausedNetworkSetupFailure = 13, 

        /// <summary>
        /// The paused sd card unavailable.
        /// </summary>
        PausedSdCardUnavailable = 14, 

        /// <summary>
        /// The failed unlicensed.
        /// </summary>
        FailedUnlicensed = 15, 

        /// <summary>
        /// The failed fetching url.
        /// </summary>
        FailedFetchingUrl = 16, 

        /// <summary>
        /// The failed sd card full.
        /// </summary>
        FailedSdCardFull = 17, 

        /// <summary>
        /// The failed canceled.
        /// </summary>
        FailedCanceled = 18, 

        /// <summary>
        /// The failed.
        /// </summary>
        Failed = 19
    }
}