using System;
using System.Reflection;
using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Android.Provider;
using Android.Util;
using Java.Lang;
using LicenseVerificationLibrary;
using Exception = Java.Lang.Exception;

namespace ExpansionDownloader.impl
{
    public class DownloadsDB
    {
        private static string DATABASE_NAME = "DownloadsDB";
        private static int DATABASE_VERSION = 7;
        public static string LOG_TAG = typeof (DownloadsDB).Name;
        private static DownloadsDB mDownloadsDB;

        private static readonly object _locker = new object();

        private static readonly string[] DC_PROJECTION = {
                                                             DownloadColumns.FILENAME,
                                                             DownloadColumns.URI,
                                                             DownloadColumns.ETAG,
                                                             DownloadColumns.TOTALBYTES, 
                                                             DownloadColumns.CURRENTBYTES,
                                                             DownloadColumns.LASTMOD,
                                                             DownloadColumns.STATUS,
                                                             DownloadColumns.CONTROL,
                                                             DownloadColumns.NUM_FAILED,
                                                             DownloadColumns.RETRY_AFTER, 
                                                             DownloadColumns.REDIRECT_COUNT,
                                                             DownloadColumns.INDEX
                                                         };

        private static int FILENAME_IDX;
        private static int URI_IDX = 1;
        private static int ETAG_IDX = 2;
        private static int TOTALBYTES_IDX = 3;
        private static int CURRENTBYTES_IDX = 4;
        private static int LASTMOD_IDX = 5;
        private static int STATUS_IDX = 6;
        private static int CONTROL_IDX = 7;
        private static int NUM_FAILED_IDX = 8;
        private static int RETRY_AFTER_IDX = 9;
        private static int REDIRECT_COUNT_IDX = 10;
        private static int INDEX_IDX = 11;
        private readonly SQLiteOpenHelper mHelper;
        public DownloaderServiceFlags mFlags;
        private SQLiteStatement mGetDownloadByIndex;
        private long mMetadataRowID = -1;
        public DownloadStatus mStatus = DownloadStatus.Unknown;
        private SQLiteStatement mUpdateCurrentBytes;
        public int mVersionCode = -1;

        private DownloadsDB(Context paramContext)
        {
            mHelper = new DownloadsContentDBHelper(paramContext);
            SQLiteDatabase sqldb = mHelper.ReadableDatabase;
            // Query for the version code, the row ID of the metadata (for future
            // updating) the status and the flags
            ICursor cur = sqldb.RawQuery("SELECT " +
                                         MetadataColumns.APKVERSION + "," +
                                         BaseColumns.Id + "," +
                                         MetadataColumns.DOWNLOAD_STATUS + "," +
                                         MetadataColumns.FLAGS +
                                         " FROM "
                                         + MetadataColumns.TABLE_NAME + " LIMIT 1", null);
            if (null != cur && cur.MoveToFirst())
            {
                mVersionCode = cur.GetInt(0);
                mMetadataRowID = cur.GetLong(1);
                mStatus = (DownloadStatus)cur.GetInt(2);
                mFlags = (DownloaderServiceFlags)cur.GetInt(3);
                cur.Close();
            }
            mDownloadsDB = this;
        }

        public static DownloadsDB getDB(Context paramContext)
        {
            lock (_locker)
            {
                if (null == mDownloadsDB)
                {
                    return new DownloadsDB(paramContext);
                }
                return mDownloadsDB;
            }
        }

        private SQLiteStatement getDownloadByIndexStatement()
        {
            if (null == mGetDownloadByIndex)
            {
                mGetDownloadByIndex = mHelper.ReadableDatabase.CompileStatement(
                    "SELECT " + BaseColumns.Id +
                    " FROM " + DownloadColumns.TABLE_NAME +
                    " WHERE " + DownloadColumns.INDEX + " = ?");
            }
            return mGetDownloadByIndex;
        }

        private SQLiteStatement getUpdateCurrentBytesStatement()
        {
            if (null == mUpdateCurrentBytes)
            {
                mUpdateCurrentBytes = mHelper.ReadableDatabase.CompileStatement(
                    "UPDATE " + DownloadColumns.TABLE_NAME +
                    " SET " + DownloadColumns.CURRENTBYTES + " = ?" +
                    " WHERE " + DownloadColumns.INDEX + " = ?");
            }
            return mUpdateCurrentBytes;
        }

        public DownloadInfo getDownloadInfoByFileName(string fileName)
        {
            SQLiteDatabase sqldb = mHelper.ReadableDatabase;
            ICursor itemcur = null;
            try
            {
                itemcur = sqldb.Query(DownloadColumns.TABLE_NAME, DC_PROJECTION,
                                      DownloadColumns.FILENAME + " = ?",
                                      new[]
                                          {
                                              fileName
                                          }, null, null, null);
                if (null != itemcur && itemcur.MoveToFirst())
                {
                    return getDownloadInfoFromCursor(itemcur);
                }
            }
            finally
            {
                if (null != itemcur)
                    itemcur.Close();
            }
            return null;
        }

