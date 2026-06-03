
using OpenXr.Framework;
using System.Numerics;
using XrEngine;

namespace XrSamples.Graffiti
{
    public class ShakeDetector : Behavior<Object3D>
    {
        private bool _isShaking;
        private Vector3 _lastLinearVelocity;
        private Vector3 _lastAngularVelocity;
        private float _shakeWindowTimer;
        private float _endTimer;
        private int _shakeImpulseCount;
        private bool _hasLastSample;
        private XrPoseInput? _input;

        public ShakeDetector()
        {
            LinearThreshold = 0.20f;
            AngularThreshold = 3.0f;

            DirectionChangeThreshold = 0.25f;
            RequiredShakeImpulses = 2;

            ShakeWindowSeconds = 0.55f;
            EndDelaySeconds = 0.35f;

            LinearWeight = 1.0f;
            AngularWeight = 1.25f;

            HardAngularThreshold = 10.0f;
            HardLinearThreshold = 2.0f;
        }

        public void Configure(XrPoseInput input)
        {
            _input = input;
        }

        protected override void Update(RenderContext ctx)
        {
            if (_input != null && _input.IsActive && _input.LinearVelocity != null && _input.AngularVelocity != null)
                Update(_input.LinearVelocity.Value, _input.AngularVelocity.Value, (float)ctx.DeltaTime);
        }

        protected void Update(
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            if (!_hasLastSample)
            {
                _lastLinearVelocity = linearVelocity;
                _lastAngularVelocity = angularVelocity;
                _hasLastSample = true;
                return;
            }

            var linearSpeed = linearVelocity.Length();
            var angularSpeed = angularVelocity.Length();

            //Log.Value("Linear", linearSpeed);
            //Log.Value("Angular", angularSpeed);

            var strongLinearMotion = linearSpeed >= LinearThreshold;
            var strongAngularMotion = angularSpeed >= AngularThreshold;

            var strongMotion = strongLinearMotion || strongAngularMotion;

            var linearDirectionChanged =
                HasDirectionChanged(
                    _lastLinearVelocity,
                    linearVelocity,
                    DirectionChangeThreshold);

            var angularDirectionChanged =
                HasDirectionChanged(
                    _lastAngularVelocity,
                    angularVelocity,
                    DirectionChangeThreshold);

            var directionChanged =
                linearDirectionChanged || angularDirectionChanged;

            var shakeScore =
                linearSpeed * LinearWeight +
                angularSpeed * AngularWeight;

            var scoreThreshold =
                LinearThreshold * LinearWeight +
                AngularThreshold * AngularWeight;

            var hardAngularHit = angularSpeed >= HardAngularThreshold;
            var hardLinearHit = linearSpeed >= HardLinearThreshold;

            var hardShakeHit = hardAngularHit || hardLinearHit;

            var repeatedShakeImpulse =
                strongMotion &&
                directionChanged &&
                shakeScore >= scoreThreshold * 0.75f;

            var shakeImpulse = repeatedShakeImpulse || hardShakeHit;

            if (shakeImpulse)
            {
                _shakeImpulseCount++;
                _shakeWindowTimer = ShakeWindowSeconds;
                _endTimer = EndDelaySeconds;
            }
            else
            {
                _shakeWindowTimer -= deltaTime;

                if (_shakeWindowTimer <= 0f)
                {
                    _shakeWindowTimer = 0f;
                    _shakeImpulseCount = 0;
                }

                if (_isShaking)
                    _endTimer -= deltaTime;
            }

            if (!_isShaking && _shakeImpulseCount >= RequiredShakeImpulses)
            {
                _isShaking = true;
                OnShakeStart?.Invoke();
            }

            if (_isShaking && _endTimer <= 0f)
            {
                _isShaking = false;
                _shakeImpulseCount = 0;
                _shakeWindowTimer = 0f;
                _endTimer = 0f;

                OnShakeEnd?.Invoke();
            }

            _lastLinearVelocity = linearVelocity;
            _lastAngularVelocity = angularVelocity;
        }

        private static bool HasDirectionChanged(
            Vector3 previous,
            Vector3 current,
            float dotThreshold)
        {
            var previousLengthSq = previous.LengthSquared();
            var currentLengthSq = current.LengthSquared();

            if (previousLengthSq < 0.0001f || currentLengthSq < 0.0001f)
                return false;

            var previousDir = Vector3.Normalize(previous);
            var currentDir = Vector3.Normalize(current);

            var dot = Vector3.Dot(previousDir, currentDir);

            return dot <= dotThreshold;
        }

        public void Reset()
        {
            _isShaking = false;
            _hasLastSample = false;

            _lastLinearVelocity = Vector3.Zero;
            _lastAngularVelocity = Vector3.Zero;

            _shakeWindowTimer = 0f;
            _endTimer = 0f;
            _shakeImpulseCount = 0;
        }


        public float LinearThreshold { get; set; }
        public float AngularThreshold { get; set; }

        public float DirectionChangeThreshold { get; set; }
        public int RequiredShakeImpulses { get; set; }

        public float ShakeWindowSeconds { get; set; }
        public float EndDelaySeconds { get; set; }

        public float LinearWeight { get; set; }
        public float AngularWeight { get; set; }

        public float HardAngularThreshold { get; set; }
        public float HardLinearThreshold { get; set; }

        public bool IsShaking => _isShaking;

        public event Action? OnShakeStart;

        public event Action? OnShakeEnd;

    }
}
