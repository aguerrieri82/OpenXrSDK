using Silk.NET.OpenXR;
using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

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

                if (!_app.ReferenceFrame.IsIdentity())
                {
                    var capsules = capsuleState.Capsules.AsSpan();

                    fixed (HandCapsuleFB* pCap = capsules)
                    {
                        for (var i = 0; i < capsules.Length; i++)
                        {
                            ref Vector3 v0 = ref Unsafe.AsRef<Vector3>(&pCap->Points.Element0);
                            ref Vector3 v1 = ref Unsafe.AsRef<Vector3>(&pCap->Points.Element1);
                            v0 = _app.ReferenceFrame.Transform(v0);
                            v1 = _app.ReferenceFrame.Transform(v1);
                        }
                    }
                }

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
