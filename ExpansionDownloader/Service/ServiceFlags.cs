// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceFlags.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   Flags for a download
// </summary>
// --------------------------------------------------------------------------------------------------------------------

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