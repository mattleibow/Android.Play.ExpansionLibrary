using System.IO;

namespace ExpansionDownloader
{
    public class Constants
    {
        /** Tag used for debugging/logging */
        public static string TAG = "LVLDL";

        /**
     * Expansion path where we store obb files
     */
        public static string EXP_PATH = Path.PathSeparator + "Android" + Path.PathSeparator + "obb" + Path.PathSeparator;

        /** The intent that gets sent when the service must wake up for a retry */
        public static string ACTION_RETRY = "android.intent.action.DOWNLOAD_WAKEUP";

        /** the intent that gets sent when clicking a successful download */
        public static string ACTION_OPEN = "android.intent.action.DOWNLOAD_OPEN";

        /** the intent that gets sent when clicking an incomplete/failed download  */
        public static string ACTION_LIST = "android.intent.action.DOWNLOAD_LIST";

        /** the intent that gets sent when deleting the notification of a completed download */
        public static string ACTION_HIDE = "android.intent.action.DOWNLOAD_HIDE";

        /**
     * When a number has to be appended to the filename, this string is used to separate the
     * base filename from the sequence number
     */
        public static string FILENAME_SEQUENCE_SEPARATOR = "-";

        /** The default user agent used for downloads */
        public static string DEFAULT_USER_AGENT = "Android.LVLDM";

        /** The buffer size used to stream the data */
        public static int BUFFER_SIZE = 4096;

        /** The minimum amount of progress that has to be done before the progress bar gets updated */
        public static int MIN_PROGRESS_STEP = 4096;

        /** The minimum amount of time that has to elapse before the progress bar gets updated, in ms */
        public static long MIN_PROGRESS_TIME = 1000;

        /** The maximum number of rows in the database (FIFO) */
        public static int MAX_DOWNLOADS = 1000;

        /**
     * The number of times that the download manager will retry its network
     * operations when no progress is happening before it gives up.
     */
        public static int MAX_RETRIES = 5;

        /**
     * The minimum amount of time that the download manager accepts for
     * a Retry-After response header with a parameter in delta-seconds.
     */
        public static int MIN_RETRY_AFTER = 30; // 30s

        /**
     * The maximum amount of time that the download manager accepts for
     * a Retry-After response header with a parameter in delta-seconds.
     */
        public static int MAX_RETRY_AFTER = 24*60*60; // 24h

        /**
     * The maximum number of redirects.
     */
        public static int MAX_REDIRECTS = 5; // can't be more than 7.

        /**
     * The time between a failure and the first retry after an IOException.
     * Each subsequent retry grows exponentially, doubling each time.
     * The time is in seconds.
     */
        public static int RETRY_FIRST_DELAY = 30;

        /** Enable separate connectivity logging */
        public static bool LOGX = true;

        /** Enable verbose logging */
        public static bool LOGV = true;

        /** Enable super-verbose logging */
        private static bool LOCAL_LOGVV = true;
        public static bool LOGVV = LOCAL_LOGVV && LOGV;

        /**
     * This download has successfully completed.
     * Warning: there might be other status values that indicate success
     * in the future.
     * Use isSucccess() to capture the entire category.
     */
        public static int STATUS_SUCCESS = 200;

        /**
     * This request couldn't be parsed. This is also used when processing
     * requests with unknown/unsupported URI schemes.
     */
        public static int STATUS_BAD_REQUEST = 400;

        /**
     * This download can't be performed because the content type cannot be
     * handled.
     */
        public static int STATUS_NOT_ACCEPTABLE = 406;

        /**
     * This download cannot be performed because the length cannot be
     * determined accurately. This is the code for the HTTP error "Length
     * Required", which is typically used when making requests that require
     * a content length but don't have one, and it is also used in the
     * client when a response is received whose length cannot be determined
     * accurately (therefore making it impossible to know when a download
     * completes).
     */
        public static int STATUS_LENGTH_REQUIRED = 411;

        /**
     * This download was interrupted and cannot be resumed.
     * This is the code for the HTTP error "Precondition Failed", and it is
     * also used in situations where the client doesn't have an ETag at all.
     */
        public static int STATUS_PRECONDITION_FAILED = 412;

        /**
     * The lowest-valued error status that is not an actual HTTP status code.
     */
        public static int MIN_ARTIFICIAL_ERROR_STATUS = 488;

        /**
     * The requested destination file already exists.
     */
        public static int STATUS_FILE_ALREADY_EXISTS_ERROR = 488;

        /**
     * Some possibly transient error occurred, but we can't resume the download.
     */
        public static int STATUS_CANNOT_RESUME = 489;

        /**
     * This download was canceled
     */
        public static int STATUS_CANCELED = 490;

        /**
     * This download has completed with an error.
     * Warning: there will be other status values that indicate errors in
     * the future. Use isStatusError() to capture the entire category.
     */
        public static int STATUS_UNKNOWN_ERROR = 491;

        /**
     * This download couldn't be completed because of a storage issue.
     * Typically, that's because the filesystem is missing or full.
     * Use the more specific {@link #STATUS_INSUFFICIENT_SPACE_ERROR}
     * and {@link #STATUS_DEVICE_NOT_FOUND_ERROR} when appropriate.
     */
        public static int STATUS_FILE_ERROR = 492;

        /**
     * This download couldn't be completed because of an HTTP
     * redirect response that the download manager couldn't
     * handle.
     */
        public static int STATUS_UNHANDLED_REDIRECT = 493;

        /**
     * This download couldn't be completed because of an
     * unspecified unhandled HTTP code.
     */
        public static int STATUS_UNHANDLED_HTTP_CODE = 494;

        /**
     * This download couldn't be completed because of an
     * error receiving or processing data at the HTTP level.
     */
        public static int STATUS_HTTP_DATA_ERROR = 495;

        /**
     * This download couldn't be completed because of an
     * HttpException while setting up the request.
     */
        public static int STATUS_HTTP_EXCEPTION = 496;

        /**
     * This download couldn't be completed because there were
     * too many redirects.
     */
        public static int STATUS_TOO_MANY_REDIRECTS = 497;

        /**
     * This download couldn't be completed due to insufficient storage
     * space.  Typically, this is because the SD card is full.
     */
        public static int STATUS_INSUFFICIENT_SPACE_ERROR = 498;

        /**
     * This download couldn't be completed because no external storage
     * device was found.  Typically, this is because the SD card is not
     * mounted.
     */
        public static int STATUS_DEVICE_NOT_FOUND_ERROR = 499;

        /**
     * The wake duration to check to see if a download is possible.
     */
        public static long WATCHDOG_WAKE_TIMER = 60*1000;

        /**
     * The wake duration to check to see if the process was killed.
     */
        public static long ACTIVE_THREAD_WATCHDOG = 5*1000;
    }
}