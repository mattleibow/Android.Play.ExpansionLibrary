// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceParameters.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The downloader service parameters.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader.Service
{
    /// <summary>
    /// The downloader service parameters.
    /// </summary>
    public static class ServiceParameters
    {
        #region Constants

        /// <summary>
        /// The flags parameter.
        /// </summary>
        public const string Flags = "flags";

        /// <summary>
        /// The messenger parameter.
        /// </summary>
        public const string Messenger = DownloaderServiceExtras.MessageHandler;

        #endregion
    }
}