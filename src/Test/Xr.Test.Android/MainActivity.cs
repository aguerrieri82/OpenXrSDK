using Android.Content;
using Android.Content.PM;
using Android.Webkit;

namespace Xr.Test.Android
{

    [Activity(
        Label = "@string/app_name",
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        ScreenOrientation = ScreenOrientation.Landscape,
        LaunchMode = LaunchMode.SingleTask,
        MainLauncher = false)]

    public class MainActivity : Activity
    {
        const string TAG = nameof(MainActivity);

        private WebView? _webView;

        protected unsafe override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            FindViewById<Button>(Resource.Id.getRooom)!.Click += (_, _) => _ = Task.Run(StartApp);

            ConfigureWebView();
        }


        void ConfigureWebView()
        {

            WebView.SetWebContentsDebuggingEnabled(true);
            _webView = FindViewById<WebView>(Resource.Id.webView)!;

            _webView.Settings.JavaScriptEnabled = true;
            _webView.Settings.AllowContentAccess = true;
            _webView.Settings.DomStorageEnabled = true;
            _webView.Settings.JavaScriptCanOpenWindowsAutomatically = true;
            _webView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            _webView.Settings.LoadsImagesAutomatically = true;

            _webView!.LoadUrl("https://roomdesigner.eusoft.net/");
        }


        private void StartApp()
        {
            StartActivityForResult(new Intent(this, typeof(GameActivity)), 100);
        }
    }
}