// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExpansionDownloadStatusExtensions.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   Extension methods to help filter the <see cref="ExpansionDownloadStatus" /> values.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader
{
    /// <summary>
    /// Extension methods to help filter the <see cref="ExpansionDownloadStatus"/> values.
    /// </summary>
    public static class ExpansionDownloadStatusExtensions
    {
        #region Public Methods and Operators

        /// <summary>
        /// Returns whether the status is a client error (i.e. 4xx).
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        /// <returns>
        /// The is status client error.
        /// </returns>
        public static bool IsClientError(this ExpansionDownloadStatus status)
        {
            return status >= ExpansionDownloadStatus.ClientErrorMinimum && status <= ExpansionDownloadStatus.ClientErrorMaximum;
        }

        /// <summary>
        /// Returns whether the download has completed (either with success or
        /// error).
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        /// <returns>
        /// The is status completed.
        /// </returns>
        public static bool IsCompleted(this ExpansionDownloadStatus status)
        {
            return status.IsSuccess() || status.IsError();
        }

        /// <summary>
        /// Returns whether the status is an error (i.e. 4xx or 5xx).
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        /// <returns>
        /// The is status error.
        /// </returns>
        public static bool IsError(this ExpansionDownloadStatus status)
        {
            return status >= ExpansionDownloadStatus.AnyErrorMinimum && status <= ExpansionDownloadStatus.AnyErrorMaximum;
        }

        /// <summary>
        /// Returns whether the status is informational (i.e. 1xx).
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        /// <returns>
        /// The is status informational.
        /// </returns>
        public static bool IsInformational(this ExpansionDownloadStatus status)
        {
            return status >= ExpansionDownloadStatus.InformationalMinimum && status <= ExpansionDownloadStatus.InformationalMaximum;
        }

        /// <summary>
        /// Returns whether the status is a redirect (i.e. 3xx).
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        /// <returns>
        /// The status a redirect.
        /// </returns>
        public static bool IsRedirect(this ExpansionDownloadStatus status)
        {
            return status >= ExpansionDownloadStatus.RedirectMinimum && status <= ExpansionDownloadStatus.RedirectMaximum;
        }

        /// <summary>
        /// Returns whether the status is a server error (i.e. 5xx).
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        /// <returns>
        /// The is status server error.
        /// </returns>
        public static bool IsServerError(this ExpansionDownloadStatus status)
        {
            return status >= ExpansionDownloadStatus.ServerErrorMinimum && status <= ExpansionDownloadStatus.ServerErrorMaximum;
        }

        /// <summary>
        /// Returns whether the status is a success (i.e. 2xx).
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        /// <returns>
        /// The is status success.
        /// </returns>
        public static bool IsSuccess(this ExpansionDownloadStatus status)
        {
            return status >= ExpansionDownloadStatus.SuccessMinimum && status <= ExpansionDownloadStatus.SuccessMaximum;
        }

        #endregion
    }
}