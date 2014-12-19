// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloadStatusExtras.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   Used with <see cref="ExpansionDownloadStatus" />
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader
{
    /// <summary>
    /// Used with <see cref="ExpansionDownloadStatus"/>
    /// </summary>
    public class DownloadStatusExtras
    {
        #region Constants

        /// <summary>
        /// The current file size.
        /// </summary>
        public const string CurrentFileSize = "CFS";

        /// <summary>
        /// The current progress.
        /// </summary>
        public const string CurrentProgress = "CFP";

        /// <summary>
        /// The state.
        /// </summary>
        public const string State = "ESS";

        /// <summary>
        /// The total progress.
        /// </summary>
        public const string TotalProgress = "TFP";

        /// <summary>
        /// The total size.
        /// </summary>
        public const string TotalSize = "ETS";

        #endregion
    }
}