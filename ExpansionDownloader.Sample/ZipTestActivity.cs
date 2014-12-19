namespace System.IO.Compression.Zip.Tests
{
    using Android.App;
    using Android.OS;
    using Android.Widget;

    using ExpansionDownloader.Sample;

    using Uri = Android.Net.Uri;

    [Activity]
    public class ZipTestActivity : Activity
    {
        private VideoView video;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.VideoPlayer);

			this.FindViewById<Button>(Resource.Id.MainPlay).Click += delegate { this.ButtonClick("MetalGearSolidV.mp4"); };
			this.FindViewById<Button>(Resource.Id.PatchPlay).Click += delegate { this.ButtonClick("Titanfall.mp4"); };

            this.video = this.FindViewById<VideoView>(Resource.Id.MyVideo);
        }

        private void ButtonClick(string file)
        {
            var uri = Uri.Parse("content://" + ZipFileContentProvider.ContentProviderAuthority + "/" + file);
            this.video.SetVideoURI(uri);
            
            // add the start/pause controls
            var controller = new MediaController(this);
            controller.SetMediaPlayer(this.video);
            this.video.SetMediaController(controller);
            this.video.Prepared += delegate
            {
                // play the video
                this.video.Start();
            };

            this.video.RequestFocus();
        }
    }
}

