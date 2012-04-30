using Android.OS;

namespace LicenseVerificationLibrary
{
    /// <summary>
    /// Local-side IPC implementation stub class.
    /// </summary>
    internal abstract class LicenseResultListenerStub : Binder, ILicenseResultListener
    {
        private const int TransactionVerifyLicense = (BinderConsts.FirstCallTransaction + 0);
        private const string Descriptor = "com.android.vending.licensing.ILicenseResultListener";

        /// <summary>
        /// Construct the stub at attach it to the interface.
        /// </summary>
        protected LicenseResultListenerStub()
        {
            AttachInterface(this, Descriptor);
        }

        /// <summary>
        /// Cast an IBinder object into an ILicenseResultListener interface, generating a proxy if needed.
        /// </summary>
        public IBinder AsBinder()
        {
            return this;
        }

        public abstract void VerifyLicense(ServerResponseCode responseCode, string signedData, string signature);

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
                    VerifyLicense((ServerResponseCode) responseCode, signedData, signature);
                    handled = true;
                    break;
            }

            return handled || base.OnTransact(code, data, reply, flags);
        }

        private class Proxy : Binder, ILicenseResultListener
        {
            private readonly IBinder _remote;

            public Proxy(IBinder remote)
            {
                _remote = remote;
            }

            public override string InterfaceDescriptor
            {
                get { return Descriptor; }
            }

            public IBinder AsBinder()
            {
                return _remote;
            }

            public void VerifyLicense(ServerResponseCode responseCode, string signedData, string signature)
            {
                Parcel data = Parcel.Obtain();
                try
                {
                    data.WriteInterfaceToken(Descriptor);
                    data.WriteInt((int) responseCode);
                    data.WriteString(signedData);
                    data.WriteString(signature);

                    _remote.Transact(TransactionVerifyLicense, data, null, TransactionFlags.Oneway);
                }
                finally
                {
                    data.Recycle();
                }
            }
        }
    }
}