        public long getIDForDownloadInfo(DownloadInfo di)
        {
            return getIDByIndex(di.ExpansionFileType);
        }

        public long getIDByIndex(int index)
        {
            SQLiteStatement downloadByIndex = getDownloadByIndexStatement();
            downloadByIndex.ClearBindings();
            downloadByIndex.BindLong(1, index);
            try
            {
                return downloadByIndex.SimpleQueryForLong();
            }
            catch (SQLiteDoneException)
            {
                return -1;
            }
        }

        public void updateDownloadCurrentBytes(DownloadInfo di)
        {
            SQLiteStatement downloadCurrentBytes = getUpdateCurrentBytesStatement();
            downloadCurrentBytes.ClearBindings();
            downloadCurrentBytes.BindLong(1, di.CurrentBytes);
            downloadCurrentBytes.BindLong(2, di.ExpansionFileType);
            downloadCurrentBytes.Execute();
        }

        public void close()
        {
            mHelper.Close();
        }

        /**
     * This function will add a new file to the database if it does not exist.
     * 
     * @param di DownloadInfo that we wish to store
     * @return the row id of the record to be updated/inserted, or -1
     */

        public bool updateDownload(DownloadInfo di)
        {
            var cv = new ContentValues();
            cv.Put(DownloadColumns.INDEX, di.ExpansionFileType);
            cv.Put(DownloadColumns.FILENAME, di.FileName);
            cv.Put(DownloadColumns.URI, di.Uri);
            cv.Put(DownloadColumns.ETAG, di.ETag);
            cv.Put(DownloadColumns.TOTALBYTES, di.TotalBytes);
            cv.Put(DownloadColumns.CURRENTBYTES, di.CurrentBytes);
            cv.Put(DownloadColumns.LASTMOD, di.LastModified);
            cv.Put(DownloadColumns.STATUS, (int)di.Status);
            cv.Put(DownloadColumns.CONTROL, di.Control);
            cv.Put(DownloadColumns.NUM_FAILED, di.FailedCount);
            cv.Put(DownloadColumns.RETRY_AFTER, di.RetryAfter);
            cv.Put(DownloadColumns.REDIRECT_COUNT, di.RedirectCount);
            return updateDownload(di, cv);
        }

        public bool updateDownload(DownloadInfo di, ContentValues cv)
        {
            long id = di == null ? -1 : getIDForDownloadInfo(di);
            try
            {
                SQLiteDatabase sqldb = mHelper.WritableDatabase;
                if (id != -1)
                {
                    if (1 != sqldb.Update(DownloadColumns.TABLE_NAME, cv, DownloadColumns._ID + " = " + id, null))
                    {
                        return false;
                    }
                }
                else
                {
                    return -1 != sqldb.Insert(DownloadColumns.TABLE_NAME, DownloadColumns.URI, cv);
                }
            }
            catch (SQLiteException ex)
            {
                ex.PrintStackTrace();
            }
            return false;
        }

        public int getLastCheckedVersionCode()
        {
            return mVersionCode;
        }

        public bool isDownloadRequired()
        {
            SQLiteDatabase sqldb = mHelper.ReadableDatabase;
            ICursor cur = sqldb.RawQuery("SELECT Count(*)" +
                                         " FROM " + DownloadColumns.TABLE_NAME +
                                         " WHERE " + DownloadColumns.STATUS + " <> 0", null);
            try
            {
                if (null != cur && cur.MoveToFirst())
                {
                    return 0 == cur.GetInt(0);
                }
            }
            finally
            {
                if (null != cur)
                    cur.Close();
            }
            return true;
        }

        public DownloaderServiceFlags getFlags()
        {
            return mFlags;
        }

        public bool UpdateFlags(DownloaderServiceFlags flags)
        {
            if (mFlags == flags)
            {
                return true;
            }
            
            var cv = new ContentValues();
            cv.Put(MetadataColumns.FLAGS, (int) flags);
            if (updateMetadata(cv))
            {
                mFlags = flags;
                return true;
            }

            return false;
        }

