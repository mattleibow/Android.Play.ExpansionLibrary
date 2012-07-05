// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ClientMessageParameters.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The downloader client message parameters.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader.Client
{
    /// <summary>
    /// The downloader client message parameters.
    /// </summary>
    public static class ClientMessageParameters
    {
        #region Constants

        /// <summary>
        /// The messenger.
        /// </summary>
        public const string Messenger = DownloaderServiceExtras.MessageHandler;

        /// <summary>
        /// The new state.
        /// </summary>
        public const string NewState = "newState";

        /// <summary>
        /// The progress.
        /// </summary>
        public const string Progress = "progress";

        #endregion
    }
}