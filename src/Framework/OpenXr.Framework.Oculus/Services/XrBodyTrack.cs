using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.FB;

namespace OpenXr.Framework.Oculus
{
    public class XrBodyTrack : IDisposable
    {
        private FBBodyTracking? _bodyTracking;
        private METABodyTrackingFidelity? _bodyTrackingFidelity;
        private METABodyTrackingCalibration? _bodyTrackingCalibration;

        private BodyTrackerFB _tracker;
        private uint _jointCount;
        private bool _isActive;
        private BodyJointLocationFB[]? _joints;
        private long _time;
        private float _confidence;
        private int _skeletonChanges = -1;
        private BodySkeletonJointFB[]? _skeleton;

        private BodyTrackingFidelityMETA? _fidelity;
        private BodyTrackingCalibrationStateMETA? _calibrationStatus;

        readonly XrApp _app;

        public XrBodyTrack(XrApp app)
        {
            _app = app;
        }

        protected void Initialize()
        {
            if (_bodyTracking != null)
                return;

            if (!_app.Xr.TryGetInstanceExtension<FBBodyTracking>(null, _app.Instance, out _bodyTracking))
                throw new NotSupportedException();

            if (_app.HasExtension(METABodyTrackingFidelity.ExtensionName))
                _bodyTrackingFidelity = new METABodyTrackingFidelity(_app.Xr, _app.Instance);

            if (_app.HasExtension(METABodyTrackingCalibration.ExtensionName))
                _bodyTrackingCalibration = new METABodyTrackingCalibration(_app.Xr, _app.Instance);
        }

        public bool IsSupported(BodyJointSetFB jointSet)
        {
            if (jointSet == BodyJointSetFB.FullBodyMeta)
            {
                var fullProps = new SystemPropertiesBodyTrackingFullBodyMETA
                {
                    Type = StructureType.SystemPropertiesBodyTrackingFullBodyMeta
                };

                _app.GetSystemProperties(ref fullProps);

                return fullProps.SupportsFullBodyTracking != 0;
            }

            var bodyProps = new SystemBodyTrackingPropertiesFB
            {
                Type = StructureType.SystemBodyTrackingPropertiesFB
            };

            _app.GetSystemProperties(ref bodyProps);

            return bodyProps.SupportsBodyTracking != 0;
        }

        public bool IsFidelitySupported()
        {
            Initialize();

            if (_bodyTrackingFidelity == null)
                return false;

            var props = new SystemPropertiesBodyTrackingFidelityMETA
            {
                Type = METABodyTrackingFidelity.TypeSystemPropertiesBodyTrackingFidelityMeta
            };

            _app.GetSystemProperties(ref props);

            return props.SupportsBodyTrackingFidelity != 0;
        }

        public void RequestFidelity(BodyTrackingFidelityMETA fidelity)
        {
            _app.CheckResult(_bodyTrackingFidelity!.RequestBodyTrackingFidelityMETA(_tracker, fidelity), "RequestBodyTrackingFidelityMETA");
        }

        public void SuggestHeight(float bodyHeight)
        {
            var info = new BodyTrackingCalibrationInfoMETA
            {
                Type = StructureType.BodyTrackingCalibrationInfoMeta,
                BodyHeight = bodyHeight
            };

            _app.CheckResult(_bodyTrackingCalibration!.SuggestBodyTrackingCalibrationOverrideMETA(_tracker, ref info), "SuggestBodyTrackingCalibrationOverrideMETA");
        }

        public void ResetCalibration()
        {
            _app.CheckResult(_bodyTrackingCalibration!.ResetBodyTrackingCalibrationMETA(_tracker), "ResetBodyTrackingCalibrationMETA");
        }

        public bool IsCalibrationSupported()
        {
            Initialize();

            if (_bodyTrackingCalibration == null)
                return false;

            var props = new SystemPropertiesBodyTrackingCalibrationMETA
            {
                Type = StructureType.SystemPropertiesBodyTrackingCalibrationMeta
            };

            _app.GetSystemProperties(ref props);

            return props.SupportsHeightOverride != 0;
        }

