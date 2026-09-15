using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using XrMath;

namespace XrEngine.OpenXr
{
    public class OculusEnvRayCollider : IEnvRayCollider, IDisposable
    {
        XrEnvironmentRaycaster? _caster;
        XrApp? _app;
        Task? _createTask;
        bool _isEnabled;

        public bool CastRay(Ray3 ray, float maxDistance, out Pose3 result)
        {
            result = default;

            EnsureInitialized();

            if (!_isEnabled || _caster == null || _createTask == null || !_createTask.IsCompletedSuccessfully)
                return false;

            var hit = _caster.Raycast(ray.Origin, ray.Direction, maxDistance);

            if (!hit.HasHit)
                return false;

            result = hit.Pose;
            return true;
        }

        private void EnsureInitialized()
        {
            if (!_isEnabled)
                return;

            var app = XrApp.Current;

            if (app == null || !app.IsStarted)
                return;

            if (_app != app)
            {
                _caster?.Dispose();
                _app?.SessionChanged += OnSessionChanged;

                _app = app;
                _caster = new XrEnvironmentRaycaster(app);
                _createTask = null;
            }

            _createTask ??= _caster!.CreateAsync();
        }

        private void OnSessionChanged()
        {
            if (_app!.State == XrAppState.Stopped)
            {
                _caster?.Dispose();
                _caster = null; 
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;

                _isEnabled = value;

                if (value)
                    EnsureInitialized();
                else
                {
                    _createTask = null;
                    _caster?.Destroy();
                }
            }
        }

        public void Dispose()
        {
            _isEnabled = false;
            _createTask = null;

            _caster?.Dispose();
            _caster = null;
            _app = null;

            GC.SuppressFinalize(this);
        }
    }
}