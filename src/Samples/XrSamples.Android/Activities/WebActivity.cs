using Android.Content;
using Android.Content.PM;
using Android.Webkit;

namespace XrSamples.Android.Activities
{

    [Activity(
        Label = "@string/app_name",
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        ScreenOrientation = ScreenOrientation.Landscape,
        LaunchMode = LaunchMode.SingleTask,
        MainLauncher = false)]

    public class WebActivity : Activity
    {
        const string TAG = nameof(WebActivity);

        private WebView? _webView;

        protected unsafe override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            SetContentView(_Microsoft.Android.Resource.Designer.ResourceConstant.Layout.activity_web);

            FindViewById<Button>(_Microsoft.Android.Resource.Designer.ResourceConstant.Id.getRooom)!.Click += (_, _) => _ = Task.Run(StartApp);

            ConfigureWebView();
        }


        void ConfigureWebView()
        {

            WebView.SetWebContentsDebuggingEnabled(true);

            _webView = FindViewById<WebView>(_Microsoft.Android.Resource.Designer.ResourceConstant.Id.webView)!;

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