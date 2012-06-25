namespace ExpansionDownloader.Database
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.Serialization;

    using ExpansionDownloader.Service;

    /// <summary>
    /// The downloads database.
    /// </summary>
    public static class DownloadsDatabase
    {
        private volatile static DownloadStatus downloadStatus;
        private volatile static ServiceFlags flags;
        private volatile static int versionCode;

        static DownloadsDatabase()
        {
            downloadStatus = DownloadStatus.Unknown;
            flags = 0;
            versionCode = -1;

            if (File.Exists(XmlDatastore.GetDataPath<MetadataTable>()))
            {
                downloadStatus = XmlDatastore.GetData<MetadataTable>().DownloadStatus;
                flags = XmlDatastore.GetData<MetadataTable>().Flags;
                versionCode = XmlDatastore.GetData<MetadataTable>().ApkVersion;
            }
        }

        #region Public Properties

        /// <summary>
        /// Gets the status.
        /// </summary>
        public static DownloadStatus DownloadStatus
        {
            get
            {
                return downloadStatus;
            }

            set
            {
                if (downloadStatus != value)
                {
                    downloadStatus = value;

                    var metadata = XmlDatastore.GetData<MetadataTable>();
                    metadata.DownloadStatus = value;
                    XmlDatastore.SaveData(metadata);
                }
            }
        }

        /// <summary>
        /// Gets Flags.
        /// </summary>
        public static ServiceFlags Flags
        {
            get
            {
                return flags;
            }

            set
            {
                if (flags != value)
                {
                    flags = value;

                    var metadata = XmlDatastore.GetData<MetadataTable>();
                    metadata.Flags = value;
                    XmlDatastore.SaveData(metadata);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether IsDownloadRequired.
        /// </summary>
        public static bool IsDownloadRequired
        {
            get
            {
                var downloadInfos = XmlDatastore.GetData<List<DownloadInfo>>();
                return !downloadInfos.Any() || downloadInfos.Any(x => x.Status != DownloadStatus.None);
            }
        }

        /// <summary>
        /// Gets VersionCode.
        /// </summary>
        public static int VersionCode
        {
            get
            {
                return versionCode;
            }
        }

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
        public static DownloadInfo GetDownloadInfo(string fileName)
        {
            return XmlDatastore.GetData<List<DownloadInfo>>().FirstOrDefault(x => x.FileName == fileName);
        }

        public static List<DownloadInfo> GetDownloads()
        {
            return XmlDatastore.GetData<List<DownloadInfo>>();
        }

        /// <summary>
        /// This function will add a new file to the database if it does not exist.
        /// </summary>
        /// <param name="info">
        /// DownloadInfo that we wish to store
        /// </param>
        public static void UpdateDownload(DownloadInfo info)
        {
            var downloads = XmlDatastore.GetData<List<DownloadInfo>>();

            var downloadInfo = downloads.FirstOrDefault(d => d.FileName == info.FileName);
            if (downloadInfo != null)
            {
                downloads.Remove(downloadInfo);
            }

            downloads.Add(info);

            XmlDatastore.SaveData(downloads);
        }

        /// <summary>
        /// The update download current bytes.
        /// </summary>
        /// <param name="di">
        /// The di.
        /// </param>
        public static void UpdateDownloadCurrentBytes(DownloadInfo di)
        {
            var info = XmlDatastore.GetData<List<DownloadInfo>>().First(x => x.ExpansionFileType == di.ExpansionFileType);
            info.CurrentBytes = di.CurrentBytes;
            UpdateDownload(info);
        }

        /// <summary>
        /// The update from database.
        /// </summary>
        /// <param name="info">
        /// The info.
        /// </param>
        public static void UpdateFromDatabase(ref DownloadInfo info)
        {
            var i = info;
            info = XmlDatastore.GetData<List<DownloadInfo>>().First(x => x.FileName == i.FileName);
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
        public static void UpdateMetadata(int apkVersion, DownloadStatus status)
        {
            var metadata = XmlDatastore.GetData<MetadataTable>();
            metadata.ApkVersion = apkVersion;
            metadata.DownloadStatus = status;
            XmlDatastore.SaveData(metadata);

            versionCode = apkVersion;
            downloadStatus = status;
        }

        #endregion

        private static class XmlDatastore
        {
            /// <summary>
            /// The app path.
            /// </summary>
            private static readonly string AppPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            /// <summary>
            /// The database path.
            /// </summary>
            private static readonly string DatabasePath = Path.Combine(AppPath, "DownloadDatabase");

            /// <summary>
            /// The get data.
            /// </summary>
            /// <typeparam name="T">
            /// </typeparam>
            /// <returns>
            /// </returns>
            internal static T GetData<T>() where T : class, new()
            {
                T data;

                string dataPath = GetDataPath<T>();
                if (File.Exists(dataPath))
                {
                    // read the file
                    var document = XDocument.Load(dataPath);
                    using (var reader = document.Root.CreateReader())
                    {
                        var serializer = new XmlSerializer(typeof(T));
                        data = serializer.Deserialize(reader) as T;
                    }
                }
                else
                {
                    if (!Directory.Exists(DatabasePath))
                    {
                        Directory.CreateDirectory(DatabasePath);
                    }

                    // create the file
                    data = new T();
                    SaveData(data);
                }

                // return the loaded/new type
                return data;
            }

            internal static string GetDataPath<T>()
            {
                var paramType = typeof(T);

                // is it a list of types
                if (typeof(IEnumerable).IsAssignableFrom(paramType))
                {
                    var genericArguments = paramType.GetType().GetGenericArguments();
                    if (genericArguments.Any())
                    {
                        paramType = genericArguments[0];
                    }
                }

                // get the filename
                var filename = paramType.Name;
                return Path.Combine(DatabasePath, filename + ".xml");
            }

            /// <summary>
            /// The save data.
            /// </summary>
            /// <param name="data">
            /// The data.
            /// </param>
            /// <typeparam name="T">
            /// </typeparam>
            internal static void SaveData<T>(T data)
            {
                var type = typeof(T);
                using (var writer = new StreamWriter(GetDataPath<T>()))
                {
                    var serializer = new XmlSerializer(type);
                    serializer.Serialize(writer, data);
                }
            }

        }
    }
}
