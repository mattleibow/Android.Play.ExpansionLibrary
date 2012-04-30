using System.Text;
using LicenseVerificationLibrary;

namespace ExpansionDownloader.impl
{
    public class DownloadInfo
    {
        public DownloadInfo(int fileType, string fileName, string package)
        {
            Fuzz = Helpers.Random.Next(1001);
            FileName = fileName;
            Package = package;
            ExpansionFileType = fileType;
        }

        public int Control { get; set; }
        public long CurrentBytes { get; set; }
        public string ETag { get; set; }
        public string FileName { get; set; }
        public string Package { get; set; }
        public int Fuzz { get; set; }
        public int ExpansionFileType { get; set; }
        public long LastModified { get; set; }
        public int FailedCount { get; set; }
        public int RedirectCount { get; set; }
        public int RetryAfter { get; set; }
        public int Status { get; set; }
        public long TotalBytes { get; set; }
        public string Uri { get; set; }

        public void ResetDownload()
        {
            CurrentBytes = 0;
            ETag = string.Empty;
            LastModified = 0;
            Status = 0;
            Control = 0;
            FailedCount = 0;
            RetryAfter = 0;
            RedirectCount = 0;
        }

        /// <summary>
        ///   Returns the time when a download should be restarted.
        /// </summary>
        public long RestartTime(long now)
        {
            if (FailedCount == 0)
            {
                return now;
            }
            if (RetryAfter > 0)
            {
                return LastModified + RetryAfter;
            }
            return LastModified + DownloaderService.RETRY_FIRST_DELAY*(1000 + Fuzz)*(1 << FailedCount - 1);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("Service adding new entry");

            sb.AppendLine("PACKAGE : " + Package);
            sb.AppendLine("URI     : " + Uri);
            sb.AppendLine("FILENAME: " + FileName);
            sb.AppendLine("CONTROL : " + Control);
            sb.AppendLine("STATUS  : " + Status);
            sb.AppendLine("FAILED_C: " + FailedCount);
            sb.AppendLine("RETRY_AF: " + RetryAfter);
            sb.AppendLine("REDIRECT: " + RedirectCount);
            sb.AppendLine("LAST_MOD: " + LastModified);
            sb.AppendLine("TOTAL   : " + TotalBytes);
            sb.AppendLine("CURRENT : " + CurrentBytes);
            sb.AppendLine("ETAG    : " + ETag);

            return sb.ToString();
        }
    }
}