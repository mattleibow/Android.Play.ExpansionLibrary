namespace ExpansionDownloader.Service
{
    using System.Diagnostics;

    using Android.App;
    using Android.Content.PM;
    using Android.Provider;

    using ExpansionDownloader.Database;

    using Java.Lang;

    using LicenseVerificationLibrary;
    using LicenseVerificationLibrary.Obfuscator;
    using LicenseVerificationLibrary.Policy;

    using Exception = System.Exception;
    using Object = Java.Lang.Object;

    /// <summary>
    /// The downloader service.
    /// </summary>
    public abstract partial class DownloaderService
    {
        #region Methods

        /// <summary>
        /// Returns true if the LVL check is required.
        /// </summary>
        /// <param name="database">
        /// a downloads DB synchronized with the latest state
        /// </param>
        /// <param name="pi">
        /// the package info for the project
        /// </param>
        /// <returns>
        /// true if the filenames need to be returned
        /// </returns>
        private static bool IsLvlCheckRequired(DownloadsDatabase database, PackageInfo pi)
        {
            // we need to update the LVL check and get a successful status to proceed
            return database.VersionCode != pi.VersionCode;
        }

        #endregion

        /// <summary>
        /// The lvl runnable.
        /// </summary>
        private class LvlRunnable : Object, IRunnable
        {
            #region Constants and Fields

            /// <summary>
            /// The context.
            /// </summary>
            private readonly DownloaderService context;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="LvlRunnable"/> class.
            /// </summary>
            /// <param name="context">
            /// The context.
            /// </param>
            /// <param name="intent">
            /// The intent.
            /// </param>
            internal LvlRunnable(DownloaderService context, PendingIntent intent)
            {
                Debug.WriteLine("DownloaderService.LvlRunnable.ctor");
                this.context = context;
                this.context.pPendingIntent = intent;
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The run.
            /// </summary>
            public void Run()
            {
                this.context.IsServiceRunning = true;
                this.context.downloadNotification.OnDownloadStateChanged(DownloaderState.FetchingUrl);
                string deviceId = Settings.Secure.GetString(this.context.ContentResolver, Settings.Secure.AndroidId);

                var aep = new ApkExpansionPolicy(
                    this.context, new AesObfuscator(this.context.Salt, this.context.PackageName, deviceId));

                // reset our policy back to the start of the world to force a re-check
                aep.ResetPolicy();

                // let's try and get the OBB file from LVL first
                // Construct the LicenseChecker with a IPolicy.
                var checker = new LicenseChecker(this.context, aep, this.context.PublicKey);
                checker.CheckAccess(new ApkLicenseCheckerCallback(this, aep));
            }

            #endregion

            /// <summary>
            /// The apk license checker callback.
            /// </summary>
            private class ApkLicenseCheckerCallback : ILicenseCheckerCallback
            {
                #region Constants and Fields

                /// <summary>
                /// The lvl runnable.
                /// </summary>
                private readonly LvlRunnable lvlRunnable;

                /// <summary>
                /// The policy.
                /// </summary>
                private readonly ApkExpansionPolicy policy;

                #endregion

                #region Constructors and Destructors

                /// <summary>
                /// Initializes a new instance of the <see cref="ApkLicenseCheckerCallback"/> class.
                /// </summary>
                /// <param name="lvlRunnable">
                /// The lvl runnable.
                /// </param>
                /// <param name="policy">
                /// The policy.
                /// </param>
                public ApkLicenseCheckerCallback(LvlRunnable lvlRunnable, ApkExpansionPolicy policy)
                {
                    this.lvlRunnable = lvlRunnable;
                    this.policy = policy;
                }

                #endregion

                #region Properties

                /// <summary>
                /// Gets Context.
                /// </summary>
                private DownloaderService Context
                {
                    get
                    {
                        return this.lvlRunnable.context;
                    }
                }

                #endregion

                #region Public Methods and Operators

                /// <summary>
                /// The allow.
                /// </summary>
                /// <param name="reason">
                /// The reason.
                /// </param>
                /// <exception cref="Java.Lang.RuntimeException">
                /// Error with LVL checking and database integrity
                /// </exception>
                /// <exception cref="Java.Lang.RuntimeException">
                /// Error with getting information from package name
                /// </exception>
                public void Allow(PolicyServerResponse reason)
                {
                    try
                    {
                        DownloadsDatabase database = DownloadsDatabase.Instance;

                        int count = this.policy.GetExpansionFilesCount();
                        if (count == 0)
                        {
                            Debug.WriteLine("No expansion packs.");
                        }

                        DownloadStatus status = 0;
                        for (int index = 0; index < count; index++)
                        {
                            var type = (ApkExpansionPolicy.ExpansionFileType)index;

                            var expansionFile = this.policy.GetExpansionFile(type);
                            string currentFileName = expansionFile.FileName;
                            if (currentFileName != null)
                            {
                                var di = new DownloadInfo
                                    {
                                        ExpansionFileType = type,
                                        FileName = currentFileName,
                                    };

                                if (this.Context.HandleFileUpdated(database, currentFileName, expansionFile.FileSize))
                                {
                                    status = DownloadStatus.Unknown;
                                    di.ResetDownload();
                                    di.Uri = expansionFile.Url;
                                    di.TotalBytes = expansionFile.FileSize;
                                    di.Status = status;
                                    database.UpdateDownload(di);
                                }
                                else
                                {
                                    // we need to read the download information from the database
                                    DownloadInfo dbdi = database.GetDownloadInfo(di.FileName);
                                    if (dbdi == null)
                                    {
                                        // the file exists already and is the correct size
                                        // was delivered by Market or through another mechanism
                                        Debug.WriteLine("file {0} found. Not downloading.", di.FileName);
                                        di.Status = DownloadStatus.Success;
                                        di.TotalBytes = expansionFile.FileSize;
                                        di.CurrentBytes = expansionFile.FileSize;
                                        di.Uri = expansionFile.Url;
                                        database.UpdateDownload(di);
                                    }
                                    else if (dbdi.Status != DownloadStatus.Success)
                                    {
                                        // we just update the URL
                                        dbdi.Uri = expansionFile.Url;
                                        database.UpdateDownload(dbdi);
                                        status = DownloadStatus.Unknown;
                                    }
                                }
                            }
                        }

                        // first: do we need to do an LVL update?
                        // we begin by getting our APK version from the package manager
                        try
                        {
                            PackageInfo pi = this.Context.PackageManager.GetPackageInfo(this.Context.PackageName, 0);
                            database.UpdateMetadata(pi.VersionCode, status);
                            var required = StartDownloadServiceIfRequired(
                                this.Context, this.Context.pPendingIntent, this.Context.GetType());
                            switch (required)
                            {
                                case DownloadServiceRequirement.NoDownloadRequired:
                                    this.Context.downloadNotification.OnDownloadStateChanged(
                                        DownloaderState.Completed);
                                    break;

                                case DownloadServiceRequirement.LvlCheckRequired: // DANGER WILL ROBINSON!
                                    Debug.WriteLine("In LVL checking loop!");
                                    this.Context.downloadNotification.OnDownloadStateChanged(
                                        DownloaderState.FailedUnlicensed);
                                    throw new RuntimeException("Error with LVL checking and database integrity");

                                case DownloadServiceRequirement.DownloadRequired:

                                    // do nothing: the download will notify the application when things are done
                                    break;
                            }
                        }
                        catch (PackageManager.NameNotFoundException e1)
                        {
                            e1.PrintStackTrace();
                            throw new RuntimeException("Error with getting information from package name");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("LVL Update Exception: " + ex.Message);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Allow Exception: " + ex.Message);
                        throw;
                    }
                    finally
                    {
                        this.Context.IsServiceRunning = false;
                    }
                }

                /// <summary>
                /// The application error.
                /// </summary>
                /// <param name="errorCode">
                /// The error code.
                /// </param>
                public void ApplicationError(CallbackErrorCode errorCode)
                {
                    try
                    {
                        this.Context.downloadNotification.OnDownloadStateChanged(
                            DownloaderState.FailedFetchingUrl);
                    }
                    finally
                    {
                        this.Context.IsServiceRunning = false;
                    }
                }

                /// <summary>
                /// The dont allow.
                /// </summary>
                /// <param name="reason">
                /// The reason.
                /// </param>
                public void DontAllow(PolicyServerResponse reason)
                {
                    try
                    {
                        switch (reason)
                        {
                            case PolicyServerResponse.NotLicensed:
                                this.Context.downloadNotification.OnDownloadStateChanged(
                                    DownloaderState.FailedUnlicensed);
                                break;
                            case PolicyServerResponse.Retry:
                                this.Context.downloadNotification.OnDownloadStateChanged(
                                    DownloaderState.FailedFetchingUrl);
                                break;
                        }
                    }
                    finally
                    {
                        this.Context.IsServiceRunning = false;
                    }
                }

                #endregion
            }
        }
    }
}