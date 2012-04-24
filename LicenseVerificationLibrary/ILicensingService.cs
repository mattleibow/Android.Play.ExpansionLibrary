using System;
using Android.OS;

namespace LicenseVerificationLibrary
{
    public interface ILicensingService : IInterface
    {
        void CheckLicense(long nonce, string packageName, ILicenseResultListener listener);
    }

    /// <summary>
    /// Local-side IPC implementation stub class.
    /// </summary>
    internal class LicensingServiceStub : Binder, ILicensingService
    {
        private const int TransactionCheckLicense = (BinderConsts.FirstCallTransaction + 0);
        private const string Descriptor = "com.android.vending.licensing.ILicensingService";

        /// <summary>
        /// Construct the stub at attach it to the interface.
        /// </summary>
        private LicensingServiceStub()
        {
            AttachInterface(this, Descriptor);
        }

        /// <summary>
        /// Cast an IBinder object into an ILicensingService interface, generating a proxy if needed.
        /// </summary>
        public IBinder AsBinder()
        {
            return this;
        }

        public virtual void CheckLicense(long nonce, string packageName, ILicenseResultListener listener)
        {
            throw new NotImplementedException();
        }

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

                    CheckLicense(nonce, packageName, resultListener);
                    handled = true;
                    break;
            }

            return handled || base.OnTransact(code, data, reply, flags);
        }

        private class Proxy : Binder, ILicensingService
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

            public void CheckLicense(long nonce, string packageName, ILicenseResultListener listener)
            {
                Parcel data = Parcel.Obtain();
                try
                {
                    data.WriteInterfaceToken(Descriptor);
                    data.WriteLong(nonce);
                    data.WriteString(packageName);
                    data.WriteStrongBinder(listener != null ? listener.AsBinder() : null);

                    _remote.Transact(TransactionCheckLicense, data, null, BinderConsts.FlagOneway);
                }
                finally
                {
                    data.Recycle();
                }
            }
        }
    }
}
