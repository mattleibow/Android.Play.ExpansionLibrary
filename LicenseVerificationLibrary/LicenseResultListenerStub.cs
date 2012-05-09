namespace LicenseVerificationLibrary
{
    using Android.OS;

    /// <summary>
    /// Local-side IPC implementation stub class.
    /// </summary>
    internal abstract class LicenseResultListenerStub : Binder, ILicenseResultListener
    {
        #region Constants and Fields

        /// <summary>
        /// The descriptor.
        /// </summary>
        private const string Descriptor = "com.android.vending.licensing.ILicenseResultListener";

        /// <summary>
        /// The transaction verify license.
        /// </summary>
        private const int TransactionVerifyLicense = BinderConsts.FirstCallTransaction + 0;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseResultListenerStub"/> class. 
        /// Construct the stub at attach it to the interface.
        /// </summary>
        protected LicenseResultListenerStub()
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
        public static ILicenseResultListener AsInterface(IBinder obj)
        {
            ILicenseResultListener result = null;

            if (obj != null)
            {
                var iin = obj.QueryLocalInterface(Descriptor) as ILicenseResultListener;
                result = iin ?? new Proxy(obj);
            }

            return result;
        }

        /// <summary>
        /// Cast an IBinder object into an ILicenseResultListener interface, generating a proxy if needed.
        /// </summary>
        public IBinder AsBinder()
        {
            return this;
        }

        /// <summary>
        /// The verify license.
        /// </summary>
        /// <param name="responseCode">
        /// The response code.
        /// </param>
        /// <param name="signedData">
        /// The signed data.
        /// </param>
        /// <param name="signature">
        /// The signature.
        /// </param>
        public abstract void VerifyLicense(ServerResponseCode responseCode, string signedData, string signature);

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
            var handled = false;

            switch (code)
            {
                case BinderConsts.InterfaceTransaction:
                    reply.WriteString(Descriptor);
                    handled = true;
                    break;

                case TransactionVerifyLicense:
                    data.EnforceInterface(Descriptor);
                    var responseCode = data.ReadInt();
                    var signedData = data.ReadString();
                    var signature = data.ReadString();
                    this.VerifyLicense((ServerResponseCode)responseCode, signedData, signature);
                    handled = true;
                    break;
            }

            return handled || base.OnTransact(code, data, reply, flags);
        }

        #endregion

        /// <summary>
        /// The proxy.
        /// </summary>
        private class Proxy : Binder, ILicenseResultListener
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
            /// The verify license.
            /// </summary>
            /// <param name="responseCode">
            /// The response code.
            /// </param>
            /// <param name="signedData">
            /// The signed data.
            /// </param>
            /// <param name="signature">
            /// The signature.
            /// </param>
            public void VerifyLicense(ServerResponseCode responseCode, string signedData, string signature)
            {
                Parcel data = Parcel.Obtain();
                try
                {
                    data.WriteInterfaceToken(Descriptor);
                    data.WriteInt((int)responseCode);
                    data.WriteString(signedData);
                    data.WriteString(signature);

                    this.remote.Transact(TransactionVerifyLicense, data, null, TransactionFlags.Oneway);
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