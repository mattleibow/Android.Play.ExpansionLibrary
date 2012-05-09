namespace ExpansionDownloader.impl
{
    /// <summary>
    /// The download service requirement.
    /// </summary>
    public enum DownloadServiceRequirement
    {
        /// <summary>
        /// The download required.
        /// </summary>
        DownloadRequired = 2, 

        /// <summary>
        /// The lvl check required.
        /// </summary>
        LvlCheckRequired = 1, 

        /// <summary>
        /// The no download required.
        /// </summary>
        NoDownloadRequired = 0
    }
}