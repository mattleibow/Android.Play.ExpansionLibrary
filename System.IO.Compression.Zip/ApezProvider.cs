// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApezProvider.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   This defines a ContentProvider that marshalls the data from the ZIP
//   files through a content provider Uri in order to provide file access
//   for certain Android APIs that expect Uri access to media files.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

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
        #region Static Fields

        /// <summary>
        /// The field name value map.
        /// </summary>
        private static readonly Dictionary<string, ApezProjection> FieldNameValueMap =
            new Dictionary<string, ApezProjection>
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

        #region Fields

        /// <summary>
        /// The apk extension file.
        /// </summary>
        private ExpansionZipFile apkExtensionFile;

        /// <summary>
        /// The initialized.
        /// </summary>
        private bool initialized;

        #endregion

        #region Enums

        /// <summary>
        /// The apez projection.
        /// </summary>
        private enum ApezProjection
        {
            /// <summary>
            /// The file.
            /// </summary>
            File = 0, 

            /// <summary>
            /// The file name.
            /// </summary>
            FileName = 1, 

            /// <summary>
            /// The zip file.
            /// </summary>
            ZipFile = 2, 

            /// <summary>
            /// The modification.
            /// </summary>
            Modification = 3, 

            /// <summary>
            /// The crc.
            /// </summary>
            Crc = 4, 

            /// <summary>
            /// The compressed length.
            /// </summary>
            CompressedLength = 5, 

            /// <summary>
            /// The uncompressed length.
            /// </summary>
            UncompressedLength = 6, 

            /// <summary>
            /// The compression type.
            /// </summary>
            CompressionType = 7
        }

        #endregion

        #region Properties

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

        /// <summary>
        /// The apply batch.
        /// </summary>
        /// <param name="operations">
        /// The operations.
        /// </param>
        /// <returns>
        /// The Android.Content.ContentProviderResult[].
        /// </returns>
        public override ContentProviderResult[] ApplyBatch(IList<ContentProviderOperation> operations)
        {
            this.InitIfNecessary();
            return base.ApplyBatch(operations);
        }

        /// <summary>
        /// The delete.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="selection">
        /// The selection.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The delete.
        /// </returns>
        public override int Delete(Uri uri, string selection, string[] args)
        {
            return 0;
        }

        /// <summary>
        /// The get type.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <returns>
        /// The get type.
        /// </returns>
        public override string GetType(Uri uri)
        {
            return "vnd.Android.cursor.item/asset";
        }

        /// <summary>
        /// The insert.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="values">
        /// The values.
        /// </param>
        /// <returns>
        /// The Android.Net.Uri.
        /// </returns>
        public override Uri Insert(Uri uri, ContentValues values)
        {
            return null;
        }

        /// <summary>
        /// The on create.
        /// </summary>
        /// <returns>
        /// The on create.
        /// </returns>
        public override bool OnCreate()
        {
            return true;
        }

        /// <summary>
        /// The open asset file.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="mode">
        /// The mode.
        /// </param>
        /// <returns>
        /// The Android.Content.Res.AssetFileDescriptor.
        /// </returns>
        public override AssetFileDescriptor OpenAssetFile(Uri uri, string mode)
        {
            this.InitIfNecessary();
            string path = uri.Path;
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

        /// <summary>
        /// The open file.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="mode">
        /// The mode.
        /// </param>
        /// <returns>
        /// The Android.OS.ParcelFileDescriptor.
        /// </returns>
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

        /// <summary>
        /// The query.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="projection">
        /// The projection.
        /// </param>
        /// <param name="selection">
        /// The selection.
        /// </param>
        /// <param name="selArgs">
        /// The sel args.
        /// </param>
        /// <param name="sort">
        /// The sort.
        /// </param>
        /// <returns>
        /// The Android.Database.ICursor.
        /// </returns>
        public override ICursor Query(Uri uri, string[] projection, string selection, string[] selArgs, string sort)
        {
            this.InitIfNecessary();

            // lists all of the items in the file that match
            ZipFileEntry[] zipEntries = this.apkExtensionFile == null
                                            ? new ZipFileEntry[0]
                                            : this.apkExtensionFile.GetAllEntries();

            ApezProjection[] setProjections = ApezProjections(ref projection);
            return MatrixCursor(projection, setProjections, zipEntries);
        }

        /// <summary>
        /// The update.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="values">
        /// The values.
        /// </param>
        /// <param name="selection">
        /// The selection.
        /// </param>
        /// <param name="selectionArgs">
        /// The selection args.
        /// </param>
        /// <returns>
        /// The update.
        /// </returns>
        public override int Update(Uri uri, ContentValues values, string selection, string[] selectionArgs)
        {
            return 0;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The apez projections.
        /// </summary>
        /// <param name="projection">
        /// The projection.
        /// </param>
        /// <returns>
        /// The System.IO.Compression.Zip.ApezProvider+ApezProjection[].
        /// </returns>
        private static ApezProjection[] ApezProjections(ref string[] projection)
        {
            if (projection == null)
            {
                string[] strings = FieldNameValueMap.Keys.ToArray();
                projection = strings;
            }

            return projection.Select(p => FieldNameValueMap[p]).ToArray();
        }

        /// <summary>
        /// The matrix cursor.
        /// </summary>
        /// <param name="projection">
        /// The projection.
        /// </param>
        /// <param name="intProjection">
        /// The int projection.
        /// </param>
        /// <param name="entries">
        /// The entries.
        /// </param>
        /// <returns>
        /// The Android.Database.MatrixCursor.
        /// </returns>
        private static MatrixCursor MatrixCursor(
            string[] projection, ApezProjection[] intProjection, ZipFileEntry[] entries)
        {
            var mc = new MatrixCursor(projection, entries.Length);
            foreach (ZipFileEntry zer in entries)
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

        /// <summary>
        /// The init if necessary.
        /// </summary>
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

        #endregion

        /// <summary>
        /// The apez content fields.
        /// </summary>
        private static class ApezContentFields
        {
            #region Constants

            /// <summary>
            /// The compressed length.
            /// </summary>
            public const string CompressedLength = "ZCOL";

            /// <summary>
            /// The compression type.
            /// </summary>
            public const string CompressionType = "ZTYP";

            /// <summary>
            /// The crc.
            /// </summary>
            public const string Crc = "ZCRC";

            /// <summary>
            /// The file.
            /// </summary>
            public const string File = BaseColumns.Id;

            /// <summary>
            /// The file name.
            /// </summary>
            public const string FileName = "ZPFN";

            /// <summary>
            /// The modification.
            /// </summary>
            public const string Modification = "ZMOD";

            /// <summary>
            /// The uncompressed length.
            /// </summary>
            public const string UncompressedLength = "ZUNL";

            /// <summary>
            /// The zip file.
            /// </summary>
            public const string ZipFile = "ZFIL";

            #endregion
        }
    }
}