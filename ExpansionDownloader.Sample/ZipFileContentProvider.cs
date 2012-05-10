namespace System.IO.Compression.Zip.Tests
{
    using System.IO.Compression.Zip;

    public class ZipFileContentProvider : ApezProvider
    {
        public const string ContentProviderAuthority = "system.io.compression.zip.tests.ZipFileContentProvider";

        protected override string Authority
        {
            get { return ContentProviderAuthority; }
        }
    }
}