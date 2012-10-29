namespace ExpansionDownloader.Service
{
    using System;

    using ExpansionDownloader.Core.Service;

    using LicenseVerificationLibrary.Policy;

    /// <summary>
    /// The download infoBase.
    /// </summary>
    [Serializable]
    public class DownloadInfo : DownloadInfoBase
    {
        /// <summary>
        /// Gets or sets ExpansionFileType.
        /// </summary>
        public ApkExpansionPolicy.ExpansionFileType ExpansionFileType
        {
            get
            {
                return (ApkExpansionPolicy.ExpansionFileType)this.Id;
            }
            set
            {
                this.Id = (int)value;
            }
        }
    }
}