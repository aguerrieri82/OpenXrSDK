using Android.Content;
using Android.Content.PM;
using Android.Webkit;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Text.Json;
using XrEngine;
using XrEngine.Devices.Android;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Android;

namespace XrSamples.Android.Activities
{

    [IntentFilter(["android.intent.action.MAIN"],
        Categories =
        [
            "android.intent.category.DEFAULT",
            "com.oculus.intent.category.VR"
        ])]
    [Activity(
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        LaunchMode = LaunchMode.SingleTask,
        Exported = true,
        MainLauncher = false,
        HardwareAccelerated = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Orientation |
                               ConfigChanges.KeyboardHidden | ConfigChanges.Keyboard | ConfigChanges.Navigation |
                               ConfigChanges.UiMode,
        ScreenOrientation = ScreenOrientation.Landscape)]
    public class GameActivity : XrEngineActivity
    {
        private WebView? _webView;
        private XrWebViewLayer? _webViewLayer;
        private GameSettings? _settings;
        private AndroidUsbCameraManager? _usbCameraManager;

        public GameActivity()
        {
            _permissions.Add("horizonos.permission.HEADSET_CAMERA");
        }

        protected override void OnLoad()
        {
            _settings = GameSettings.Graffiti();

            var settingsJson = Intent?.GetStringExtra("Settings");

            if (settingsJson == null && string.IsNullOrWhiteSpace(_settings.SampleName))
            {
                var intent = new Intent(this, typeof(SelectActivity));
                StartActivity(intent);
                FinishAndRemoveTask();
                return;
            }

            if (settingsJson != null)
                _settings = JsonSerializer.Deserialize<GameSettings>(settingsJson);

            _usbCameraManager = new AndroidUsbCameraManager(this);

            base.OnLoad();
        }

        protected override void OnStart()
        {
            base.OnStart();
            _usbCameraManager?.Start();
        }

        protected override void OnStop()
        {
            _usbCameraManager?.Stop();
            base.OnStop();
        }

        protected override void OnDestroy()
        {
            _usbCameraManager?.Dispose();
            _usbCameraManager = null;

            base.OnDestroy();
        }

        protected override void OnXrAppStarted(XrApp app)
        {
            _webViewLayer = _engine!.XrApp.Layers.List.OfType<XrWebViewLayer>().FirstOrDefault();

            if (_webViewLayer != null)
            {
                _webView = _webViewLayer.WebView!;
                _webView.LoadUrl("https://www.youtube.com");
            }

            base.OnXrAppStarted(app);
        }

        protected override void BuildApp(XrEngineAppBuilder builder)
        {
            var external = global::Android.OS.Environment.ExternalStorageDirectory!.AbsolutePath;
            XrEngine.Context.Implement<IAssetStore>(new LocalAssetStore(Path.Combine(external, "Assets")));

            Debug.Assert(_settings != null);

            var useAngle = _settings.Driver == GraphicDriver.Angle;

            builder.Options.Driver = _settings.Driver;

            if (_settings.Driver == GraphicDriver.OpenGL)
                builder.UseOpenGL();
            else if (_settings.Driver == GraphicDriver.Angle)
                builder.UseAngle();

            if (_settings.Driver == GraphicDriver.OpenGL || _settings.Driver == GraphicDriver.Angle)
                builder.SetGlOptions(opt =>
                {
                    opt.UseDepthPass = _settings.EnableDepthPass;

                    opt.SortByCameraDistance = !_settings.EnableDepthPass;
                    opt.FrustumCulling = _settings.FrustumCulling;

                    opt.Compression.Use = _settings.TextureCompression;
                    opt.Compression.BlockSize = 4;
                    opt.Compression.Quality = 60;

                    opt.UseSharedSsbo = _settings.UseSharedSsbo;
                    opt.UseAsyncShaderCompile = _settings.UseAsyncShaderCompile;
                    opt.UseShaderCache = true;
                    opt.UseShaderPreprocessor = true;

                    opt.ToneMap = _settings.ToneMap;

                    opt.FloatPrecision = ShaderPrecision.High;
                    opt.SamplerPrecision = ShaderPrecision.Medium;
                    opt.IntPrecision = ShaderPrecision.High;

                    opt.InvalidateDepth = false;
                    opt.UsePrimitiveBoundingBox = _settings.UsePrimitiveBoundingBox;

                    opt.UseFxAA = _settings.UseFxAA;
                    opt.UseRayCollider = _settings.UseRayCollider;

                    if (_settings.Msaa > 1)
                        opt.UseFxAA = false;

                    if (!XrDevice.IsMetaQuest)
                    {
                        opt.UseAsyncShaderCompile = false;
                        opt.UsePrimitiveBoundingBox = false;
                    }
                });
            else
                ImageLight.UseCache = false;

            TriangleMesh.EnableCompression = _settings.UseMeshCompression;

            builder.SetXrOptions(opt =>
            {
                if (!XrDevice.IsMetaQuest)
                    opt.BlendMode = EnvironmentBlendMode.Opaque;

                opt.UseSimmetricFov = _settings.UseSimmetricFov;
            });

            builder.UseOculus(opt =>
            {
                opt.UseDynamicResolution = _settings.UseDynamicResolution;
            });

            if ((_settings.Driver == GraphicDriver.OpenGL || _settings.Driver == GraphicDriver.Angle) && _settings.IsMultiView)
                builder.UseMultiView();

            builder.SetRenderQuality(_settings.Scale, (uint)_settings.Msaa)
                   .RemovePlaneGrid();

            if (_settings.UseProfileOverlay) 
                builder.AddProfileOverlay();

            if (XrDevice.IsMetaQuest && _settings.ProjDepthMode != XrProjDepthMode.None)
                builder.UseProjDepth(_settings.ProjDepthMode, _settings.DepthScale);

            if (_settings.MotionVectorMode != MotionVectorMode.None)
                builder.UseSpaceWarp(_settings.MotionVectorMode);

#if DEBUG
            GlDebug.TrackBuffers = false;
            builder.EnableDebug(sync: true);
#endif

            SampleScenes.DefaultHDR = _settings.Hdri;

            var manager = XrEngine.Context.Require<SampleManager>();
            var sample = manager.GetSample(_settings.SampleName!);

            sample.Build!(builder);
        }
    }
}