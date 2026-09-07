using OpenAl.Framework;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Media;

namespace XrSamples
{
    public sealed class WaterStepAudio : Behavior<Water>
    {
        private static readonly (float Start, float End)[] StepRanges =
        [
            (0.08f, 0.55f),
            (0.55f, 0.96f),
            (0.96f, 1.46f),
            (1.54f, 1.93f),
            (1.93f, 2.48f),
            (2.66f, 3.18f),
            (3.25f, 3.78f),
            (4.24f, 4.82f),
            (4.82f, 5.18f),
            (5.18f, 5.56f),
            (5.56f, 5.98f),
            (6.35f, 6.93f)
        ];

        private readonly List<AudioClip> _stepClips;
        private readonly List<AlBuffer> _steps;
        private WaterInteraction _interaction = null!;
        private AudioEmitter _emitter = null!;
        private float _leftHandTime;
        private float _rightHandTime;
        private float _distanceSinceStep;
        private float _nextStepDistance;
        private float _idleTime;
        private int _lastStep;
        private bool _wasMoving;

        public WaterStepAudio()
        {
            UpdatePriority = 2;
            _stepClips = [];
            _steps = [];
            _lastStep = -1;
            AudioPath = "Audio/audio_3756fcf5fd.mp3";
            TailPadding = 0.3f;
            FadeOutDuration = TailPadding;
            Volume = 1f;
            StartStepDistance = 0.08f;
            StepDistance = 0.52f;
            StepDistanceVariation = 0.08f;
        }

        private void PrepareSteps()
        {
            var path = Context.Require<IAssetStore>().GetPath(AudioPath);
            var pcm = Context.Require<IAudioDecoder>().DecodeToPCM(path, out var format);
            var clip = new AudioClip(pcm, format).ToMono();

            foreach (var range in StepRanges)
            {
                var end = MathF.Min(range.End + TailPadding, clip.Range.EndTime);
                var step = clip.SubClipTime(range.Start, end);
                var samples = step.ToFloat();
                var fadeSamples = Math.Min(samples.Length, (int)(step.Format.SampleRate * FadeOutDuration));

                for (var i = 0; i < fadeSamples; i++)
                {
                    var t = i / (float)Math.Max(fadeSamples - 1, 1);
                    var gain = 1f - t * t * (3f - 2f * t);
                    samples[samples.Length - fadeSamples + i] *= gain;
                }

                _stepClips.Add(AudioClip.FromFloats(samples, step.Format));
            }
        }

        protected override void Start(RenderContext ctx)
        {
            _interaction = _host.Interaction;
            _emitter = _interaction.State.Player.EnsureComponent<AudioEmitter>();

            if (_stepClips.Count == 0)
                PrepareSteps();

            var al = _host.Scene!.Component<AudioSystem>().Device.Al;

            foreach (var clip in _stepClips)
            {
                var buffer = new AlBuffer(al);
                buffer.SetData(clip.ToAlAudio());
                _steps.Add(buffer);
            }

            _nextStepDistance = RandomStepDistance();
            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            var deltaTime = MathF.Max((float)ctx.DeltaTime, 0.0001f);
            var state = _interaction.State;
            var isMoving = state.PlayerMotion > 0;

            if (isMoving)
            {
                _idleTime = 0;
                _distanceSinceStep += state.PlayerDistance;

                if (!_wasMoving && _distanceSinceStep >= StartStepDistance)
                {
                    PlayStep();
                    _wasMoving = true;
                }
                else if (_wasMoving && _distanceSinceStep >= _nextStepDistance)
                {
                    PlayStep();
                }
            }
            else
            {
                _idleTime += deltaTime;

                if (_idleTime >= 0.2f)
                {
                    _distanceSinceStep = 0;
                    _wasMoving = false;
                }
            }

            if (state.PlayerTeleported)
            {
                _distanceSinceStep = 0;
                _wasMoving = false;
            }

            UpdateHand(state.LeftHandMotion, deltaTime, ref _leftHandTime);
            UpdateHand(state.RightHandMotion, deltaTime, ref _rightHandTime);
            base.Update(ctx);
        }

        private void UpdateHand(float motion, float deltaTime, ref float time)
        {
            time = MathF.Max(0, time - deltaTime);

            if (motion > 0 && time == 0)
            {
                PlaySplash();
                time = 0.3f;
            }
        }

        private void PlayStep()
        {
            PlaySplash();
            _distanceSinceStep = 0;
            _nextStepDistance = RandomStepDistance();
        }

        private void PlaySplash()
        {
            if (_steps.Count == 0)
                return;

            var step = Random.Shared.Next(_steps.Count);

            if (_steps.Count > 1 && step == _lastStep)
                step = (step + Random.Shared.Next(1, _steps.Count)) % _steps.Count;

            var source = _emitter.Play(_steps[step], _interaction.State.Player.Forward);

            source.Gain = Volume;
            source.Pitch = 0.96f + Random.Shared.NextSingle() * 0.08f;
            source.ReferenceDistance = 2f;
            source.RolloffFactor = 0.25f;

            _lastStep = step;
        }

        private float RandomStepDistance()
        {
            return StepDistance + (Random.Shared.NextSingle() * 2f - 1f) * StepDistanceVariation;
        }

        public override void Reset(bool onlySelf = false)
        {
            foreach (var step in _steps)
                step.Dispose();

            _steps.Clear();
            _distanceSinceStep = 0;
            _idleTime = 0;
            _wasMoving = false;
            _leftHandTime = 0;
            _rightHandTime = 0;
            base.Reset(onlySelf);
        }

        public string AudioPath { get; }

        public float TailPadding { get; }

        public float FadeOutDuration { get; }

        public float Volume { get; set; }

        public float StartStepDistance { get; set; }

        public float StepDistance { get; set; }

        public float StepDistanceVariation { get; set; }
    }
}
