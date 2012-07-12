// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloadInfo.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The download info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader.Service
{
    using System;
    using System.Text;

    using LicenseVerificationLibrary.Policy;

    /// <summary>
    /// The download info.
    /// </summary>
    [Serializable]
    public class DownloadInfo
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadInfo"/> class.
        /// </summary>
        public DownloadInfo()
        {
            // this.Fuzz = Helpers.Random.Next(1001);
            this.ResetDownload();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets CurrentBytes.
        /// </summary>
        public long CurrentBytes { get; set; }

        /// <summary>
        /// Gets or sets ETag.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets ExpansionFileType.
        /// </summary>
        public ApkExpansionPolicy.ExpansionFileType ExpansionFileType { get; set; }

        /// <summary>
        /// Gets or sets FailedCount.
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Gets or sets FileName.
        /// </summary>
        public string FileName { get; set; }

        ///// <summary>
        ///// Gets or sets Fuzz.
        ///// </summary>
        // public int Fuzz { get; set; }

        /// <summary>
        /// Gets or sets LastModified.
        /// </summary>
        public long LastModified { get; set; }

        /// <summary>
        /// Gets or sets Package.
        /// </summary>
        public string Package { get; set; }

        /// <summary>
        /// Gets or sets RedirectCount.
        /// </summary>
        public int RedirectCount { get; set; }

        /// <summary>
        /// Gets or sets RetryAfter.
        /// </summary>
        public int RetryAfter { get; set; }

        /// <summary>
        /// Gets or sets Status.
        /// </summary>
        public DownloadStatus Status { get; set; }

        /// <summary>
        /// Gets or sets TotalBytes.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets Uri.
        /// </summary>
        public string Uri { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The reset download.
        /// </summary>
        public void ResetDownload()
        {
            this.CurrentBytes = 0;
            this.ETag = string.Empty;
            this.LastModified = 0;
            this.Status = DownloadStatus.Unknown;
            this.FailedCount = 0;
            this.RetryAfter = 0;
            this.RedirectCount = 0;
        }

        ///// <summary>
        ///// Returns the time when a download should be restarted.
        ///// </summary>
        ///// <param name="now">
        ///// The now.
        ///// </param>
        ///// <returns>
        ///// The restart time.
        ///// </returns>
        // public long RestartTime(long now)
        // {
        // if (this.FailedCount == 0)
        // {
        // return now;
        // }
        // if (this.RetryAfter > 0)
        // {
        // return this.LastModified + this.RetryAfter;
        // }
        // var fuzz = 1000 + this.Fuzz;
        // var failCount = 1 << (this.FailedCount - 1);
        // var i = DownloaderService.RetryFirstDelay * fuzz * failCount;
        // var restartTime = this.LastModified + i;
        // return restartTime;
        // }

        /// <summary>
        /// Convert the download info object into a string.
        /// </summary>
        /// <returns>
        /// A string representing the download info.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder("Service adding new entry");

            sb.AppendLine("PACKAGE : " + this.Package);
            sb.AppendLine("URI     : " + this.Uri);
            sb.AppendLine("FILENAME: " + this.FileName);
            sb.AppendLine("STATUS  : " + this.Status);
            sb.AppendLine("FAILED_C: " + this.FailedCount);
            sb.AppendLine("RETRY_AF: " + this.RetryAfter);
            sb.AppendLine("REDIRECT: " + this.RedirectCount);
            sb.AppendLine("LAST_MOD: " + this.LastModified);
            sb.AppendLine("TOTAL   : " + this.TotalBytes);
            sb.AppendLine("CURRENT : " + this.CurrentBytes);
            sb.AppendLine("ETAG    : " + this.ETag);

            return sb.ToString();
        }

        #endregion
    }
}