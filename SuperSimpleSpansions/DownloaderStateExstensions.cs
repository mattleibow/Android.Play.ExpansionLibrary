namespace SuperSimpleSpansions
{
    using ExpansionDownloader;
    using ExpansionDownloader.Core;

    public static class DownloaderStateExstensions
    {
        public static bool IsIndeterminate(this DownloaderState newState)
        {
            bool indeterminate = false;
            switch (newState)
            {
                case DownloaderState.Idle:
                case DownloaderState.Connecting:
                case DownloaderState.FetchingUrl:
                    indeterminate = true;
                    break;
            }
            return indeterminate;
        }

        public static bool IsPaused(this DownloaderState newState)
        {
            bool paused = true;
            switch (newState)
            {
                case DownloaderState.Idle:
                case DownloaderState.Connecting:
                case DownloaderState.FetchingUrl:
                case DownloaderState.Downloading:
                    paused = false;
                    break;
            }
            return paused;
        }

        public static bool CanShowProgress(this DownloaderState newState)
        {
            bool showDashboard = true;
            switch (newState)
            {
                case DownloaderState.Failed:
                case DownloaderState.FailedCanceled:
                case DownloaderState.FailedFetchingUrl:
                case DownloaderState.FailedUnlicensed:
                case DownloaderState.PausedNeedCellularPermission:
                case DownloaderState.PausedWifiDisabledNeedCellularPermission:
                    showDashboard = false;
                    break;
            }
            return showDashboard;
        }

        public static bool IsWaitingForCellApproval(this DownloaderState newState)
        {
            bool showCellMessage = false;
            switch (newState)
            {
                case DownloaderState.PausedNeedCellularPermission:
                case DownloaderState.PausedWifiDisabledNeedCellularPermission:
                    showCellMessage = true;
                    break;
            }
            return showCellMessage;
        }
    }
}