using Silk.NET.OpenXR;
using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

namespace OpenXr.Framework.Oculus
{

    public enum XrHandAimFinger
    {
        Index,
        Middle,
        Ring,
        Little
    }

    public struct XrHandAimState
    {
        public XrHandAimFinger Finger;
        public float Value;
        public bool IsPinching;
    }

    public class XrHandInputOculus : XrHandInput
    {
        private readonly OculusXrPlugin _oculus;
        private XrHandMesh? _mesh;
        private HandTrackingDataSourceEXT _dataSource;
        private HandTrackingCapsulesStateFB.CapsulesBuffer _capsules;
        private readonly HandJointVelocityEXT[] _velocities;
        private float _scale;
        private bool _isAimValid;
        private Pose3 _aimPose;
        protected readonly XrHandAimState[] _aimStates = new XrHandAimState[4];

        public XrHandInputOculus(XrApp app) : base(app)
        {
            _oculus = _app.Plugin<OculusXrPlugin>();
            _velocities = new HandJointVelocityEXT[XR_HAND_JOINT_COUNT_EXT];
        }

        public unsafe override HandJointLocationEXT[] LocateHandJoints(Space space, long time)
        {
            var scale = new HandTrackingScaleFB()
            {
                SensorOutput = 1,
                CurrentOutput = 1,
                OverrideValueInput = 1,
                OverrideHandScale = 0,
            };

            var capsuleState = new HandTrackingCapsulesStateFB()
            {
                Type = StructureType.HandTrackingCapsulesStateFB,
                Next = &scale,
            };

            var aimState = new HandTrackingAimStateFB()
            {
                Type = StructureType.HandTrackingAimStateFB,
                Next = &capsuleState
            };

            var dataSource = new HandTrackingDataSourceStateEXT()
            {
                Type = StructureType.HandTrackingDataSourceStateExt,
                Next = &aimState
            };

            var unextrapolatedPoses = new HandTrackingUnextrapolatedPosesMETA()
            {
                Next = &dataSource,
            };

            var unextrapolatedRequest = new HandTrackingUnextrapolatedPosesRequestMETA();

            fixed (HandJointVelocityEXT* pVelo = _velocities)
            {
                var velocities = new HandJointVelocitiesEXT
                {
                    Type = StructureType.HandJointVelocitiesExt,
                    JointCount = XR_HAND_JOINT_COUNT_EXT,
                    JointVelocities = pVelo,
                    Next = UseUnextrapolatedPoses ?
                         &unextrapolatedPoses :
                         unextrapolatedPoses.Next
                };

                var resReq = UseUnextrapolatedPoses ? &unextrapolatedRequest : null;

                var result = LocateHandJoints(space, time, &velocities, resReq);

                if (!_app.ReferenceFrame.IsIdentity())
                {
                    var capsules = capsuleState.Capsules.AsSpan();

                    fixed (HandCapsuleFB* pCap = capsules)
                    {
                        for (var i = 0; i < capsules.Length; i++)
                        {
                            ref var v0 = ref Unsafe.AsRef<Vector3>(&pCap[i].Points.Element0);
                            ref var v1 = ref Unsafe.AsRef<Vector3>(&pCap[i].Points.Element1);
                            v0 = _app.ReferenceFrame.Transform(v0);
                            v1 = _app.ReferenceFrame.Transform(v1);
                        }
                    }
                }

                _isAimValid = (aimState.Status & HandTrackingAimFlagsFB.ValidBitFB) != 0;

                if (_isAimValid)
                    _aimPose = aimState.AimPose.ToPose3();

                _aimStates[0] = new XrHandAimState
                {
                    Finger = XrHandAimFinger.Index,
                    Value = aimState.PinchStrengthIndex,
                    IsPinching = (aimState.Status & HandTrackingAimFlagsFB.IndexPinchingBitFB) != 0
                };

                _aimStates[1] = new XrHandAimState
                {
                    Finger = XrHandAimFinger.Middle,
                    Value = aimState.PinchStrengthMiddle,
                    IsPinching = (aimState.Status & HandTrackingAimFlagsFB.MiddlePinchingBitFB) != 0
                };

                _aimStates[2] = new XrHandAimState
                {
                    Finger = XrHandAimFinger.Ring,
                    Value = aimState.PinchStrengthRing,
                    IsPinching = (aimState.Status & HandTrackingAimFlagsFB.RingPinchingBitFB) != 0
                };

                _aimStates[3] = new XrHandAimState
                {
                    Finger = XrHandAimFinger.Little,
                    Value = aimState.PinchStrengthLittle,
                    IsPinching = (aimState.Status & HandTrackingAimFlagsFB.LittlePinchingBitFB) != 0
                };

                _dataSource = dataSource.IsActive == 1 ? dataSource.DataSource : 0;

                _capsules = capsuleState.Capsules;

                _scale = scale.CurrentOutput;

                return result;
            }
        }

        public void LoadMesh()
        {
            _mesh = _oculus.GetHandMesh(_tracker);
            _mesh.Type = _handType;
        }

        public bool UseUnextrapolatedPoses { get; set; }

        public bool IsAimValid => _isAimValid;

        public Pose3 Aim => _aimPose;

        public Span<HandCapsuleFB> Capsules => _capsules.AsSpan();

        public XrHandAimState[] AimStates => _aimStates;

        public XrHandMesh? Mesh => _mesh;

        public HandTrackingDataSourceEXT DataSource => _dataSource;

        public float Scale => _scale;
    }
}
