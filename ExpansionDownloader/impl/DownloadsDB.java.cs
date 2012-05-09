namespace ExpansionDownloader.impl
{
    using System;
    using System.Diagnostics;
    using System.Text;

    using Android.Content;
    using Android.Database;
    using Android.Database.Sqlite;
    using Android.Provider;

    using ExpansionDownloader.Service;

    using LicenseVerificationLibrary;

    /// <summary>
    /// The downloads database.
    /// </summary>
    public class DownloadsDB
    {
        #region Constants and Fields

        /// <summary>
        /// The database name.
        /// </summary>
        private const string DatabaseName = "DownloadsDatabase";

        /// <summary>
        /// The database version.
        /// </summary>
        private const int DatabaseVersion = 10;

        /// <summary>
        /// The projection.
        /// </summary>
        private static readonly string[] DownloadColumnsProjection = 
                                                                     {
                                                                         DownloadColumns.FileName, DownloadColumns.Uri,
                                                                         DownloadColumns.ETag, DownloadColumns.TotalBytes,
                                                                         DownloadColumns.CurrentBytes,
                                                                         DownloadColumns.LastModified,
                                                                         DownloadColumns.Status, DownloadColumns.Control,
                                                                         DownloadColumns.NumFailed,
                                                                         DownloadColumns.RetryAfter,
                                                                         DownloadColumns.RedirectCount,
                                                                         DownloadColumns.FileIndex
                                                                     };

        /// <summary>
        /// The locker.
        /// </summary>
        private static readonly object Locker = new object();

        /// <summary>
        /// The m helper.
        /// </summary>
        private readonly SQLiteOpenHelper openHelper;

        /// <summary>
        /// The _instance.
        /// </summary>
        private static DownloadsDB instance;

        /// <summary>
        /// The m get download by index.
        /// </summary>
        private SQLiteStatement sqlGetDownloadByIndex;

        /// <summary>
        /// The m metadata row id.
        /// </summary>
        private long metadataRowId;

        /// <summary>
        /// The m update current bytes.
        /// </summary>
        private SQLiteStatement sqlUpdateCurrentBytes;

        /// <summary>
        /// the curent status of the database
        /// </summary>
        private DownloadStatus downloadStatus;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadsDB"/> class.
        /// </summary>
        /// <param name="paramContext">
        /// The param context.
        /// </param>
        private DownloadsDB(Context paramContext)
        {
            this.metadataRowId = -1;
            this.downloadStatus = DownloadStatus.Unknown;
            this.VersionCode = -1;

            this.openHelper = new DownloadsContentDBHelper(paramContext);
            SQLiteDatabase sqldb = this.openHelper.ReadableDatabase;

            // Query for the version code, the row ID of the metadata (for future
            // updating) the status and the flags
            string query = string.Format(
                "SELECT {0}, {1}, {2}, {3} FROM {4} LIMIT 1", 
                MetadataColumns.ApkVersion, 
                BaseColumns.Id, 
                MetadataColumns.DownloadStatus, 
                MetadataColumns.Flags, 
                typeof(MetadataColumns).Name);
            ICursor cur = sqldb.RawQuery(query, null);

            if (cur != null && cur.MoveToFirst())
            {
                this.VersionCode = cur.GetInt(0);
                this.metadataRowId = cur.GetLong(1);
                this.DownloadStatus = (DownloadStatus)cur.GetInt(2);
                this.Flags = (DownloaderServiceFlags)cur.GetInt(3);
                cur.Close();
            }
        }

        #endregion

        #region Enums

        /// <summary>
        /// The column indexes.
        /// </summary>
        private enum ColumnIndexes
        {
            /// <summary>
            /// The filename.
            /// </summary>
            Filename = 0, 

            /// <summary>
            /// The uri.
            /// </summary>
            Uri = 1, 

            /// <summary>
            /// The e tag.
            /// </summary>
            ETag = 2, 

            /// <summary>
            /// The total bytes.
            /// </summary>
            TotalBytes = 3, 

            /// <summary>
            /// The current bytes.
            /// </summary>
            CurrentBytes = 4, 

            /// <summary>
            /// The last modified.
            /// </summary>
            LastModified = 5, 

            /// <summary>
            /// The status.
            /// </summary>
            Status = 6, 

            /// <summary>
            /// The control.
            /// </summary>
            Control = 7, 

            /// <summary>
            /// The num failed.
            /// </summary>
            NumFailed = 8, 

            /// <summary>
            /// The retry after.
            /// </summary>
            RetryAfter = 9, 

            /// <summary>
            /// The redirect count.
            /// </summary>
            RedirectCount = 10, 

            /// <summary>
            /// The index.
            /// </summary>
            Index = 11
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets Flags.
        /// </summary>
        public DownloaderServiceFlags Flags { get; private set; }
        
        /// <summary>
        /// Gets the status.
        /// </summary>
        public DownloadStatus DownloadStatus
        {
            get
            {
                return this.downloadStatus;
            }
            
            set
            {
                if (this.DownloadStatus != value)
                {
                    var cv = new ContentValues();
                    cv.Put(MetadataColumns.DownloadStatus, (int)value);
                    if (this.UpdateMetadata(cv))
                    {
                        this.downloadStatus = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsDownloadRequired.
        /// </summary>
        public bool IsDownloadRequired
        {
            get
            {
                SQLiteDatabase sqldb = this.openHelper.ReadableDatabase;
                string query = string.Format(
                    "SELECT Count(*) FROM {0} WHERE {1} <> 0", typeof(DownloadColumns).Name, DownloadColumns.Status);
                ICursor cur = sqldb.RawQuery(query, null);
                if (cur != null)
                {
                    try
                    {
                        if (cur.MoveToFirst())
                        {
                            return cur.GetInt(0) == 0;
                        }
                    }
                    finally
                    {
                        cur.Close();
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Gets LastCheckedVersionCode.
        /// </summary>
        public int LastCheckedVersionCode
        {
            get
            {
                return this.VersionCode;
            }
        }

        /// <summary>
        /// Gets VersionCode.
        /// </summary>
        public int VersionCode { get; private set; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets DownloadByIndexStatement.
        /// </summary>
        private SQLiteStatement DownloadByIndexStatement
        {
            get
            {
                string query = string.Format(
                    "SELECT {0} FROM {1} WHERE {2} = ?", 
                    BaseColumns.Id, 
                    typeof(DownloadColumns).Name, 
                    DownloadColumns.FileIndex);
                return this.sqlGetDownloadByIndex
                       ?? (this.sqlGetDownloadByIndex = this.openHelper.ReadableDatabase.CompileStatement(query));
            }
        }

        /// <summary>
        /// Gets UpdateCurrentBytesStatement.
        /// </summary>
        private SQLiteStatement UpdateCurrentBytesStatement
        {
            get
            {
                string query = string.Format(
                    "UPDATE {0} SET {1} = ? WHERE {2} = ?", 
                    typeof(DownloadColumns).Name, 
                    DownloadColumns.CurrentBytes, 
                    DownloadColumns.FileIndex);
                return this.sqlUpdateCurrentBytes
                       ?? (this.sqlUpdateCurrentBytes = this.openHelper.ReadableDatabase.CompileStatement(query));
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Returns a new database if there is none, or the existing database 
        /// if there is an existing one already.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The instance of the database.
        /// </returns>
        public static DownloadsDB GetDatabase(Context context)
        {
            lock (Locker)
            {
                return instance ?? (instance = new DownloadsDB(context));
            }
        }

        /// <summary>
        /// Close the current database.
        /// </summary>
        public void Close()
        {
            this.openHelper.Close();
        }

        /// <summary>
        /// Returns the download information for the given filename.
        /// </summary>
        /// <param name="fileName">
        /// The file name.
        /// </param>
        /// <returns>
        /// The download information for the filename
        /// </returns>
        public DownloadInfo GetDownloadInfo(string fileName)
        {
            SQLiteDatabase sqldb = this.openHelper.ReadableDatabase;
            ICursor itemcur = null;
            try
            {
                itemcur = sqldb.Query(
                    typeof(DownloadColumns).Name, 
                    DownloadColumnsProjection, 
                    DownloadColumns.FileName + " = ?", 
                    new[] { fileName }, 
                    null, 
                    null, 
                    null);
                if (null != itemcur && itemcur.MoveToFirst())
                {
                    return GetDownloadInfo(itemcur);
                }
            }
            finally
            {
                if (null != itemcur)
                {
                    itemcur.Close();
                }
            }

            return null;
        }

        /// <summary>
        /// The get downloads.
        /// </summary>
        /// <returns>
        /// </returns>
        public DownloadInfo[] GetDownloads()
        {
            SQLiteDatabase sqldb = this.openHelper.ReadableDatabase;
            ICursor cur = null;
            try
            {
                cur = sqldb.Query(typeof(DownloadColumns).Name, DownloadColumnsProjection, null, null, null, null, null);
                if (null != cur && cur.MoveToFirst())
                {
                    var retInfos = new DownloadInfo[cur.Count];
                    int idx = 0;
                    do
                    {
                        DownloadInfo di = GetDownloadInfo(cur);
                        retInfos[idx++] = di;
                    }
                    while (cur.MoveToNext());
                    return retInfos;
                }

                return new DownloadInfo[0];
            }
            finally
            {
                if (null != cur)
                {
                    cur.Close();
                }
            }
        }

        /// <summary>
        /// This function will add a new file to the database if it does not exist.
        /// </summary>
        /// <param name="di">
        /// DownloadInfo that we wish to store
        /// </param>
        /// <returns>
        /// the row id of the record to be updated/inserted, or -1
        /// </returns>
        public bool UpdateDownload(DownloadInfo di)
        {
            var cv = new ContentValues();

            cv.Put(DownloadColumns.FileIndex, (int)di.ExpansionFileType);
            cv.Put(DownloadColumns.FileName, di.FileName);
            cv.Put(DownloadColumns.Uri, di.Uri);
            cv.Put(DownloadColumns.ETag, di.ETag);
            cv.Put(DownloadColumns.TotalBytes, di.TotalBytes);
            cv.Put(DownloadColumns.CurrentBytes, di.CurrentBytes);
            cv.Put(DownloadColumns.LastModified, di.LastModified);
            cv.Put(DownloadColumns.Status, (int)di.Status);
            cv.Put(DownloadColumns.Control, di.Control);
            cv.Put(DownloadColumns.NumFailed, di.FailedCount);
            cv.Put(DownloadColumns.RetryAfter, di.RetryAfter);
            cv.Put(DownloadColumns.RedirectCount, di.RedirectCount);

            return this.UpdateDownload(di, cv);
        }

        /// <summary>
        /// The update download current bytes.
        /// </summary>
        /// <param name="di">
        /// The di.
        /// </param>
        public void UpdateDownloadCurrentBytes(DownloadInfo di)
        {
            SQLiteStatement downloadCurrentBytes = this.UpdateCurrentBytesStatement;
            downloadCurrentBytes.ClearBindings();
            downloadCurrentBytes.BindLong(1, di.CurrentBytes);
            downloadCurrentBytes.BindLong(2, (int)di.ExpansionFileType);
            downloadCurrentBytes.Execute();
        }

        /// <summary>
        /// The update flags.
        /// </summary>
        /// <param name="flags">
        /// The flags.
        /// </param>
        /// <returns>
        /// The update flags.
        /// </returns>
        public bool UpdateFlags(DownloaderServiceFlags flags)
        {
            if (this.Flags == flags)
            {
                return true;
            }

            var cv = new ContentValues();
            cv.Put(MetadataColumns.Flags, (int)flags);
            if (this.UpdateMetadata(cv))
            {
                this.Flags = flags;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The update from database.
        /// </summary>
        /// <param name="di">
        /// The di.
        /// </param>
        /// <returns>
        /// The update from database.
        /// </returns>
        public bool UpdateFromDatabase(DownloadInfo di)
        {
            SQLiteDatabase sqldb = this.openHelper.ReadableDatabase;
            ICursor cur = sqldb.Query(
                typeof(DownloadColumns).Name, 
                DownloadColumnsProjection, 
                DownloadColumns.FileName + "= ?", 
                new[] { di.FileName }, 
                null, 
                null, 
                null);
            if (cur != null)
            {
                try
                {
                    if (cur.MoveToFirst())
                    {
                        SetDownloadInfo(di, cur);
                        return true;
                    }
                }
                finally
                {
                    cur.Close();
                }
            }

            return false;
        }

        /// <summary>
        /// The update metadata.
        /// </summary>
        /// <param name="apkVersion">
        /// The apk version.
        /// </param>
        /// <param name="downloadStatus">
        /// The download status.
        /// </param>
        /// <returns>
        /// The update metadata.
        /// </returns>
        public bool UpdateMetadata(int apkVersion, DownloadStatus downloadStatus)
        {
            var cv = new ContentValues();
            cv.Put(MetadataColumns.ApkVersion, apkVersion);
            cv.Put(MetadataColumns.DownloadStatus, (int)downloadStatus);

            if (this.UpdateMetadata(cv))
            {
                this.VersionCode = apkVersion;
                this.DownloadStatus = downloadStatus;
                return true;
            }

            return false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The set download info.
        /// </summary>
        /// <param name="di">
        /// The di.
        /// </param>
        /// <param name="cur">
        /// The cur.
        /// </param>
        private static void SetDownloadInfo(DownloadInfo di, ICursor cur)
        {
            di.Uri = cur.GetString((int)ColumnIndexes.Uri);
            di.ETag = cur.GetString((int)ColumnIndexes.ETag);
            di.TotalBytes = cur.GetLong((int)ColumnIndexes.TotalBytes);
            di.CurrentBytes = cur.GetLong((int)ColumnIndexes.CurrentBytes);
            di.LastModified = cur.GetLong((int)ColumnIndexes.LastModified);
            di.Status = (DownloadStatus)cur.GetInt((int)ColumnIndexes.Status);
            di.Control = cur.GetInt((int)ColumnIndexes.Control);
            di.FailedCount = cur.GetInt((int)ColumnIndexes.NumFailed);
            di.RetryAfter = cur.GetInt((int)ColumnIndexes.RetryAfter);
            di.RedirectCount = cur.GetInt((int)ColumnIndexes.RedirectCount);
        }

        /// <summary>
        /// The get download info.
        /// </summary>
        /// <param name="cur">
        /// The cur.
        /// </param>
        /// <returns>
        /// </returns>
        private DownloadInfo GetDownloadInfo(ICursor cur)
        {
            var di = new DownloadInfo(
                (ApkExpansionPolicy.ExpansionFileType)cur.GetInt((int)ColumnIndexes.Index),
                cur.GetString((int)ColumnIndexes.Filename),
                this.GetType().Namespace);
            SetDownloadInfo(di, cur);
            return di;
        }

        /// <summary>
        /// The get id.
        /// </summary>
        /// <param name="di">
        /// The di.
        /// </param>
        /// <returns>
        /// The get id.
        /// </returns>
        private long GetId(DownloadInfo di)
        {
            return this.GetId(di.ExpansionFileType);
        }

        /// <summary>
        /// The get id for a expansion file.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The id.
        /// </returns>
        private long GetId(ApkExpansionPolicy.ExpansionFileType index)
        {
            SQLiteStatement downloadByIndex = this.DownloadByIndexStatement;
            downloadByIndex.ClearBindings();
            downloadByIndex.BindLong(1, (int)index);
            try
            {
                return downloadByIndex.SimpleQueryForLong();
            }
            catch (SQLiteDoneException)
            {
                return -1;
            }
        }

        /// <summary>
        /// The update download.
        /// </summary>
        /// <param name="di">
        /// The download info.
        /// </param>
        /// <param name="cv">
        /// The values.
        /// </param>
        /// <returns>
        /// True if the update was successful
        /// </returns>
        private bool UpdateDownload(DownloadInfo di, ContentValues cv)
        {
            long id = di == null ? -1 : this.GetId(di);

            try
            {
                SQLiteDatabase sqldb = this.openHelper.WritableDatabase;
                if (id != -1)
                {
                    if (1 != sqldb.Update(typeof(DownloadColumns).Name, cv, DownloadColumns._Id + " = " + id, null))
                    {
                        return false;
                    }
                }
                else
                {
                    return -1 != sqldb.Insert(typeof(DownloadColumns).Name, DownloadColumns.Uri, cv);
                }
            }
            catch (SQLiteException ex)
            {
                ex.PrintStackTrace();
            }

            return false;
        }

        /// <summary>
        /// The update metadata.
        /// </summary>
        /// <param name="cv">
        /// The values.
        /// </param>
        /// <returns>
        /// True if the update was successful
        /// </returns>
        private bool UpdateMetadata(ContentValues cv)
        {
            SQLiteDatabase sqldb = this.openHelper.WritableDatabase;
            if (this.metadataRowId == -1)
            {
                long newId = sqldb.Insert(typeof(MetadataColumns).Name, MetadataColumns.ApkVersion, cv);
                if (newId == -1)
                {
                    return false;
                }

                this.metadataRowId = newId;
            }
            else
            {
                var whereClause = string.Format("{0} = {1}", BaseColumns.Id, this.metadataRowId);
                if (sqldb.Update(typeof(MetadataColumns).Name, cv, whereClause, null) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        /// <summary>
        /// The base tables.
        /// </summary>
        public abstract class BaseTables
        {
        }

        /// <summary>
        /// The download columns.
        /// </summary>
        public class DownloadColumns : BaseTables
        {
            #region Constants and Fields

            /// <summary>
            /// The control.
            /// </summary>
            public static string Control = "CONTROL";

            /// <summary>
            /// The current bytes.
            /// </summary>
            public static string CurrentBytes = "CURRENTBYTES";

            /// <summary>
            /// The e tag.
            /// </summary>
            public static string ETag = "ETAG";

            /// <summary>
            /// The file index.
            /// </summary>
            public static string FileIndex = "FILEIDX";

            /// <summary>
            /// The file name.
            /// </summary>
            public static string FileName = "FN";

            /// <summary>
            /// The last modified.
            /// </summary>
            public static string LastModified = "LASTMOD";

            /// <summary>
            /// The num failed.
            /// </summary>
            public static string NumFailed = "FAILCOUNT";

            /// <summary>
            /// The redirect count.
            /// </summary>
            public static string RedirectCount = "REDIRECTCOUNT";

            /// <summary>
            /// The retry after.
            /// </summary>
            public static string RetryAfter = "RETRYAFTER";


            /// <summary>
            /// The status.
            /// </summary>
            public static string Status = "STATUS";

            /// <summary>
            /// The total bytes.
            /// </summary>
            public static string TotalBytes = "TOTALBYTES";

            /// <summary>
            /// The uri.
            /// </summary>
            public static string Uri = "URI";

            /// <summary>
            /// The _ id.
            /// </summary>
            public static string _Id = "DownloadColumns._id";

            /// <summary>
            /// The schema.
            /// </summary>
            public static string[][] Schema = new[]
                                                  {
                                                      new[] {BaseColumns.Id, "INTEGER PRIMARY KEY"},
                                                      new[] {FileIndex, "INTEGER UNIQUE"},
                                                      new[] {Uri, "TEXT"}, 
                                                      new[] {FileName, "TEXT UNIQUE"},
                                                      new[] {ETag, "TEXT"},
                                                      new[] {TotalBytes, "INTEGER"}, 
                                                      new[] {CurrentBytes, "INTEGER"},
                                                      new[] {LastModified, "INTEGER"},
                                                      new[] {Status, "INTEGER"}, 
                                                      new[] {Control, "INTEGER"},
                                                      new[] {NumFailed, "INTEGER"},
                                                      new[] {RetryAfter, "INTEGER"}, 
                                                      new[] {RedirectCount, "INTEGER"}
                                                  };
            #endregion
        }

        /// <summary>
        /// The metadata columns.
        /// </summary>
        public class MetadataColumns : BaseTables
        {
            #region Constants and Fields

            /// <summary>
            /// The apk version.
            /// </summary>
            public static string ApkVersion = "APKVERSION";

            /// <summary>
            /// The download status.
            /// </summary>
            public static string DownloadStatus = "DOWNLOADSTATUS";

            /// <summary>
            /// The flags.
            /// </summary>
            public static string Flags = "DOWNLOADFLAGS";

            /// <summary>
            /// The _ id.
            /// </summary>
            public static string _Id = "MetadataColumns._id";

            /// <summary>
            /// The schema.
            /// </summary>
            public static string[][] Schema = new[]
                                                  {
                                                      new[] {BaseColumns.Id, "INTEGER PRIMARY KEY"},
                                                      new[] {ApkVersion, "INTEGER"},
                                                      new[] {DownloadStatus, "INTEGER"},
                                                      new[] {Flags, "INTEGER"}
                                                  };

            #endregion
        }

        /// <summary>
        /// The downloads content db helper.
        /// </summary>
        private class DownloadsContentDBHelper : SQLiteOpenHelper
        {
            #region Constants and Fields

            /// <summary>
            /// The tables.
            /// </summary>
            private static readonly Type[] Tables = new[] { typeof(MetadataColumns), typeof(DownloadColumns) };

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="DownloadsContentDBHelper"/> class.
            /// </summary>
            /// <param name="paramContext">
            /// The param context.
            /// </param>
            public DownloadsContentDBHelper(Context paramContext)
                : base(paramContext, DatabaseName, null, DatabaseVersion)
            {
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The on create.
            /// </summary>
            /// <param name="database">
            /// The database.
            /// </param>
            public override void OnCreate(SQLiteDatabase database)
            {
                Debug.WriteLine("Creating downloads database...");

                foreach (var table in Tables)
                {
                    Debug.WriteLine("Creating downloads table " + table.Name);
                    try
                    {
                        database.ExecSQL(CreateTableQuery(table));
                    }
                    catch (Exception localException)
                    {
                        Debug.WriteLine(localException);
                    }
                }
            }

            /// <summary>
            /// The on upgrade.
            /// </summary>
            /// <param name="database">
            /// The database.
            /// </param>
            /// <param name="oldVersion">
            /// The old version.
            /// </param>
            /// <param name="newVersion">
            /// The new version.
            /// </param>
            public override void OnUpgrade(SQLiteDatabase database, int oldVersion, int newVersion)
            {
                Debug.WriteLine(
                    "Upgrading database from version {0} to {1}, which will destroy all old data", 
                    oldVersion, 
                    newVersion);

                DropTables(database);
                this.OnCreate(database);
            }

            #endregion

            #region Methods

            /// <summary>
            /// The create table query.
            /// </summary>
            /// <param name="table">
            /// The table.
            /// </param>
            /// <returns>
            /// create table query.
            /// </returns>
            private static string CreateTableQuery(Type table)
            {
                var tableName = table.Name;
                var columns = (string[][]) table.GetField("Schema").GetValue(null);

                var localStringBuilder = new StringBuilder();
                localStringBuilder.Append("CREATE TABLE ");
                localStringBuilder.Append(tableName);
                localStringBuilder.Append(" (");

                foreach (var column in columns)
                {
                    localStringBuilder.Append(' ');
                    localStringBuilder.Append(column[0]);
                    localStringBuilder.Append(' ');
                    localStringBuilder.Append(column[1]);
                    localStringBuilder.Append(',');
                }

                localStringBuilder.Length = localStringBuilder.Length - 1;
                localStringBuilder.Append(");");

                return localStringBuilder.ToString();
            }

            /// <summary>
            /// The drop tables.
            /// </summary>
            /// <param name="database">
            /// The database.
            /// </param>
            private static void DropTables(SQLiteDatabase database)
            {
                foreach (var table in Tables)
                {
                    try
                    {
                        database.ExecSQL("DROP TABLE IF EXISTS " + table.Name);
                    }
                    catch (Exception localException)
                    {
                        Debug.WriteLine(localException);
                    }
                }
            }

            #endregion
        }
    }
}
