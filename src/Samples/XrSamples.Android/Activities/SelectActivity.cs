using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Content.PM;
using System.Text.Json;
using XrEngine.OpenXr;

namespace XrSamples.Android.Activities
{

    [Activity(
        Label = "@string/app_name",
        ScreenOrientation = ScreenOrientation.Portrait,
        Exported = true,
        LaunchMode = LaunchMode.SingleTask,
        MainLauncher = true)]

    public class SelectActivity : Activity
    {
        private readonly GameSettings _settings = GameSettings.Graffiti();
        private IList<AppSample>? _samples;

        public SelectActivity()
        {
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (!string.IsNullOrWhiteSpace(_settings.SampleName) && savedInstanceState == null)
            {
                StartGame();
                Finish();
                return;
            }

            SetContentView(ResourceConstant.Layout.activity_select);

            //Samples
            var manager = XrEngine.Context.Require<SampleManager>();

            _samples = manager.List();

            var listView = FindViewById<ListView>(ResourceConstant.Id.listView)!;

            listView.Adapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleListItemSingleChoice,
                _samples.Select(a => a.Name!).ToArray());

            listView.ItemClick += OnSampleSelected;

            //MSAA
            var mssa = FindViewById<Spinner>(ResourceConstant.Id.msaa)!;
            mssa.Adapter = new ArrayAdapter<int>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                [1, 2, 4]);

            mssa.ItemSelected += (s, e) =>
            {
                _settings.Msaa = (int)e.Parent!.GetItemAtPosition(e.Position)!;
            };

            //HDRI
            var images = manager.GetHDRs().ToArray();

            var hdris = FindViewById<Spinner>(ResourceConstant.Id.hdri)!;
            hdris.Adapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                images.Select(a => a.Name!).ToArray());

            hdris.ItemSelected += (s, e) =>
            {
                _settings.Hdri = images[e.Position].Uri;
            };

            //Engine
            GraphicDriver[] engines = [GraphicDriver.OpenGL, GraphicDriver.FilamentVulkan, GraphicDriver.FilamentOpenGL];

            var engine = FindViewById<Spinner>(ResourceConstant.Id.engine)!;
            engine.Adapter = new ArrayAdapter<GraphicDriver>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                engines);

            engine.ItemSelected += (s, e) =>
            {
                _settings.Driver = engines[e.Position];
            };

            //MultiView

            var mw = FindViewById<CheckBox>(ResourceConstant.Id.multi_view)!;
            mw.Checked = _settings.IsMultiView;
            mw.CheckedChange += (s, e) =>
            {
                _settings.IsMultiView = e.IsChecked;
            };

            //Depth
            var depth = FindViewById<CheckBox>(ResourceConstant.Id.depth)!;
            depth.Checked = _settings.EnableDepthPass;
            depth.CheckedChange += (s, e) =>
            {
                _settings.EnableDepthPass = e.IsChecked;
            };

            //Compression
            var compression = FindViewById<CheckBox>(ResourceConstant.Id.compression)!;
            compression.Checked = _settings.TextureCompression;
            compression.CheckedChange += (s, e) =>
            {
                _settings.TextureCompression = e.IsChecked;
            };

            //Space-Warp
            var sw = FindViewById<CheckBox>(ResourceConstant.Id.space_warp)!;
            sw.Checked = _settings.UseSpaceWarp;
            sw.CheckedChange += (s, e) =>
            {
                _settings.UseSpaceWarp = e.IsChecked;
            };

        }

        protected void StartGame()
        {
            var intent = new Intent(this, typeof(GameActivity));
            intent.PutExtra("Settings", JsonSerializer.Serialize(_settings));
            StartActivity(intent);
        }

        private void OnSampleSelected(object? sender, AdapterView.ItemClickEventArgs e)
        {
            _settings.SampleName = (string)e.Parent!.GetItemAtPosition(e.Position)!;
            StartGame();
        }
    }
}