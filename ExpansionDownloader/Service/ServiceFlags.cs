namespace ExpansionDownloader.Service
{
    using System;

    /// <summary>
    /// Flags for a download
    /// </summary>
    [Flags]
    public enum ServiceFlags
    {
        /// <summary>
        /// Set this flag in response to the 
        /// <see cref="DownloaderState.PausedNeedCellularPermission"/> 
        /// state and then call RequestContinueDownload to resume a download
        /// </summary>
        FlagsDownloadOverCellular = 1
    }
}