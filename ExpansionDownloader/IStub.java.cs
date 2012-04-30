using Android.Content;
using Android.OS;

namespace ExpansionDownloader
{
    public interface IStub
    {
        Messenger GetMessenger();
        void Connect(Context c);
        void Disconnect(Context c);
    }
}