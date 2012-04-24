using Android.Content;
using Android.OS;

namespace ExpansionDownloader
{
    public interface IStub
    {
        Messenger getMessenger();
        void connect(Context c);
        void disconnect(Context c);
    }
}