        public bool updateStatus(DownloadStatus status)
        {
            if (mStatus != status)
            {
                var cv = new ContentValues();
                cv.Put(MetadataColumns.DOWNLOAD_STATUS, (int)status);
                if (updateMetadata(cv))
                {
                    mStatus = status;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public bool updateMetadata(ContentValues cv)
        {
            SQLiteDatabase sqldb = mHelper.WritableDatabase;
            if (-1 == mMetadataRowID)
            {
                long newID = sqldb.Insert(MetadataColumns.TABLE_NAME, MetadataColumns.APKVERSION, cv);
                if (-1 == newID) return false;
                mMetadataRowID = newID;
            }
            else
            {
                if (0 == sqldb.Update(MetadataColumns.TABLE_NAME, cv, BaseColumns.Id + " = " + mMetadataRowID, null))
                    return false;
            }
            return true;
        }

        public bool updateMetadata(int apkVersion, DownloadStatus downloadStatus)
        {
            var cv = new ContentValues();
            cv.Put(MetadataColumns.APKVERSION, apkVersion);
            cv.Put(MetadataColumns.DOWNLOAD_STATUS, (int)downloadStatus);

            if (this.updateMetadata(cv))
            {
                mVersionCode = apkVersion;
                mStatus = downloadStatus;
                return true;
            }

            return false;
        }

        public bool updateFromDb(DownloadInfo di)
        {
            SQLiteDatabase sqldb = mHelper.ReadableDatabase;
            ICursor cur = null;
            try
            {
                cur = sqldb.Query(DownloadColumns.TABLE_NAME,
                                  DC_PROJECTION,
                                  DownloadColumns.FILENAME + "= ?",
                                  new[]
                                      {
                                          di.FileName
                                      }, null, null, null);
                if (null != cur && cur.MoveToFirst())
                {
                    setDownloadInfoFromCursor(di, cur);
                    return true;
                }
                return false;
            }
            finally
            {
                if (null != cur)
                {
                    cur.Close();
                }
            }
        }

        public void setDownloadInfoFromCursor(DownloadInfo di, ICursor cur)
        {
            di.Uri = cur.GetString(URI_IDX);
            di.ETag = cur.GetString(ETAG_IDX);
            di.TotalBytes = cur.GetLong(TOTALBYTES_IDX);
            di.CurrentBytes = cur.GetLong(CURRENTBYTES_IDX);
            di.LastModified = cur.GetLong(LASTMOD_IDX);
            di.Status = (DownloadStatus)cur.GetInt(STATUS_IDX);
            di.Control = cur.GetInt(CONTROL_IDX);
            di.FailedCount = cur.GetInt(NUM_FAILED_IDX);
            di.RetryAfter = cur.GetInt(RETRY_AFTER_IDX);
            di.RedirectCount = cur.GetInt(REDIRECT_COUNT_IDX);
        }

        public DownloadInfo getDownloadInfoFromCursor(ICursor cur)
        {
            var di = new DownloadInfo(cur.GetInt(INDEX_IDX), cur.GetString(FILENAME_IDX), GetType().Namespace);
            setDownloadInfoFromCursor(di, cur);
            return di;
        }

        public DownloadInfo[] GetDownloads()
        {
            SQLiteDatabase sqldb = mHelper.ReadableDatabase;
            ICursor cur = null;
            try
            {
                cur = sqldb.Query(DownloadColumns.TABLE_NAME, DC_PROJECTION, null, null, null, null, null);
                if (null != cur && cur.MoveToFirst())
                {
                    var retInfos = new DownloadInfo[cur.Count];
                    int idx = 0;
                    do
                    {
                        DownloadInfo di = getDownloadInfoFromCursor(cur);
                        retInfos[idx++] = di;
                    } while (cur.MoveToNext());
                    return retInfos;
                }
                return null;
            }
            finally
            {
                if (null != cur)
                {
                    cur.Close();
                }
            }
        }

        #region Nested type: BaseTables

        public abstract class BaseTables
        {
        }

        #endregion

        #region Nested type: DownloadColumns

        public class DownloadColumns : BaseTables
        {
            public static string INDEX = "FILEIDX";
            public static string URI = "URI";
            public static string FILENAME = "FN";
            public static string ETAG = "ETAG";

            public static string TOTALBYTES = "TOTALBYTES";
            public static string CURRENTBYTES = "CURRENTBYTES";
            public static string LASTMOD = "LASTMOD";

            public static string STATUS = "STATUS";
            public static string CONTROL = "CONTROL";
            public static string NUM_FAILED = "FAILCOUNT";
            public static string RETRY_AFTER = "RETRYAFTER";
            public static string REDIRECT_COUNT = "REDIRECTCOUNT";

            public static string[][] SCHEMA = new[]
                                                  {
                                                      new[] {BaseColumns.Id, "INTEGER PRIMARY KEY"},
                                                      new[] {INDEX, "INTEGER UNIQUE"},
                                                      new[] {URI, "TEXT"},
                                                      new[] {FILENAME, "TEXT UNIQUE"},
                                                      new[] {ETAG, "TEXT"},
                                                      new[] {TOTALBYTES, "INTEGER"},
                                                      new[] {CURRENTBYTES, "INTEGER"},
                                                      new[] {LASTMOD, "INTEGER"},
                                                      new[] {STATUS, "INTEGER"},
                                                      new[] {CONTROL, "INTEGER"},
                                                      new[] {NUM_FAILED, "INTEGER"},
                                                      new[] {RETRY_AFTER, "INTEGER"},
                                                      new[] {REDIRECT_COUNT, "INTEGER"}
                                                  };

            public static string TABLE_NAME = "DownloadColumns";
            public static string _ID = "DownloadColumns._id";
        }

        #endregion

        #region Nested type: DownloadsContentDBHelper

        protected class DownloadsContentDBHelper : SQLiteOpenHelper
        {
            public DownloadsContentDBHelper(Context paramContext)
                : base(paramContext, DATABASE_NAME, null, DATABASE_VERSION)
            {
            }

            private string createTableQueryFromArray(string paramString, string[][] paramArrayOfString)
            {
                var localStringBuilder = new System.Text.StringBuilder();
                localStringBuilder.Append("CREATE TABLE ");
                localStringBuilder.Append(paramString);
                localStringBuilder.Append(" (");
                int i = paramArrayOfString.Length;
                for (int j = 0;; j++)
                {
                    if (j >= i)
                    {
                        localStringBuilder.Length = localStringBuilder.Length - 1;
                        localStringBuilder.Append(");");
                        return localStringBuilder.ToString();
                    }
                    string[] arrayOfString = paramArrayOfString[j];
                    localStringBuilder.Append(' ');
                    localStringBuilder.Append(arrayOfString[0]); 
                    localStringBuilder.Append(' ');
                    localStringBuilder.Append(arrayOfString[1]);
                    localStringBuilder.Append(',');
                }
            }

            private void dropTables(SQLiteDatabase paramSQLiteDatabase)
            {
                Type[] arrayOfClass = typeof (DownloadsDB).GetNestedTypes();
                int i = arrayOfClass.Length;
                int j = 0;
                while (true)
                {
                    if (j >= i)
                        return;
                    Type localClass = arrayOfClass[j];
                    if (typeof (BaseTables).IsAssignableFrom(localClass))
                        try
                        {
                            j++;
                            var str = (string) localClass.GetField("TABLE_NAME").GetValue(null);
                            paramSQLiteDatabase.ExecSQL("DROP TABLE IF EXISTS " + str);
                        }
                        catch (Exception localException)
                        {
                            localException.PrintStackTrace();
                        }
                }
            }

            public override void OnCreate(SQLiteDatabase paramSQLiteDatabase)
            {
                Type[] arrayOfClass = typeof (DownloadsDB).GetNestedTypes();
                int numClasses = arrayOfClass.Length;
                for (int i = 0; i < numClasses; i++)
                {
                    Type localClass = arrayOfClass[i];
                    if (typeof (BaseTables).IsAssignableFrom(localClass) && localClass != typeof (BaseTables))
                    {
                        try
                        {
                            FieldInfo localField1 = localClass.GetField("SCHEMA");
                            FieldInfo localField2 = localClass.GetField("TABLE_NAME");
                            var arrayOfString = (string[][]) localField1.GetValue(null);
                            paramSQLiteDatabase.ExecSQL(createTableQueryFromArray((string) localField2.GetValue(null), arrayOfString));
                        }
                        catch (Exception localException)
                        {
                            while (true)
                                localException.PrintStackTrace();
                        }
                    }
                }
            }

            public override void OnUpgrade(SQLiteDatabase paramSQLiteDatabase, int paramInt1, int paramInt2)
            {
                Log.Warn(typeof (DownloadsContentDBHelper).Name,
                         "Upgrading database from version " + paramInt1 + " to " + paramInt2 + ", which will destroy all old data");
                dropTables(paramSQLiteDatabase);
                OnCreate(paramSQLiteDatabase);
            }
        }

        #endregion

        #region Nested type: MetadataColumns

        public class MetadataColumns : BaseTables
        {
            public static string APKVERSION = "APKVERSION";
            public static string DOWNLOAD_STATUS = "DOWNLOADSTATUS";
            public static string FLAGS = "DOWNLOADFLAGS";

            public static string[][] SCHEMA = new[]
                                                  {
                                                      new[] {BaseColumns.Id, "INTEGER PRIMARY KEY"},
                                                      new[] {APKVERSION, "INTEGER"},
                                                      new[] {DOWNLOAD_STATUS, "INTEGER"},
                                                      new[] {FLAGS, "INTEGER"}
                                                  };

            public static string TABLE_NAME = "MetadataColumns";
            public static string _ID = "MetadataColumns._id";
        }

        #endregion
    }
}