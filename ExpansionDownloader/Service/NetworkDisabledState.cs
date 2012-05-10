namespace ExpansionDownloader.Service
{
    /// <summary>
    /// The following constants are used to indicates specific reasons for disallowing a
    /// download from using a network, since specific causes can require special handling.
    /// </summary>
    public enum NetworkDisabledState
    {
        /// <summary>
        ///  The current connection is roaming, and the download can't proceed over a
        ///  roaming connection.
        /// </summary>
        CannotUseRoaming = 5, 

        /// <summary>
        ///  There is no network connectivity.
        /// </summary>
        NoConnection = 2, 

        /// <summary>
        ///  The network is usable for the given download.
        /// </summary>
        Ok = 1, 

        /// <summary>
        ///  The download exceeds the recommended maximum size for this network, the
        ///  user must confirm for this download to proceed without WiFi.
        /// </summary>
        RecommendedUnusableDueToSize = 4, 

        /// <summary>
        ///  The app requesting the download specific that it can't use the current
        ///  network connection.
        /// </summary>
        TypeDisallowedByRequestor = 6, 

        /// <summary>
        ///  The download exceeds the maximum size for this network.
        /// </summary>
        UnusableDueToSize = 3
    }
}