        public void Create(BodyJointSetFB jointSet)
        {
            Initialize();

            if (!IsSupported(jointSet))
                throw new Exception("Body tracking not supported");

            var info = new BodyTrackerCreateInfoFB()
            {
                Type = StructureType.BodyTrackerCreateInfoFB,
                BodyJointSet = jointSet,
            };

            var result = new BodyTrackerFB();

            _app.CheckResult(_bodyTracking!.CreateBodyTrackerFB(_app.Session, ref info, ref result), "CreateBodyTrackerFB");

            _tracker = result;
            _jointCount = jointSet == BodyJointSetFB.FullBodyMeta
                ? (uint)FullBodyJointMETA.CountMeta
                : (uint)BodyJointFB.CountFB;
            _skeletonChanges = -1;
        }

        public unsafe BodySkeletonJointFB[] GetSkeleton()
        {
            var result = new BodySkeletonFB()
            {
                Type = StructureType.BodySkeletonFB,
                JointCount = _jointCount
            };

            var joints = new BodySkeletonJointFB[result.JointCount];

            fixed (BodySkeletonJointFB* pJoints = joints)
            {
                result.Joints = pJoints;
                _app.CheckResult(_bodyTracking!.GetBodySkeletonFB(_tracker, ref result), "GetBodySkeletonFB");
            }

            return joints;
        }

        public unsafe BodyJointLocationFB[] LocateJoints(Space space, long time)
        {
            var info = new BodyJointsLocateInfoFB
            {
                Type = StructureType.BodyJointsLocateInfoFB,
                BaseSpace = space,
                Time = time
            };

            var result = new BodyJointLocationsFB
            {
                Type = StructureType.BodyJointLocationsFB,
                JointCount = _jointCount
            };

            var calibrationStatus = new BodyTrackingCalibrationStatusMETA
            {
                Type = StructureType.BodyTrackingCalibrationStatusMeta
            };

            var fidelityStatus = new BodyTrackingFidelityStatusMETA
            {
                Type = METABodyTrackingFidelity.TypeBodyTrackingFidelityStatusMeta
            };

            if (_bodyTrackingCalibration != null)
                StructChain.AddNextStruct(ref result, &calibrationStatus);

            if (_bodyTrackingFidelity != null)
                StructChain.AddNextStruct(ref result, &fidelityStatus);

            var joints = new BodyJointLocationFB[result.JointCount];

            fixed (BodyJointLocationFB* pJoints = joints)
            {
                result.JointLocations = pJoints;

                _app.CheckResult(_bodyTracking!.LocateBodyJointsFB(_tracker, ref info, ref result), "LocateBodyJointsFB");
            }

            _joints = joints;
            _isActive = result.IsActive != 0;
            _time = result.Time;
            _confidence = result.Confidence;

            if (_bodyTrackingCalibration != null)
                _calibrationStatus = calibrationStatus.Status;

            if (_bodyTrackingFidelity != null)
                _fidelity = fidelityStatus.Fidelity;

            if (_skeletonChanges != result.SkeletonChangedCount)
            {
                _skeleton = GetSkeleton();
                _skeletonChanges = (int)result.SkeletonChangedCount;
            }

            return _joints;
        }

        public void Destroy()
        {
            if (_tracker.Handle == 0)
                return;

            if (_app.Session.Handle != 0)
                _app.CheckResult(_bodyTracking!.DestroyBodyTrackerFB(_tracker), "DestroyBodyTrackerFB");

            _tracker.Handle = 0;
        }

        public void Dispose()
        {
            Destroy();

            GC.SuppressFinalize(this);
        }

        public BodySkeletonJointFB[]? Skeleton => _skeleton;

        public float Confidence => _confidence;

        public long PoseTime => _time;

        public bool IsActive => _isActive;

        public BodyJointLocationFB[]? Joints => _joints;

        public BodyTrackingFidelityMETA? Fidelity => _fidelity;

        public BodyTrackingCalibrationStateMETA? CalibrationStatus => _calibrationStatus;
    }
}
