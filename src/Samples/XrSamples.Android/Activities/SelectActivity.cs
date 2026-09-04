using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Content.PM;
using System.Text.Json;
using XrEngine;
using XrEngine.OpenGL;
using XrEngine.OpenXr;

namespace XrSamples.Android.Activities
{

    [IntentFilter(["android.intent.action.MAIN"],
        Categories =
        [
            "android.intent.category.DEFAULT",
            "com.oculus.intent.category.2D"
        ])]
    [Activity(
        Label = "@string/app_name",
        ScreenOrientation = ScreenOrientation.Landscape,
        Exported = true,
        LaunchMode = LaunchMode.SingleTask,
        MainLauncher = true,
        ResizeableActivity = true,
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.ScreenLayout |
            ConfigChanges.Orientation |
            ConfigChanges.KeyboardHidden |
            ConfigChanges.Keyboard |
            ConfigChanges.Navigation |
            ConfigChanges.UiMode)]

    public class SelectActivity : Activity
    {
        private const string PreferencesName = "XrSamples";
        private const string SettingsKey = "GameSettings";

        private GameSettings _settings = GameSettings.Graffiti();
        private IList<AppSample>? _samples;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (!string.IsNullOrWhiteSpace(_settings.SampleName) && savedInstanceState == null)
            {
                StartGame();
                FinishAndRemoveTask();
                return;
            }

            LoadSettings();

            SetContentView(ResourceConstant.Layout.activity_select);

            var manager = XrEngine.Context.Require<SampleManager>();

            _samples = manager.List()
                .OrderByDescending(a => a.Name == _settings.SampleName)
                .ThenBy(a => a.Name)
                .ToList();

            SetupSamples();
            SetupMsaa();
            SetupHdri(manager);
            SetupDriver();
            SetupToneMap();
            SetupDepthMode();
            SetupSpaceWarp();

            SetupCheckBox(ResourceConstant.Id.multi_view, _settings.IsMultiView, value => _settings.IsMultiView = value);
            SetupCheckBox(ResourceConstant.Id.depth, _settings.EnableDepthPass, value => _settings.EnableDepthPass = value);
            SetupCheckBox(ResourceConstant.Id.compression, _settings.TextureCompression, value => _settings.TextureCompression = value);
            SetupCheckBox(ResourceConstant.Id.fxaa, _settings.UseFxAA, value => _settings.UseFxAA = value);
            SetupCheckBox(ResourceConstant.Id.symmetric_fov, _settings.UseSimmetricFov, value => _settings.UseSimmetricFov = value);
            SetupCheckBox(ResourceConstant.Id.dynamic_resolution, _settings.UseDynamicResolution, value => _settings.UseDynamicResolution = value);
            SetupCheckBox(ResourceConstant.Id.ray_collider, _settings.UseRayCollider, value => _settings.UseRayCollider = value);
            SetupCheckBox(ResourceConstant.Id.primitive_bounding_box, _settings.UsePrimitiveBoundingBox, value => _settings.UsePrimitiveBoundingBox = value);
            SetupCheckBox(ResourceConstant.Id.shared_ssbo, _settings.UseSharedSsbo, value => _settings.UseSharedSsbo = value);
            SetupCheckBox(ResourceConstant.Id.mesh_compression, _settings.UseMeshCompression, value => _settings.UseMeshCompression = value);
            SetupCheckBox(ResourceConstant.Id.profile_overlay, _settings.UseProfileOverlay, value => _settings.UseProfileOverlay = value);
            SetupCheckBox(ResourceConstant.Id.async_shader_compile, _settings.UseAsyncShaderCompile, value => _settings.UseAsyncShaderCompile = value);

            SetupScale(ResourceConstant.Id.render_scale, ResourceConstant.Id.render_scale_label, "Render Scale", _settings.Scale, 0.25f, 2f, value => _settings.Scale = value);
            SetupScale(ResourceConstant.Id.depth_scale, ResourceConstant.Id.depth_scale_label, "Depth Scale", _settings.DepthScale, 0.25f, 1f, value => _settings.DepthScale = value);
        }

        private void SetupSamples()
        {
            var listView = FindViewById<ListView>(ResourceConstant.Id.listView)!;

            listView.Adapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleListItem1,
                _samples!.Select(a => a.Name!).ToArray());

