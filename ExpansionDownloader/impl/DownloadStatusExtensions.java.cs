namespace ExpansionDownloader.impl
{
    /// <summary>
    /// Extension methods to help filter the <see cref="DownloadStatus" /> values.
    /// </summary>
    public static class DownloadStatusExtensions
    {
        /// <summary>
        /// Returns whether the status is informational (i.e. 1xx).
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        /// <returns>
        /// The is status informational.
        /// </returns>
        public static bool IsInformational(this DownloadStatus status)
        {
            return status >= DownloadStatus.InformationalMinimum && status <= DownloadStatus.InformationalMaximum;
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
        public static bool IsSuccess(this DownloadStatus status)
        {
            return status >= DownloadStatus.SuccessMinimum && status <= DownloadStatus.SuccessMaximum;
        }

        /// <summary>
        /// Returns whether the status is a client error (i.e. 4xx).
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        /// <returns>
        /// The is status client error.
        /// </returns>
        public static bool IsClientError(this DownloadStatus status)
        {
            return status >= DownloadStatus.ClientErrorMinimum && status <= DownloadStatus.ClientErrorMaximum;
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
        public static bool IsCompleted(this DownloadStatus status)
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
        public static bool IsError(this DownloadStatus status)
        {
            return status >= DownloadStatus.AnyErrorMinimum && status <= DownloadStatus.AnyErrorMaximum;
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
        public static bool IsServerError(this DownloadStatus status)
        {
            return status >= DownloadStatus.ServerErrorMinimum && status <= DownloadStatus.ServerErrorMaximum;
        }
    }
}