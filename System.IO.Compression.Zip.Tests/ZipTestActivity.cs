namespace System.IO.Compression.Zip.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Linq;

    using Android.App;
    using Android.OS;
    using Android.Widget;

    using Uri = Android.Net.Uri;

    [Activity(Label = "TheZipTest", MainLauncher = true, Icon = "@drawable/icon")]
    public class ZipTestActivity : Activity
    {
        private VideoView video;

        private const string ZipFile = "/sdcard/Archive.zip";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            this.FindViewById<Button>(Resource.Id.MainPlay).Click += delegate { this.ButtonClick("video_001_sting.m4v"); };
            this.FindViewById<Button>(Resource.Id.PatchPlay).Click += delegate { this.ButtonClick("03_texts.mp4"); };

            this.video = this.FindViewById<VideoView>(Resource.Id.MyVideo);
        }

        private void ButtonClick(string file)
        {
            const string Authority = "system.io.compression.zip.tests.ZipFileContentProvider";
            var uri = Uri.Parse("content://" + Authority + "/" + file);
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