            listView.ItemClick += OnSampleSelected;
        }

        private void SetupMsaa()
        {
            int[] values = [1, 2, 4];

            var seekBar = FindViewById<SeekBar>(ResourceConstant.Id.msaa)!;
            var label = FindViewById<TextView>(ResourceConstant.Id.msaa_label)!;

            seekBar.Min = 0;
            seekBar.Max = values.Length - 1;

            var index = Array.IndexOf(values, _settings.Msaa);
            if (index < 0)
                index = 0;

            seekBar.Progress = index;
            label.Text = $"MSAA: {values[index]}x";

            seekBar.ProgressChanged += (s, e) =>
            {
                if (!e.FromUser)
                    return;

                _settings.Msaa = values[e.Progress];
                label.Text = $"MSAA: {_settings.Msaa}x";

                SaveSettings();
            };
        }

        private void SetupHdri(SampleManager manager)
        {
            var images = manager.GetHDRs().ToArray();

            var spinner = FindViewById<Spinner>(ResourceConstant.Id.hdri)!;
            spinner.Adapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                images.Select(a => a.Name!).ToArray());

            var index = Array.FindIndex(images, a => a.Uri == _settings.Hdri);
            if (index >= 0)
                spinner.SetSelection(index);

            spinner.ItemSelected += (s, e) =>
            {
                _settings.Hdri = images[e.Position].Uri;
                SaveSettings();
            };
        }

        private void SetupDriver()
        {
            GraphicDriver[] values =
            [
                GraphicDriver.OpenGL,
                GraphicDriver.Angle,
                GraphicDriver.FilamentVulkan,
                GraphicDriver.FilamentOpenGL
            ];

            SetupSpinner(ResourceConstant.Id.engine, values, _settings.Driver, value => _settings.Driver = value);
        }

        private void SetupToneMap()
        {
            ToneMapMode[] values =
            [
                ToneMapMode.None,
                ToneMapMode.Normal,
                ToneMapMode.Neutral,
                ToneMapMode.Aces
            ];

            SetupSpinner(ResourceConstant.Id.tone_map, values, _settings.ToneMap, value => _settings.ToneMap = value);
        }

        private void SetupDepthMode()
        {
            XrProjDepthMode[] values =
            [
                XrProjDepthMode.None,
                XrProjDepthMode.DepthPass,
                XrProjDepthMode.DepthCopy,
                XrProjDepthMode.DepthCopyImage
            ];

            SetupSpinner(ResourceConstant.Id.proj_depth, values, _settings.ProjDepthMode, value => _settings.ProjDepthMode = value);
        }

        private void SetupSpaceWarp()
        {
            MotionVectorMode[] values =
            [
                MotionVectorMode.None,
                MotionVectorMode.Pass,
                MotionVectorMode.Shared
            ];

            SetupSpinner(ResourceConstant.Id.space_warp, values, _settings.MotionVectorMode, value => _settings.MotionVectorMode = value);
        }

        private void SetupSpinner<T>(int id, T[] values, T currentValue, Action<T> setter)
        {
            var spinner = FindViewById<Spinner>(id)!;

            spinner.Adapter = new ArrayAdapter<T>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                values);

            var index = Array.IndexOf(values, currentValue);
            if (index >= 0)
                spinner.SetSelection(index);

            spinner.ItemSelected += (s, e) =>
            {
                setter(values[e.Position]);
                SaveSettings();
            };
        }

        private void SetupCheckBox(int id, bool value, Action<bool> setter)
        {
            var checkBox = FindViewById<CheckBox>(id)!;

            checkBox.Checked = value;

            checkBox.CheckedChange += (s, e) =>
            {
                setter(e.IsChecked);
                SaveSettings();
            };
        }

        private void SetupScale(int id, int labelId, string labelText, float value, float min, float max, Action<float> setter)
        {
            const float step = 0.25f;

            var seekBar = FindViewById<SeekBar>(id)!;
            var label = FindViewById<TextView>(labelId)!;

            var steps = (int)MathF.Round((max - min) / step);

            seekBar.Min = 0;
            seekBar.Max = steps;
            seekBar.Progress = Math.Clamp((int)MathF.Round((value - min) / step), 0, steps);

            var current = min + seekBar.Progress * step;

            label.Text = $"{labelText}: {current:0.##}";
            setter(current);

            seekBar.ProgressChanged += (s, e) =>
            {
                if (!e.FromUser)
                    return;

                current = min + e.Progress * step;

                label.Text = $"{labelText}: {current:0.##}";
                setter(current);

                SaveSettings();
            };
        }

        private void LoadSettings()
        {
            var preferences = GetSharedPreferences(PreferencesName, FileCreationMode.Private);
            var json = preferences?.GetString(SettingsKey, null);

            if (string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                _settings = JsonSerializer.Deserialize<GameSettings>(json) ?? GameSettings.Graffiti();
            }
            catch
            {
                _settings = GameSettings.Graffiti();
            }
        }

        private void SaveSettings()
        {
            var preferences = GetSharedPreferences(PreferencesName, FileCreationMode.Private);

            preferences!.Edit()!
                .PutString(SettingsKey, JsonSerializer.Serialize(_settings))!
                .Apply();
        }

        protected void StartGame()
        {
            SaveSettings();

            var intent = new Intent(this, typeof(GameActivity));
            intent.SetAction(Intent.ActionMain);
            intent.AddFlags(ActivityFlags.NewTask);
            intent.PutExtra("Settings", JsonSerializer.Serialize(_settings));

            StartActivity(intent);
        }

        private void OnSampleSelected(object? sender, AdapterView.ItemClickEventArgs e)
        {
            _settings.SampleName = _samples![e.Position].Name;

            SaveSettings();
            StartGame();
        }
    }
}