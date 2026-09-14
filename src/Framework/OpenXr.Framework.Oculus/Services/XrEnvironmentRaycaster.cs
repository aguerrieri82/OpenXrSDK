using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;


namespace OpenXr.Framework.Oculus
{
    public struct XrEnvironmentRaycastHit
    {
        public EnvironmentRaycastHitStatusMETA Status;
        public Pose3 Pose;

        public bool HasHit => Status == EnvironmentRaycastHitStatusMETA.HitMeta;
    }

    public class XrEnvironmentRaycaster : IDisposable
    {
        readonly XrApp _app;
        readonly METAEnvironmentRaycast _ext;

        EnvironmentRaycasterMETA _raycaster;

        public XrEnvironmentRaycaster(XrApp app)
        {
            _app = app;
            _ext = new METAEnvironmentRaycast(app.Xr, app.Instance);
        }

        public bool IsSupported()
        {
            var props = new SystemEnvironmentRaycastPropertiesMETA();

            _app.GetSystemProperties(ref props);

            return props.SupportsEnvironmentRaycast != 0;
        }

        public async Task CreateAsync()
        {
            if (!IsSupported())
                throw new NotSupportedException();

            var info = new EnvironmentRaycasterCreateInfoMETA();
            var future = new FutureEXT();

            _app.CheckResult(
                _ext.CreateEnvironmentRaycasterAsyncMETA(_app.Session, ref info, ref future),
                "CreateEnvironmentRaycasterAsyncMETA");

            await _app.WaitFutureAsync(future);

            var completion = new EnvironmentRaycasterCreateCompletionMETA();

            _app.CheckResult(
                _ext.CreateEnvironmentRaycasterCompleteMETA(_app.Session, future, ref completion),
                "CreateEnvironmentRaycasterCompleteMETA");

            _app.CheckResult(completion.FutureResult, "CreateEnvironmentRaycaster");

            _raycaster = completion.EnvironmentRaycaster;
        }

        public XrEnvironmentRaycastHit Raycast(Vector3 origin, Vector3 direction, float? maxDistance = null)
        {
            return Raycast(origin, direction, _app.ReferenceSpace, 0, maxDistance);
        }

        public unsafe XrEnvironmentRaycastHit Raycast(
            Vector3 origin,
            Vector3 direction,
            Space space,
            long time = 0,
            float? maxDistance = null)
        {
            EnvironmentRaycastFilterDistanceMETA distanceFilter;
            EnvironmentRaycastFilterBaseHeaderMETA* filterPtr;
            EnvironmentRaycastFilterBaseHeaderMETA** filters = null;
            uint filterCount = 0;

            if (maxDistance.HasValue)
            {
                distanceFilter = new EnvironmentRaycastFilterDistanceMETA
                {
                    MaxDistance = maxDistance.Value
                };

                filterPtr = (EnvironmentRaycastFilterBaseHeaderMETA*)&distanceFilter;
                filters = &filterPtr;
                filterCount = 1;
            }

            var info = new EnvironmentRaycastHitGetInfoMETA
            {
                BaseSpace = space,
                Time = time == 0 ? _app.FramePredictedDisplayTime : time,
                Origin = origin.ToVector3f(),
                Direction = direction.ToVector3f(),
                FilterCount = filterCount,
                Filters = filters
            };

            var hit = new EnvironmentRaycastHitMETA();

            _app.CheckResult(
                _ext.PerformEnvironmentRaycastMETA(_raycaster, ref info, ref hit),
                "PerformEnvironmentRaycastMETA");

            return new XrEnvironmentRaycastHit
            {
                Status = hit.Status,
                Pose = hit.Pose.ToPose3()
            };
        }

        public void Destroy()
        {
            if (_raycaster.Handle == 0)
                return;

            _app.CheckResult(
                _ext.DestroyEnvironmentRaycasterMETA(_raycaster),
                "DestroyEnvironmentRaycasterMETA");

            _raycaster.Handle = 0;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        public bool IsCreated => _raycaster.Handle != 0;

        public static implicit operator EnvironmentRaycasterMETA(XrEnvironmentRaycaster value)
        {
            return value._raycaster;
        }
    }
}