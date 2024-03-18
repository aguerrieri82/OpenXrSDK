using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{
    public class XrHandInputMesh : XrHandInput
    {
        private readonly OculusXrPlugin _oculus;
        private XrHandMesh? _mesh;
        private HandTrackingCapsulesStateFB.CapsulesBuffer _capsules;
        private readonly HandJointVelocityEXT[] _velocities;
        private float _scale;

        public XrHandInputMesh(XrApp app) : base(app)
        {
            _oculus = _app.Plugin<OculusXrPlugin>();
            _velocities = new HandJointVelocityEXT[XR_HAND_JOINT_COUNT_EXT];
        }

        public unsafe override HandJointLocationEXT[] LocateHandJoints(Space space, long time)
        {
            var scale = new HandTrackingScaleFB
            {
                Type = StructureType.HandTrackingScaleFB,
                SensorOutput = 1,
                CurrentOutput = 1,
                OverrideValueInput = 1,
                OverrideHandScale = 0,
            };

            var capsuleState = new HandTrackingCapsulesStateFB
            {
                Type = StructureType.HandTrackingCapsulesStateFB,
                Next = &scale,
            };

            var aimState = new HandTrackingAimStateFB
            {
                Type = StructureType.HandTrackingAimStateFB,
                Next = &capsuleState
            };

            fixed (HandJointVelocityEXT* pVelo = _velocities)
            {
                var velocities = new HandJointVelocitiesEXT
                {
                    Type = StructureType.HandJointVelocitiesExt,
                    Next = &aimState,
                    JointCount = XR_HAND_JOINT_COUNT_EXT,
                    JointVelocities = pVelo
                };

                var result = LocateHandJoints(space, time, &velocities);

                _capsules = capsuleState.Capsules;

                _scale = scale.CurrentOutput;

                return result;
            }
        }

        public void LoadMesh()
        {
            _mesh = _oculus.GetHandMesh(_tracker);
        }

        public Span<HandCapsuleFB> Capsules => _capsules.AsSpan();

        public XrHandMesh? Mesh => _mesh;

        public float Scale => _scale;
    }
}
