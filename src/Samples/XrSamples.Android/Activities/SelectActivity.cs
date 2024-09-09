using Android.Content;
using Android.Content.PM;
using Android.Webkit;
using Android.Widget;
using System.Text.Json;
using _Microsoft.Android.Resource.Designer;
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
        const string TAG = nameof(SelectActivity);

        private readonly GameSettings _settings = new GameSettings();
        private IList<AppSample>? _samples;

        protected unsafe override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(ResourceConstant.Layout.activity_select);

            var manager = XrEngine.Context.Require<SampleManager>();

            _samples = manager.List();

            var listView = FindViewById<ListView>(ResourceConstant.Id.listView)!;

            listView.Adapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleListItemSingleChoice,
                _samples.Select(a => a.Name).ToArray());

            listView.ItemClick += OnSampleSelected;


            var mssa = FindViewById<Spinner>(ResourceConstant.Id.msaa)!;
            mssa.Adapter = new ArrayAdapter<int>(this, 
                global::Android.Resource.Layout.SimpleSpinnerItem, 
                [1, 2, 4]);

            mssa.ItemSelected += (s, e) =>
            {
                _settings.Msaa = (int)e.Parent.GetItemAtPosition(e.Position);
            };

            var images = manager.GetHDRs().ToArray();

            var hdris = FindViewById<Spinner>(ResourceConstant.Id.hdri)!;
            hdris.Adapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                images.Select(a=> a.Name).ToArray());

            hdris.ItemSelected += (s, e) =>
            {
                _settings.Hdri = images[e.Position].Uri;
            };

            GraphicDriver[] engines = [GraphicDriver.OpenGL, GraphicDriver.FilamentVulkan, GraphicDriver.FilamentOpenGL];

            var engine = FindViewById<Spinner>(ResourceConstant.Id.engine)!;
            engine.Adapter = new ArrayAdapter<GraphicDriver>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                engines);

            engine.ItemSelected += (s, e) =>
            {
                _settings.Driver = engines[e.Position];
            };
        }

        private void OnSampleSelected(object? sender, AdapterView.ItemClickEventArgs e)
        {
            _settings.SampleName = (string)e.Parent.GetItemAtPosition(e.Position);

            var intent = new Intent(this, typeof(GameActivity));
            intent.PutExtra("Settings", JsonSerializer.Serialize(_settings));
            StartActivity(intent);
        }
    }
}