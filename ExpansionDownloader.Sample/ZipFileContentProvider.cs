namespace ExpansionDownloader.Sample
{
    using System.IO.Compression.Zip;

    using Android.App;
    using Android.Content;

    [ContentProvider(new[] { ContentProviderAuthority }, Exported = false)]
    //[MetaData("mainVersion", Value = "56")]
    //[MetaData("patchVersion", Value = "56")]
    public class ZipFileContentProvider : ApezProvider
    {
        public const string ContentProviderAuthority = "expansiondownloader.sample.ZipFileContentProvider";

        protected override string Authority
        {
            get { return ContentProviderAuthority; }
        }
    }
}