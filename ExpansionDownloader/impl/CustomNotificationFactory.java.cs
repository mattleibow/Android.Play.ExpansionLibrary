using Java.Lang;

namespace ExpansionDownloader.impl
{
    public static class CustomNotificationFactory
    {
        public static DownloadNotification.ICustomNotification Notification { get; set; }

        /**
     * Returns maximum size, in bytes, of downloads that may go over a mobile connection; or null if
     * there's no limit
     *
     * @param context the {@link Context} to use for accessing the {@link ContentResolver}
     * @return maximum size, in bytes, of downloads that may go over a mobile connection; or null if
     * there's no limit
     */

        public static long MaxBytesOverMobile { get; set; }

        /**
     * Returns recommended maximum size, in bytes, of downloads that may go over a mobile
     * connection; or null if there's no recommended limit.  The user will have the option to bypass
     * this limit.
     *
     * @param context the {@link Context} to use for accessing the {@link ContentResolver}
     * @return recommended maximum size, in bytes, of downloads that may go over a mobile
     * connection; or null if there's no recommended limit.
     */

        public static long RecommendedMaxBytesOverMobile { get; set; }
    }
}