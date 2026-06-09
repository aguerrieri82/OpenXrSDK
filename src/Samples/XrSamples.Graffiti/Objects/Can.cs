
using OpenXr.Framework.Oculus;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Gltf;
using XrEngine.Media;
using XrEngine.OpenXr;
using XrMath;
using XrSamples.Graffiti.Objects;

namespace XrSamples.Graffiti
{

    public class Can : Group3D
    {
        private readonly Object3D _canBody;
        private readonly Object3D _cap;
        private float _sprayAperture;
        private XrOculusTouchController? _inputs;
        private readonly AudioLooper _shakeLoop;
        private readonly AudioLooper _sprayLoop;
        private bool _isSpraying;
        private readonly AudioEmitter _emitter;
        private readonly ShakeDetector _shakeDetector;
        private IAudioControl? _shakeControl;
        private IAudioControl? _sprayControl;
        private readonly SprayTracker? _tracker;
        private Color _color;
        private SprayRays? _spray;
        private bool _reconstructMode;

        public Can(bool reconstructMode)
        {
            _reconstructMode = reconstructMode; 

            if (reconstructMode)
            {
                _canBody = new TriangleMesh();
                _cap = new TriangleMesh();  
            }
            else
            {
                var mesh = AssetLoader.Instance.Load<Group3D>("res://asset/uploads_files_4848386_spray_can.glb",
                    new GltfLoaderOptions
                    {
                        MaterialFactory = matId => matId == 0 ? new CanMaterial() : new PbrV2Material()
                    });
                _canBody = mesh.FindByName<Object3D>("CanYellow")!;
                _cap = mesh.FindByName<Object3D>("Cap")!;
                _cap.Transform.Orientation = new Quaternion(-6.181724E-08f, 0.70710677f, 0f, 0.70710677f);
            }


            AddChild(_canBody!);
            AddChild(_cap!);

            Transform.SetScale(0.03f);

            _sprayLoop = new AudioLooper
            {
                Loop = LoadAudio("1141558.audio-Air_Burst_Single_Long_02.mp3")
                                  .SubClipTime(0.5f, 1.2f)
                                  .ToMono()
                                  .ToAlAudio(),

                FadeSize = 0.1f
            };

            _shakeLoop = new AudioLooper
            {
                Loop = LoadAudio("333819-WS_Spray_paint_can_shake_fast_with_little_marble_inside.mp3")
                         .SubClipTime(0.2f, 4f)
                         .ToMono()
                         .ToAlAudio(),

                FadeSize = 0.1f
            };


            _emitter = this.AddComponent<AudioEmitter>();
            _tracker = this.AddComponent<SprayTracker>();

            _shakeDetector = this.AddComponent<ShakeDetector>();
            _shakeDetector.OnShakeEnd += OnShakeEnd;
            _shakeDetector.OnShakeStart += OnShakeStart;

            Offset = new Vector3(0, -0.04f, 0);

            Color = new Color(1, 0, 0);

            SoundEnabled = !XrPlatform.IsEditor;
        }

        static string GetAssetPath(string name)
        {
            return Context.Require<IAssetStore>().GetPath(name);
        }

        protected AudioClip LoadAudio(string resPath)
        {
            var fullPath = GetAssetPath(resPath);

            var bytes = Context.Require<IAudioDecoder>().DecodeToPCM(fullPath, out var format);

            return new AudioClip(bytes, format);
        }

        public void Configure(XrEngineApp e)
        {
            _inputs = e.GetInputs<XrOculusTouchController>();

            _shakeDetector!.Configure(_inputs.Right!.GripPose!);
        }

        public override void Update(RenderContext ctx)
        {
            if (_reconstructMode)
                return;

            Debug.Assert(_inputs?.Right != null);

            var pose = _inputs.Right.GripPose;
            var trigger = _inputs.Right.TriggerValue;

            if (pose != null && pose.IsActive)
            {
                this.SetWorldPose(pose.Value.Multiply(new Pose3
                {
                    Position = Offset,
                    Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2) *
                                  Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2)
                }));
            }

            if (trigger != null && trigger.IsActive)
            {
                SprayAperture = trigger.Value;
                if (SprayAperture > 0)
                    _inputs.Right.Haptic!.VibrateStart(20, 0.7f, TimeSpan.FromSeconds(0.5));
                else
                    _inputs.Right.Haptic!.VibrateStop();
            }

            base.Update(ctx);
        }

        protected void OnSprayStart()
        {
            _spray ??= _scene?.Descendants<SprayRays>().First();

            if (SoundEnabled)
                _sprayControl = _emitter!.Play(_sprayLoop!, () => Forward);

            _isSpraying = true;
            _spray?.IsVisible = true;   
        }

        protected void OnSprayEnd()
        {
            _isSpraying = false;
            _sprayControl?.Stop();
            _spray?.IsVisible = false;
        }

        protected virtual void OnShakeEnd()
        {
            _shakeControl?.Stop();
        }

        protected virtual void OnShakeStart()
        {
            _shakeControl = _emitter!.Play(_shakeLoop!, () => Forward);
        }

        [Range(0, 1, 0.05f)]
        public float SprayAperture
        {
            get => _sprayAperture;
            set
            {
                _sprayAperture = value;
                _cap!.Transform.SetPositionY(1.8865331f - _sprayAperture * 0.2f);

                if (!_isSpraying && _sprayAperture > 0)
                    OnSprayStart();
                if (_isSpraying && _sprayAperture == 0)
                    OnSprayEnd();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                if (!_reconstructMode)
                {
                    var mat = (CanMaterial)((TriangleMesh)_canBody).Materials[0];
                    mat.CanColor = _color;
                }
            }
        }

        public Vector3 Offset { get; set; }

        public bool SoundEnabled { get; set; }
    }
}
