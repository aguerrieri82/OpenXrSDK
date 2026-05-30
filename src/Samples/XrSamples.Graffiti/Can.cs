using OpenAl.Framework;
using OpenXr.Framework.Oculus;

using XrEngine;
using XrEngine.Media;
using XrEngine.OpenXr;
using XrEngine.Audio;
using XrMath;
using System.Numerics;
using System.Diagnostics;

namespace XrSamples.Graffiti
{


    public class Can : Group3D
    {
        private Object3D? _canBody;
        private Object3D? _cap;
        private float _sprayAperture;
        private XrOculusTouchController? _inputs;
        private AudioLooper? _shakeLoop;
        private AudioLooper? _sprayLoop;

        private bool _isSpraying;
        private AudioEmitter? _emitter;
        private ShakeDetector? _shakeDetector;
        private IAudioControl? _shakeControl;
        private IAudioControl? _sprayControl;

        public Can()
        {
            Load();
        }


        public void Load()
        {
            var mesh = (Group3D)AssetLoader.Instance.Load(new Uri("res://asset/uploads_files_4848386_spray_can.glb"), typeof(Group3D), null);
            _canBody = mesh.FindByName<Object3D>("CanYellow");
            _cap = mesh.FindByName<Object3D>("Cap");
            
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

            _shakeDetector = this.AddComponent<ShakeDetector>();
            _shakeDetector.OnShakeEnd += OnShakeEnd;
            _shakeDetector.OnShakeStart += OnShakeStart;
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
            Debug.Assert(_inputs?.Right != null);

            var pose = _inputs.Right.GripPose;
            var trigger = _inputs.Right.TriggerValue;

            if (pose != null && pose.IsActive)
            {
                this.SetWorldPose(pose.Value.Multiply(new Pose3
                {
                    Position = new Vector3(0, 0, -0.02f),
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
            _sprayControl = _emitter!.Play(_sprayLoop!, () => Forward);
            _isSpraying = true;
        }

        protected void OnSprayEnd()
        {
            _isSpraying = false;
            _sprayControl?.Stop();
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
    }
}
