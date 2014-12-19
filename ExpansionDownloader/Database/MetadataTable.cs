// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetadataTable.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The metadata columns.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader.Database
{
    using System;

    using ExpansionDownloader.Service;

    /// <summary>
    /// The metadata columns.
    /// </summary>
    [Serializable]
    public class MetadataTable
    {
        #region Public Properties

        /// <summary>
        /// The apk version.
        /// </summary>
        public int ApkVersion { get; set; }

        /// <summary>
        /// The download status.
        /// </summary>
        public ExpansionDownloadStatus DownloadStatus { get; set; }

        /// <summary>
        /// The flags.
        /// </summary>
        public ServiceFlags Flags { get; set; }

        #endregion
    }
}