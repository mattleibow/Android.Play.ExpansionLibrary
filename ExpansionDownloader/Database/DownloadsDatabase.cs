// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DownloadsDatabase.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The downloads database.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExpansionDownloader.Database
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;

    using ExpansionDownloader.Service;

    /// <summary>
    /// The downloads database.
    /// </summary>
    public static class DownloadsDatabase
    {
        #region Static Fields

        /// <summary>
        /// </summary>
        private static volatile ExpansionDownloadStatus downloadStatus;

        /// <summary>
        /// </summary>
        private static volatile ServiceFlags flags;

        /// <summary>
        /// </summary>
        private static volatile int versionCode;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes static members of the <see cref="DownloadsDatabase"/> class.
        /// </summary>
        static DownloadsDatabase()
        {
            downloadStatus = ExpansionDownloadStatus.Unknown;
            flags = 0;
            versionCode = -1;

            if (File.Exists(XmlDatastore.GetDataPath<MetadataTable>()))
            {
                downloadStatus = XmlDatastore.GetData<MetadataTable>().DownloadStatus;
                flags = XmlDatastore.GetData<MetadataTable>().Flags;
                versionCode = XmlDatastore.GetData<MetadataTable>().ApkVersion;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the status.
        /// </summary>
        public static ExpansionDownloadStatus DownloadStatus
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
                return !downloadInfos.Any() || downloadInfos.Any(x => x.Status != ExpansionDownloadStatus.None);
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

        /// <summary>
        /// </summary>
        /// <returns>
        /// The System.Collections.Generic.List`1[T -&gt; ExpansionDownloader.Service.DownloadInfo].
        /// </returns>
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

            DownloadInfo downloadInfo = downloads.FirstOrDefault(d => d.FileName == info.FileName);
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
            DownloadInfo info =
                XmlDatastore.GetData<List<DownloadInfo>>().First(x => x.ExpansionFileType == di.ExpansionFileType);
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
            DownloadInfo i = info;
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
        public static void UpdateMetadata(int apkVersion, ExpansionDownloadStatus status)
        {
            var metadata = XmlDatastore.GetData<MetadataTable>();
            metadata.ApkVersion = apkVersion;
            metadata.DownloadStatus = status;
            XmlDatastore.SaveData(metadata);

            versionCode = apkVersion;
            downloadStatus = status;
        }

        #endregion

        /// <summary>
        /// </summary>
        private static class XmlDatastore
        {
            #region Static Fields

            /// <summary>
            /// The app path.
            /// </summary>
            private static readonly string AppPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            /// <summary>
            /// The database path.
            /// </summary>
            private static readonly string DatabasePath = Path.Combine(AppPath, "DownloadDatabase");

            #endregion

            #region Methods

            /// <summary>
            /// The get data.
            /// </summary>
            /// <typeparam name="T">
            /// </typeparam>
            /// <returns>
            /// The T.
            /// </returns>
            internal static T GetData<T>() where T : class, new()
            {
                T data;

                string dataPath = GetDataPath<T>();
                if (File.Exists(dataPath))
                {
                    // read the file
                    using (FileStream reader = File.OpenRead(dataPath))
                    {
                        var serializer = new BinaryFormatter();
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

            /// <summary>
            /// </summary>
            /// <typeparam name="T">
            /// </typeparam>
            /// <returns>
            /// The System.String.
            /// </returns>
            internal static string GetDataPath<T>()
            {
                Type paramType = typeof(T);

                // is it a list of types
                if (typeof(IEnumerable).IsAssignableFrom(paramType))
                {
                    Type[] genericArguments = paramType.GetGenericArguments();
                    if (genericArguments.Any())
                    {
                        paramType = genericArguments[0];
                    }
                }

                // get the filename
                string filename = paramType.Name;
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
                using (var writer = new FileStream(GetDataPath<T>(), FileMode.Create))
                {
                    var serializer = new BinaryFormatter();
                    serializer.Serialize(writer, data);
                }
            }

            #endregion
        }
    }
}