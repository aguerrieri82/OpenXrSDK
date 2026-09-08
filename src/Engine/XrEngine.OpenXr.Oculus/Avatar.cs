using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr.Oculus
{
    public partial class Avatar : Group3D
    {
        private static readonly Dictionary<FullBodyJointMETA, string> _bodyMap = new()
        {
            [FullBodyJointMETA.RootMeta] = "root_joint",
            [FullBodyJointMETA.HipsMeta] = "pelvis_joint",
            [FullBodyJointMETA.SpineLowerMeta] = "spineLower_joint",
            [FullBodyJointMETA.SpineMiddleMeta] = "spineMiddle_joint",
            [FullBodyJointMETA.SpineUpperMeta] = "spineUpper_joint",
            [FullBodyJointMETA.ChestMeta] = "chest_joint",
            [FullBodyJointMETA.NeckMeta] = "neck_joint",
            [FullBodyJointMETA.HeadMeta] = "head_joint",

            [FullBodyJointMETA.LeftShoulderMeta] = "clavicle_left_joint",
            [FullBodyJointMETA.LeftArmUpperMeta] = "shoulder_left_joint",
            [FullBodyJointMETA.LeftArmLowerMeta] = "elbow_left_joint",
            [FullBodyJointMETA.LeftHandWristMeta] = "handWrist_left_joint",

            [FullBodyJointMETA.RightShoulderMeta] = "clavicle_right_joint",
            [FullBodyJointMETA.RightArmUpperMeta] = "shoulder_right_joint",
            [FullBodyJointMETA.RightArmLowerMeta] = "elbow_right_joint",
            [FullBodyJointMETA.RightHandWristMeta] = "handWrist_right_joint",

            [FullBodyJointMETA.LeftHandThumbMetacarpalMeta] = "handThumbMeta_left_joint",
            [FullBodyJointMETA.LeftHandThumbProximalMeta] = "handThumb_00_left_joint",
            [FullBodyJointMETA.LeftHandThumbDistalMeta] = "handThumb_01_left_joint",
            [FullBodyJointMETA.LeftHandThumbTipMeta] = "handThumb_02_left_joint",

            [FullBodyJointMETA.LeftHandIndexMetacarpalMeta] = "handIndexMeta_left_joint",
            [FullBodyJointMETA.LeftHandIndexProximalMeta] = "handIndex_00_left_joint",
            [FullBodyJointMETA.LeftHandIndexIntermediateMeta] = "handIndex_01_left_joint",
            [FullBodyJointMETA.LeftHandIndexDistalMeta] = "handIndex_02_left_joint",

            [FullBodyJointMETA.LeftHandMiddleMetacarpalMeta] = "handMiddleMeta_left_joint",
            [FullBodyJointMETA.LeftHandMiddleProximalMeta] = "handMiddle_00_left_joint",
            [FullBodyJointMETA.LeftHandMiddleIntermediateMeta] = "handMiddle_01_left_joint",
            [FullBodyJointMETA.LeftHandMiddleDistalMeta] = "handMiddle_02_left_joint",

            [FullBodyJointMETA.LeftHandRingMetacarpalMeta] = "handRingMeta_left_joint",
            [FullBodyJointMETA.LeftHandRingProximalMeta] = "handRing_00_left_joint",
            [FullBodyJointMETA.LeftHandRingIntermediateMeta] = "handRing_01_left_joint",
            [FullBodyJointMETA.LeftHandRingDistalMeta] = "handRing_02_left_joint",

            [FullBodyJointMETA.LeftHandLittleMetacarpalMeta] = "handPinkyMeta_left_joint",
            [FullBodyJointMETA.LeftHandLittleProximalMeta] = "handPinky_00_left_joint",
            [FullBodyJointMETA.LeftHandLittleIntermediateMeta] = "handPinky_01_left_joint",
            [FullBodyJointMETA.LeftHandLittleDistalMeta] = "handPinky_02_left_joint",

            [FullBodyJointMETA.RightHandThumbMetacarpalMeta] = "handThumbMeta_right_joint",
            [FullBodyJointMETA.RightHandThumbProximalMeta] = "handThumb_00_right_joint",
            [FullBodyJointMETA.RightHandThumbDistalMeta] = "handThumb_01_right_joint",
            [FullBodyJointMETA.RightHandThumbTipMeta] = "handThumb_02_right_joint",

            [FullBodyJointMETA.RightHandIndexMetacarpalMeta] = "handIndexMeta_right_joint",
            [FullBodyJointMETA.RightHandIndexProximalMeta] = "handIndex_00_right_joint",
            [FullBodyJointMETA.RightHandIndexIntermediateMeta] = "handIndex_01_right_joint",
            [FullBodyJointMETA.RightHandIndexDistalMeta] = "handIndex_02_right_joint",

            [FullBodyJointMETA.RightHandMiddleMetacarpalMeta] = "handMiddleMeta_right_joint",
            [FullBodyJointMETA.RightHandMiddleProximalMeta] = "handMiddle_00_right_joint",
            [FullBodyJointMETA.RightHandMiddleIntermediateMeta] = "handMiddle_01_right_joint",
            [FullBodyJointMETA.RightHandMiddleDistalMeta] = "handMiddle_02_right_joint",

            [FullBodyJointMETA.RightHandRingMetacarpalMeta] = "handRingMeta_right_joint",
            [FullBodyJointMETA.RightHandRingProximalMeta] = "handRing_00_right_joint",
            [FullBodyJointMETA.RightHandRingIntermediateMeta] = "handRing_01_right_joint",
            [FullBodyJointMETA.RightHandRingDistalMeta] = "handRing_02_right_joint",

            [FullBodyJointMETA.RightHandLittleMetacarpalMeta] = "handPinkyMeta_right_joint",
            [FullBodyJointMETA.RightHandLittleProximalMeta] = "handPinky_00_right_joint",
            [FullBodyJointMETA.RightHandLittleIntermediateMeta] = "handPinky_01_right_joint",
            [FullBodyJointMETA.RightHandLittleDistalMeta] = "handPinky_02_right_joint",

            [FullBodyJointMETA.LeftUpperLegMeta] = "hip_left_joint",
            [FullBodyJointMETA.LeftLowerLegMeta] = "knee_left_joint",
            [FullBodyJointMETA.LeftFootAnkleMeta] = "footAnkle_left_joint",
            [FullBodyJointMETA.LeftFootBallMeta] = "footBall_left_joint",

            [FullBodyJointMETA.RightUpperLegMeta] = "hip_right_joint",
            [FullBodyJointMETA.RightLowerLegMeta] = "knee_right_joint",
            [FullBodyJointMETA.RightFootAnkleMeta] = "footAnkle_right_joint",
            [FullBodyJointMETA.RightFootBallMeta] = "footBall_right_joint",
        };

        protected XrBodyTrack? _bodyTrack;
        protected XrApp? _xrApp;
        protected Joint3D?[]? _bodyJoints;
        protected int[] _jointUpdateOrder = [];

        protected BodySkeletonJointFB[]? _bodySkeleton;
        protected Quaternion[]? _avatarBindWorldOrientations;
        protected Quaternion[]? _trackingBindWorldOrientations;
        protected Quaternion[]? _avatarBindLocalOrientations;

        private static readonly Quaternion _bodyBasis =
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);

        protected override void UpdateSelf(RenderContext ctx)
        {
            _xrApp ??= XrApp.Current;

            if (_xrApp != null && _xrApp.IsStarted)
            {
                if (_bodyTrack == null)
                {
                    _bodyTrack = new XrBodyTrack(_xrApp);
                    _bodyTrack.Create(BodyJointSetFB.FullBodyMeta);

                    BindBodyJoints();
                }

                var locations = _bodyTrack.LocateJoints(
                    _xrApp.ReferenceSpace,
                    _xrApp.FramePredictedDisplayTime);

                if (_bodyTrack.IsActive)
                    UpdateBodyJoints(locations);
            }

            base.UpdateSelf(ctx);

            if (DumpRuntimeDiagnosticsRequested && _bodyTrack?.IsActive == true)
            {
                DumpRuntimeDiagnosticsRequested = false;
                LogRuntimeDiagnostics(includeVertices: false);
            }
        }

        protected void BindBodyJoints()
        {
            CaptureDiagnosticBindPose();

            var joints = this.Descendants()
                .OfType<Joint3D>()
                .ToDictionary(a => a.Name!);

            var skeleton = _bodyTrack!.GetSkeleton();

            _bodySkeleton = skeleton;
            _bodyJoints = new Joint3D?[(int)FullBodyJointMETA.CountMeta];

            _avatarBindWorldOrientations = new Quaternion[_bodyJoints.Length];
            _trackingBindWorldOrientations = new Quaternion[_bodyJoints.Length];
            _avatarBindLocalOrientations = new Quaternion[_bodyJoints.Length];

            foreach (var item in _bodyMap)
            {
                if (!joints.TryGetValue(item.Value, out var joint))
                    continue;

                var index = (int)item.Key;

                _bodyJoints[index] = joint;
                _avatarBindWorldOrientations[index] = joint.WorldOrientation;
                _trackingBindWorldOrientations[index] = skeleton[index].Pose.ToPose3().Orientation;
                _avatarBindLocalOrientations[index] = joint.Transform.Orientation;
            }

            _jointUpdateOrder = Enumerable.Range(0, _bodyJoints.Length)
                .Where(i => _bodyJoints[i] != null)
                .OrderBy(i => _bodyJoints[i]!.Ancestors().Count())
                .ToArray();
        }

        protected void UpdateBodyJoints(BodyJointLocationFB[] locations)
        {
            foreach (var i in _jointUpdateOrder)
            {
                var joint = _bodyJoints![i]!;
                ref var location = ref locations[i];
                var pose = location.Pose.ToPose3();

                if ((location.LocationFlags & SpaceLocationFlags.OrientationValidBit) != 0)
                {
                    joint.WorldOrientation = Quaternion.Normalize(
                        pose.Orientation * _bodyBasis);
                }

                if ((location.LocationFlags & SpaceLocationFlags.PositionValidBit) != 0)
                    joint.WorldPosition = pose.Position;
            }
        }

        public override void Dispose()
        {
            _bodyTrack?.Dispose();
            _bodyTrack = null;

            base.Dispose();
        }
    }
}