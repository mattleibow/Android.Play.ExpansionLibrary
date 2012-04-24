using Android.Util;

namespace ExpansionDownloader.impl
{
    public class DownloadInfo
    {
        public int mControl;
        public long mCurrentBytes;
        public string mETag;
        public string mFileName;
        public int mFuzz;
        public int mIndex;
        private bool mInitialized;
        public long mLastMod;
        public int mNumFailed;
        public int mRedirectCount;
        public int mRetryAfter;
        public int mStatus;
        public long mTotalBytes;
        public string mUri;

        public DownloadInfo(int index, string fileName, string pkg)
        {
            mFuzz = Helpers.sRandom.NextInt(1001);
            mFileName = fileName;
            mIndex = index;
        }

        public void resetDownload()
        {
            mCurrentBytes = 0;
            mETag = string.Empty;
            mLastMod = 0;
            mStatus = 0;
            mControl = 0;
            mNumFailed = 0;
            mRetryAfter = 0;
            mRedirectCount = 0;
        }

        /**
     * Returns the time when a download should be restarted.
     */

        public long restartTime(long now)
        {
            if (mNumFailed == 0)
            {
                return now;
            }
            if (mRetryAfter > 0)
            {
                return mLastMod + mRetryAfter;
            }
            return mLastMod +
                   Constants.RETRY_FIRST_DELAY*
                   (1000 + mFuzz)*(1 << (mNumFailed - 1));
        }

        public void logVerboseInfo()
        {
            Log.Verbose(Constants.TAG, "Service adding new entry");
            Log.Verbose(Constants.TAG, "FILENAME: " + mFileName);
            Log.Verbose(Constants.TAG, "URI     : " + mUri);
            Log.Verbose(Constants.TAG, "FILENAME: " + mFileName);
            Log.Verbose(Constants.TAG, "CONTROL : " + mControl);
            Log.Verbose(Constants.TAG, "STATUS  : " + mStatus);
            Log.Verbose(Constants.TAG, "FAILED_C: " + mNumFailed);
            Log.Verbose(Constants.TAG, "RETRY_AF: " + mRetryAfter);
            Log.Verbose(Constants.TAG, "REDIRECT: " + mRedirectCount);
            Log.Verbose(Constants.TAG, "LAST_MOD: " + mLastMod);
            Log.Verbose(Constants.TAG, "TOTAL   : " + mTotalBytes);
            Log.Verbose(Constants.TAG, "CURRENT : " + mCurrentBytes);
            Log.Verbose(Constants.TAG, "ETAG    : " + mETag);
        }
    }
}