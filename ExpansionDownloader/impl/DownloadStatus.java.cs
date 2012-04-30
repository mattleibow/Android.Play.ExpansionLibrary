namespace ExpansionDownloader.impl
{
    /// <summary>
    /// Lists the states that the download manager can set on a download to 
    /// notify applications of the download progress. 
    /// 
    /// The codes follow the HTTP families:
    ///   1xx: informational
    ///   2xx: success
    ///   3xx: redirects (not used by the download manager)
    ///   4xx: client errors
    ///   5xx: server errors
    /// </summary>
    public class DownloadStatus
    {
        /// <summary>
        /// This download hasn't stated yet.
        /// </summary>
        public const int Pending = 190;

        /// <summary>
        /// This download has started.
        /// </summary>
        public const int Running = 192;

        /// <summary>
        /// This download has been paused by the owning app.
        /// </summary>
        public const int PausedByApp = 193;

        /// <summary>
        /// This download encountered some network error and is waiting before 
        /// retrying the request.
        /// </summary>
        public const int WaitingToRetry = 194;

        /// <summary>
        /// This download is waiting for network connectivity to proceed.
        /// </summary>
        public const int WaitingForNetwork = 195;

        /// <summary>
        /// This download exceeded a size limit for mobile networks and is
        /// waiting for a Wi-Fi connection to proceed.
        /// </summary>
        public const int QueuedForWifi = 196;

        /// <summary>
        /// This download has successfully completed. Warning: there might be 
        /// other status values that indicate success in the future. 
        /// 
        /// Use isSucccess() to capture the entire category.
        /// </summary>
        public const int Success = 200;

        /// <summary>
        /// The requested URL is no longer available.
        /// </summary>
        public const int Forbidden = 403;

        /// <summary>
        /// The file was delivered incorrectly.
        /// </summary>
        public const int FileDeliveredIncorrectly = 487;

        /// <summary>
        /// The requested destination file already exists.
        /// </summary>
        public const int FileAlreadyExists = 488;

        /// <summary>
        /// Some possibly transient error occurred, but we can't resume the 
        /// download.
        /// </summary>
        public const int CannotResume = 489;

        /// <summary>
        /// This download was canceled
        /// </summary>
        public const int Canceled = 490;

        /// <summary>
        /// This download has completed with an error. Warning: there will be 
        /// other status values that indicate errors in the future. 
        /// 
        /// Use isStatusError() to capture the entire category.
        /// </summary>
        public const int UnknownError = 491;

        /// <summary>
        /// This download couldn't be completed because of a storage issue.
        /// Typically, that's because the filesystem is missing or full.
        /// 
        /// Use the more specific {@link #InsufficientSpaceError} and
        /// {@link #DeviceNotFoundError} when appropriate.
        /// </summary>
        public const int FileError = 492;

        /// <summary>
        /// This download couldn't be completed because of an HTTP redirect 
        /// response that the download manager couldn't handle.
        /// </summary>
        public const int UnhandledRedirect = 493;

        /// <summary>
        /// This download couldn't be completed because of an unspecified 
        /// unhandled HTTP code.
        /// </summary>
        public const int UnhandledHttpCode = 494;

        /// <summary>
        /// This download couldn't be completed because of an error receiving 
        /// or processing data at the HTTP level.
        /// </summary>
        public const int HttpDataError = 495;

        /// <summary>
        /// This download couldn't be completed because of an HttpException 
        /// while setting up the request.
        /// </summary>
        public const int HttpException = 496;

        /// <summary>
        /// This download couldn't be completed because there were too many 
        /// redirects.
        /// </summary>
        public const int TooManyRedirects = 497;

        /// <summary>
        /// This download couldn't be completed due to insufficient storage 
        /// space. Typically, this is because the SD card is full.
        /// </summary>
        public const int InsufficientSpaceError = 498;

        /// <summary>
        /// This download couldn't be completed because no external storage 
        /// device was found. Typically, this is because the SD card is not 
        /// mounted.
        /// </summary>
        public const int DeviceNotFoundError = 499;

        /// <summary>
        /// This request couldn't be parsed. This is also used when processing 
        /// requests with unknown/unsupported URI schemes.
        /// </summary>
        public static int BadRequest = 400;

        /// <summary>
        /// This download can't be performed because the content type cannot 
        /// be handled.
        /// </summary>
        public static int NotAcceptable = 406;

        /// <summary>
        /// This download cannot be performed because the length cannot be
        /// determined accurately. 
        /// 
        /// This is the code for the HTTP error "Length Required", which is 
        /// typically used when making requests that require a content length 
        /// but don't have one, and it is also used in the client when a 
        /// response is received whose length cannot be determined accurately 
        /// (thus making it impossible to know when a download completes).
        /// </summary>
        public static int LengthRequired = 411;

        /// <summary>
        /// This download was interrupted and cannot be resumed.
        /// 
        /// This is the code for the HTTP error "Precondition Failed", and it 
        /// is also used in situations where the client doesn't have an ETag 
        /// at all.
        /// </summary>
        public static int PreconditionFailed = 412;

        /// <summary>
        /// The lowest-valued error status that is not an actual HTTP status 
        /// code.
        /// </summary>
        public static int MinimumArtificialErrorStatus = 488;
    }
}