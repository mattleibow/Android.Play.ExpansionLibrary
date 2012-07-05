// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientMessages.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The downloader client messages.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader.Client
{
    /// <summary>
    /// The downloader client messages.
    /// </summary>
    public enum ClientMessages
    {
        /// <summary>
        /// The download state changed.
        /// </summary>
        DownloadStateChanged = 10, 

        /// <summary>
        /// The download progress.
        /// </summary>
        DownloadProgress = 11, 

        /// <summary>
        /// The service connected.
        /// </summary>
        ServiceConnected = 12
    }
}