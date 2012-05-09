namespace LicenseVerificationLibrary
{
    using System;

    using Android.OS;

    /// <summary>
    /// Local-side IPC implementation stub class.
    /// </summary>
    internal class LicensingServiceStub : Binder, ILicensingService
    {
        #region Constants and Fields

        /// <summary>
        /// The descriptor.
        /// </summary>
        private const string Descriptor = "com.android.vending.licensing.ILicensingService";

        /// <summary>
        /// The transaction check license.
        /// </summary>
        private const int TransactionCheckLicense = BinderConsts.FirstCallTransaction + 0;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Prevents a default instance of the <see cref="LicensingServiceStub"/> class from being created. 
        /// Construct the stub at attach it to the interface.
        /// </summary>
        private LicensingServiceStub()
        {
            System.Diagnostics.Debug.WriteLine(Descriptor);
            this.AttachInterface(this, Descriptor);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The as interface.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <returns>
        /// </returns>
        public static ILicensingService AsInterface(IBinder obj)
        {
            ILicensingService result = null;

            if (obj != null)
            {
                var iin = obj.QueryLocalInterface(Descriptor) as ILicensingService;
                result = iin ?? new Proxy(obj);
            }

            return result;
        }

        /// <summary>
        /// Cast an IBinder object into an ILicensingService interface, generating a proxy if needed.
        /// </summary>
        public IBinder AsBinder()
        {
            return this;
        }

        /// <summary>
        /// The check license.
        /// </summary>
        /// <param name="nonce">
        /// The nonce.
        /// </param>
        /// <param name="packageName">
        /// The package name.
        /// </param>
        /// <param name="listener">
        /// The listener.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// </exception>
        public void CheckLicense(long nonce, string packageName, ILicenseResultListener listener)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The on transact.
        /// </summary>
        /// <param name="code">
        /// The code.
        /// </param>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <param name="reply">
        /// The reply.
        /// </param>
        /// <param name="flags">
        /// The flags.
        /// </param>
        /// <returns>
        /// The on transact.
        /// </returns>
        protected override bool OnTransact(int code, Parcel data, Parcel reply, int flags)
        {
            bool handled = false;

            switch (code)
            {
                case BinderConsts.InterfaceTransaction:
                    reply.WriteString(Descriptor);
                    handled = true;
                    break;
                case TransactionCheckLicense:
                    data.EnforceInterface(Descriptor);
                    var nonce = data.ReadLong();
                    var packageName = data.ReadString();
                    var resultListener = LicenseResultListenerStub.AsInterface(data.ReadStrongBinder());

                    this.CheckLicense(nonce, packageName, resultListener);
                    handled = true;
                    break;
            }

            return handled || base.OnTransact(code, data, reply, flags);
        }

        #endregion

        /// <summary>
        /// The proxy.
        /// </summary>
        private class Proxy : Binder, ILicensingService
        {
            #region Constants and Fields

            /// <summary>
            /// The remote.
            /// </summary>
            private readonly IBinder remote;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="Proxy"/> class.
            /// </summary>
            /// <param name="remote">
            /// The remote.
            /// </param>
            public Proxy(IBinder remote)
            {
                this.remote = remote;
            }

            #endregion

            #region Public Properties

            /// <summary>
            /// Gets InterfaceDescriptor.
            /// </summary>
            public override string InterfaceDescriptor
            {
                get
                {
                    return Descriptor;
                }
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The as binder.
            /// </summary>
            /// <returns>
            /// </returns>
            public IBinder AsBinder()
            {
                return this.remote;
            }

            /// <summary>
            /// The check license.
            /// </summary>
            /// <param name="nonce">
            /// The nonce.
            /// </param>
            /// <param name="packageName">
            /// The package name.
            /// </param>
            /// <param name="listener">
            /// The listener.
            /// </param>
            public void CheckLicense(long nonce, string packageName, ILicenseResultListener listener)
            {
                Parcel data = Parcel.Obtain();
                try
                {
                    data.WriteInterfaceToken(Descriptor);
                    data.WriteLong(nonce);
                    data.WriteString(packageName);
                    data.WriteStrongBinder(listener != null ? listener.AsBinder() : null);

                    this.remote.Transact(TransactionCheckLicense, data, null, TransactionFlags.Oneway);
                }
                finally
                {
                    data.Recycle();
                }
            }

            #endregion
        }
    }
}