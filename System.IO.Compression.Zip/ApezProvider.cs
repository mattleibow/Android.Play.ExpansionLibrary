namespace System.IO.Compression.Zip
{
    using System.Collections.Generic;
    using System.Linq;

    using Android.Content;
    using Android.Content.PM;
    using Android.Content.Res;
    using Android.Database;
    using Android.Net;
    using Android.OS;
    using Android.Provider;

    using Java.IO;

    using FileNotFoundException = System.IO.FileNotFoundException;
    using IOException = System.IO.IOException;

    /// <summary>
    /// This defines a ContentProvider that marshalls the data from the ZIP 
    /// files through a content provider Uri in order to provide file access 
    /// for certain Android APIs that expect Uri access to media files.
    /// </summary>
    public abstract class ApezProvider : ContentProvider
    {
        #region Fields

        private ExpansionZipFile apkExtensionFile;

        private bool initialized;

        private static readonly Dictionary<string, ApezProjection> fieldNameValueMap = new Dictionary<string, ApezProjection>
            {
                { ApezContentFields.File, ApezProjection.File },
                { ApezContentFields.FileName, ApezProjection.FileName },
                { ApezContentFields.ZipFile, ApezProjection.ZipFile },
                { ApezContentFields.Modification, ApezProjection.Modification },
                { ApezContentFields.Crc, ApezProjection.Crc },
                { ApezContentFields.CompressedLength, ApezProjection.CompressedLength },
                { ApezContentFields.UncompressedLength, ApezProjection.UncompressedLength },
                { ApezContentFields.CompressionType, ApezProjection.CompressionType }
            };

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the Authority for this content provider.
        /// </summary>
        /// <remarks>
        /// To implement APEZProvider in your application, you'll want to 
        /// change the Authority to match what you define in the manifest.
        /// </remarks>
        protected abstract string Authority { get; }

        #endregion

        #region Public Methods and Operators

        public override ContentProviderResult[] ApplyBatch(IList<ContentProviderOperation> operations)
        {
            this.InitIfNecessary();
            return base.ApplyBatch(operations);
        }

        public override int Delete(Uri uri, string selection, string[] args)
        {
            return 0;
        }

        public override string GetType(Uri uri)
        {
            return "vnd.Android.cursor.item/asset";
        }

        public override Uri Insert(Uri uri, ContentValues values)
        {
            return null;
        }

        public override bool OnCreate()
        {
            return true;
        }

        public override AssetFileDescriptor OpenAssetFile(Uri uri, string mode)
        {
            this.InitIfNecessary();
            string path = uri.EncodedPath;
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            ZipFileEntry entry = this.apkExtensionFile.GetEntry(path);

            if (entry != null && entry.Method == Compression.Store)
            {
                try
                {
                    ParcelFileDescriptor pfd = ParcelFileDescriptor.Open(
                        new File(entry.ZipFileName), ParcelFileMode.ReadOnly);

                    return new AssetFileDescriptor(pfd, entry.FileOffset, entry.FileSize);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    Console.Write(e.StackTrace);
                }
            }

            return null;
        }

        public override ParcelFileDescriptor OpenFile(Uri uri, string mode)
        {
            this.InitIfNecessary();
            AssetFileDescriptor af = this.OpenAssetFile(uri, mode);
            if (null != af)
            {
                return af.ParcelFileDescriptor;
            }

            return null;
        }

        public override ICursor Query(Uri uri, string[] projection, string selection, string[] selArgs, string sort)
        {
            this.InitIfNecessary();

            // lists all of the items in the file that match
            var zipEntries = this.apkExtensionFile == null ? new ZipFileEntry[0] : this.apkExtensionFile.GetAllEntries();

            return MatrixCursor(projection, ApezProjections(ref projection), zipEntries);
        }

        public override int Update(Uri uri, ContentValues values, string selection, string[] selectionArgs)
        {
            return 0;
        }

        #endregion

        #region Methods

        private void InitIfNecessary()
        {
            if (!this.initialized)
            {
                Context ctx = this.Context;
                PackageManager pm = ctx.PackageManager;
                ProviderInfo pi = pm.ResolveContentProvider(this.Authority, PackageInfoFlags.MetaData);
                PackageInfo packInfo = null;

                try
                {
                    packInfo = pm.GetPackageInfo(ctx.PackageName, 0);
                }
                catch (PackageManager.NameNotFoundException e1)
                {
                    Console.WriteLine(e1.ToString());
                    Console.Write(e1.StackTrace);
                }

                if (packInfo != null)
                {
                    int patchFileVersion;
                    int mainFileVersion;
                    int appVersionCode = packInfo.VersionCode;
                    if (null != pi.MetaData)
                    {
                        mainFileVersion = pi.MetaData.GetInt("mainVersion", appVersionCode);
                        patchFileVersion = pi.MetaData.GetInt("patchVersion", appVersionCode);
                    }
                    else
                    {
                        mainFileVersion = patchFileVersion = appVersionCode;
                    }

                    try
                    {
                        this.apkExtensionFile = ApkExpansionSupport.GetApkExpansionZipFile(
                            ctx, mainFileVersion, patchFileVersion);

                        this.initialized = true;
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine(e.ToString());
                        Console.Write(e.StackTrace);
                    }
                }
            }
        }

        private static ApezProjection[] ApezProjections(ref string[] projection)
        {
            if (projection == null)
            {
                projection = fieldNameValueMap.Keys.ToArray();
            }

            return projection.Select(p => fieldNameValueMap[p]).ToArray();
        }

        private static MatrixCursor MatrixCursor(string[] projection, ApezProjection[] intProjection, ZipFileEntry[] entries)
        {
            var mc = new MatrixCursor(projection, entries.Length);
            foreach (var zer in entries)
            {
                MatrixCursor.RowBuilder rb = mc.NewRow();
                for (int i = 0; i < intProjection.Length; i++)
                {
                    switch (intProjection[i])
                    {
                        case ApezProjection.File:
                            rb.Add(i);
                            break;
                        case ApezProjection.FileName:
                            rb.Add(zer.FilenameInZip);
                            break;
                        case ApezProjection.ZipFile:
                            rb.Add(zer.ZipFileName);
                            break;
                        case ApezProjection.Modification:
                            rb.Add(ZipFile.DateTimeToDosTime(zer.ModifyTime));
                            break;
                        case ApezProjection.Crc:
                            rb.Add(zer.Crc32);
                            break;
                        case ApezProjection.CompressedLength:
                            rb.Add(zer.CompressedSize);
                            break;
                        case ApezProjection.UncompressedLength:
                            rb.Add(zer.FileSize);
                            break;
                        case ApezProjection.CompressionType:
                            rb.Add((int)zer.Method);
                            break;
                    }
                }
            }

            return mc;
        }

        #endregion

        private enum ApezProjection
        {
            File = 0,
            FileName = 1,
            ZipFile = 2,
            Modification = 3,
            Crc = 4,
            CompressedLength = 5,
            UncompressedLength = 6,
            CompressionType = 7
        }

        private static class ApezContentFields
        {
            public const string File = BaseColumns.Id;
            public const string FileName = "ZPFN";
            public const string ZipFile = "ZFIL";
            public const string Modification = "ZMOD";
            public const string Crc = "ZCRC";
            public const string CompressedLength = "ZCOL";
            public const string UncompressedLength = "ZUNL";
            public const string CompressionType = "ZTYP";
        }
    }
}