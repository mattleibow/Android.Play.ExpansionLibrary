namespace ExpansionDownloader.Client
{
    using ExpansionDownloader.impl;

    public class DownloaderClientMessageParameters
    {
        public const string NewState = "newState";

        public const string Progress = "progress";

        public const string Messenger = DownloaderServiceExtras.MessageHandler;
    }
}