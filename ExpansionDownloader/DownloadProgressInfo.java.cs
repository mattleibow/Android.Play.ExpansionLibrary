namespace ExpansionDownloader
{
    public class DownloadProgressInfo
    {
        public float mCurrentSpeed; // speed in KB/S
        public long mOverallProgress;
        public long mOverallTotal;
        public long mTimeRemaining; // time remaining

        public DownloadProgressInfo(string parcel)
        {
            string[] items = parcel.Split('|');
            mOverallTotal = long.Parse(items[0]);
            mOverallProgress = long.Parse(items[1]);
            mTimeRemaining = long.Parse(items[2]);
            mCurrentSpeed = float.Parse(items[3]);
        }

        public DownloadProgressInfo(long overallTotal, long overallProgress, long timeRemaining, float currentSpeed)
        {
            mOverallTotal = overallTotal;
            mOverallProgress = overallProgress;
            mTimeRemaining = timeRemaining;
            mCurrentSpeed = currentSpeed;
        }

        public override string ToString()
        {
            return string.Join("|", mOverallTotal, mOverallProgress, mTimeRemaining, mCurrentSpeed.ToString("0.00"));
        }
    }
}