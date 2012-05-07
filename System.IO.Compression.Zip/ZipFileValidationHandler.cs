namespace System.IO.Compression.Zip
{
    public class ZipFileValidationHandler
    {
        public ZipFileValidationHandler(string filename)
        {
            Filename = filename;
        }

        public string Filename { get; private set; }
        public bool ShouldCancel { get; set; }

        public long TotalBytes { get; set; }

        public long CurrentBytes { get; set; }
        public float AverageSpeed { get; set; }
        public long TimeRemaining { get; set; }

        public Action<ZipFileValidationHandler> UpdateUi { get; set; }
    }
}