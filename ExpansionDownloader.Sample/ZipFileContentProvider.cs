namespace ExpansionDownloader.Sample
{
    using System.IO.Compression.Zip;

    using Android.App;
    using Android.Content;

    [ContentProvider(new[] { ContentProviderAuthority }, Exported = false)]
	[MetaData(ApezProvider.MetaData.MainVersion, Value = "14")]
	[MetaData(ApezProvider.MetaData.PatchVersion, Value = "14")]
    public class ZipFileContentProvider : ApezProvider
    {
        public const string ContentProviderAuthority = "expansiondownloader.sample.ZipFileContentProvider";

        protected override string Authority
        {
            get { return ContentProviderAuthority; }
        }
    }
}