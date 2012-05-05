namespace System.IO.Compression.Zip.Tests
{
    using System.IO.Compression.Zip;

    public class ZipFileContentProvider : ApezProvider
    {
        protected override string Authority
        {
            get { return "system.io.compression.zip.tests.ZipFileContentProvider"; }
        }
    }
}