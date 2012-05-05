namespace ExpansionDownloader.impl
{
    public enum DownloaderServiceVisibility
    {
        /// <summary>
        /// This download is visible but only shows in the notifications while it's in progress.
        /// </summary>
        Visible = 0,

        /// <summary>
        /// This download is visible and shows in the notifications while in progress and after completion.
        /// </summary>
        VisibleNotifyCompleted = 1,

        /// <summary>
        /// This download doesn't show in the UI or in the notifications.
        /// </summary>
        Hidden = 2
    }
}