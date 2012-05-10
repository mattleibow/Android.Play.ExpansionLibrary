namespace ExpansionDownloader.impl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.Serialization;

    using Android.Database.Sqlite;

    using ExpansionDownloader.Service;

    /// <summary>
    /// The downloads database.
    /// </summary>
    public class DownloadsDatabase
    {
        #region Constants and Fields

        /// <summary>
        /// The locker.
        /// </summary>
        private static readonly object Locker = new object();

        /// <summary>
        /// The app path.
        /// </summary>
        private static string AppPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        /// <summary>
        /// The database path.
        /// </summary>
        private static string DatabasePath = Path.Combine(AppPath, "DownloadDatabase");

        /// <summary>
        /// The _instance.
        /// </summary>
        private static DownloadsDatabase instance;

        /// <summary>
        /// the curent status of the database
        /// </summary>
        private DownloadStatus downloadStatus;

        /// <summary>
        /// The flags.
        /// </summary>
        private DownloaderServiceFlags flags;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Prevents a default instance of the <see cref="DownloadsDatabase"/> class from being created.
        /// </summary>
        private DownloadsDatabase()
        {
            this.downloadStatus = DownloadStatus.Unknown;
            this.VersionCode = -1;

            if (!Directory.Exists(DatabasePath))
            {
                Directory.CreateDirectory(DatabasePath);
            }

            var metadata = GetData<MetadataTable>();
            if (metadata != null)
            {
                this.VersionCode = metadata.ApkVersion;
                this.DownloadStatus = metadata.DownloadStatus;
                this.Flags = metadata.Flags;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns a new database if there is none, or the existing database 
        /// if there is an existing one already.
        /// </summary>
        /// <returns>
        /// The instance of the database.
        /// </returns>
        public static DownloadsDatabase Instance
        {
            get
            {
                lock (Locker)
                {
                    return instance ?? (instance = new DownloadsDatabase());
                }
            }
        }

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
                    var metadata = GetMetadata();
                    metadata.DownloadStatus = value;
                    SaveData(metadata);

                    this.downloadStatus = value;
                }
            }
        }

        /// <summary>
        /// Gets Flags.
        /// </summary>
        public DownloaderServiceFlags Flags
        {
            get
            {
                return this.flags;
            }

            set
            {
                if (this.flags != value)
                {
                    var metadata = GetMetadata();
                    metadata.Flags = value;
                    SaveData(metadata);

                    this.flags = value;
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
                var downloadInfos = this.GetDownloads();
                return !downloadInfos.Any() || downloadInfos.Any(x => x.Status != DownloadStatus.None);
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

        #region Public Methods and Operators


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
            return this.GetDownloads().FirstOrDefault(x => x.FileName == fileName);
        }

        /// <summary>
        /// The get downloads.
        /// </summary>
        /// <returns>
        /// </returns>
        public List<DownloadInfo> GetDownloads()
        {
            return GetData<List<DownloadInfo>>(typeof(DownloadInfo).Name) ?? new List<DownloadInfo>();
        }

        /// <summary>
        /// This function will add a new file to the database if it does not exist.
        /// </summary>
        /// <param name="instance">
        /// DownloadInfo that we wish to store
        /// </param>
        public void UpdateDownload(DownloadInfo instance)
        {
            List<DownloadInfo> downloads = this.GetDownloads();

            var downloadInfo = downloads.FirstOrDefault(d => d.FileName == instance.FileName);
            if (downloadInfo != null)
            {
                downloads.Remove(downloadInfo);
            }

            downloads.Add(instance);

            SaveData(downloads, typeof(DownloadInfo).Name);
        }

        /// <summary>
        /// The update download current bytes.
        /// </summary>
        /// <param name="di">
        /// The di.
        /// </param>
        public void UpdateDownloadCurrentBytes(DownloadInfo di)
        {
            var info = this.GetDownloads().First(x => x.ExpansionFileType == di.ExpansionFileType);
            info.CurrentBytes = di.CurrentBytes;
            this.UpdateDownload(info);
        }

        /// <summary>
        /// The update from database.
        /// </summary>
        /// <param name="info">
        /// The info.
        /// </param>
        public void UpdateFromDatabase(ref DownloadInfo info)
        {
            var downloadInfo = info;
            info = this.GetDownloads().First(x => x.FileName == downloadInfo.FileName);
        }

        /// <summary>
        /// The update metadata.
        /// </summary>
        /// <param name="apkVersion">
        /// The apk version.
        /// </param>
        /// <param name="status">
        /// The download status.
        /// </param>
        public void UpdateMetadata(int apkVersion, DownloadStatus status)
        {
            var metadata = GetMetadata();
            metadata.ApkVersion = apkVersion;
            metadata.DownloadStatus = status;
            SaveData(metadata);
        }

        #endregion

        #region Methods

        /// <summary>
        /// The get data.
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        private static T GetData<T>() where T : class
        {
            return GetData<T>(typeof(T).Name);
        }

        /// <summary>
        /// The get data.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        private static T GetData<T>(string filename) where T : class
        {
            var dataPath = GetDataPath(filename);

            if (File.Exists(dataPath))
            {
                var document = XDocument.Load(dataPath);
                using (var reader = document.Root.CreateReader())
                {
                    var serializer = new XmlSerializer(typeof(T));
                    return serializer.Deserialize(reader) as T;
                }
            }

            return null;
        }

        /// <summary>
        /// The get data path.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <returns>
        /// The get data path.
        /// </returns>
        private static string GetDataPath(string filename)
        {
            var dataPath = Path.Combine(DatabasePath, filename + ".xml");

            return dataPath;
        }

        /// <summary>
        /// The get metadata.
        /// </summary>
        /// <returns>
        /// </returns>
        private static MetadataTable GetMetadata()
        {
            return GetData<MetadataTable>() ?? new MetadataTable();
        }

        /// <summary>
        /// The save data.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        private static void SaveData<T>(T data)
        {
            SaveData(data, typeof(T).Name);
        }

        /// <summary>
        /// The save data.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        private static void SaveData<T>(T data, string filename)
        {
            var type = typeof(T);
            using (var writer = new StreamWriter(GetDataPath(filename)))
            {
                var serializer = new XmlSerializer(type);
                serializer.Serialize(writer, data);
            }
        }

        #endregion
    }
}