namespace ExpansionDownloader.impl
{
    public enum ControlAction
    {
        /// <summary>
        /// This download is allowed to run.
        /// </summary>
        Run = 0,

        /// <summary>
        /// This download must pause at the first opportunity.
        /// </summary>
        Paused = 1
    }
}