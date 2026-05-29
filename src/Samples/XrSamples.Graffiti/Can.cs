using OpenAl.Framework;
using OpenXr.Framework.Oculus;
using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;
using XrEngine.Media;
using XrEngine.OpenXr;
using XrEngine.Audio;
using XrMath;
using System.Numerics;

namespace XrSamples.Graffiti
{
    public class Can : Group3D
    {
        private Object3D? _canBody;
        private Object3D? _cap;
        private float _sprayAperture;
        private XrOculusTouchController? _inputs;
        private AudioLooper? _sprayLoop;
        private bool _isSpraying;
        private AudioEmitter? _emitter;

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

            _sprayLoop = new AudioLooper();

            _sprayLoop.Loop = LoadAudio("1141558.audio-Air_Burst_Single_Long_02.mp3")
                              .SubClipTime(0.5f, 1.2f)
                              .ToMono()
                              .ToAlAudio();

            _sprayLoop.FadeSize = 0.1f;

            _emitter = this.AddComponent<AudioEmitter>();
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

        }

        public override void Update(RenderContext ctx)
        {
            var pose = _inputs?.Right?.GripPose;
            var trigger = _inputs?.Right?.TriggerValue;

            if (pose != null && pose.IsActive)
            {
                this.SetWorldPose(pose.Value.Multiply(new Pose3
                {
                    Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2) *
                                  Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2)
                }));
            }

            if (trigger != null && trigger.IsActive)
            {
                SprayAperture = trigger.Value;
                if (SprayAperture > 0)
                    _inputs.Right.Haptic.VibrateStart(20, 0.7f, TimeSpan.FromSeconds(0.5));
                else
                    _inputs.Right.Haptic.VibrateStop();
            }

            base.Update(ctx);
        }

        protected void OnSprayStart()
        {
            _emitter!.Play(_sprayLoop, () => this.Forward);
            _isSpraying = true;
        }

        protected void OnSprayEnd()
        {
            _isSpraying = false;
            _emitter!.Stop();
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
