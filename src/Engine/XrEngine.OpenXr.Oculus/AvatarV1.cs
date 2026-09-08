using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr.Oculus
{
    public partial class AvatarV1 : Group3D
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

        private static readonly Quaternion _bodyBasis = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
        private static readonly Quaternion _footAnkleBasis = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);

        protected XrBodyTrack? _bodyTrack;
        protected XrApp? _xrApp;
        protected Joint3D?[]? _bodyJoints;
        protected int[] _jointUpdateOrder = [];
        protected float _bodyScale = 1;

        protected Quaternion[]? _jointBasis;

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

                var locations = _bodyTrack.LocateJoints(_xrApp.ReferenceSpace, _xrApp.FramePredictedDisplayTime);

                if (_bodyTrack.IsActive)
                    UpdateBodyJoints(locations);
            }

            base.UpdateSelf(ctx);
        }
        protected void BindBodyJoints()
        {
            var joints = this.Descendants()
                .OfType<Joint3D>()
                .ToDictionary(a => a.Name!);

            var skeleton = _bodyTrack!.GetSkeleton();

            _bodyJoints = new Joint3D?[(int)FullBodyJointMETA.CountMeta];
            _jointBasis = new Quaternion[_bodyJoints.Length];

            for (var i = 0; i < _jointBasis.Length; i++)
                _jointBasis[i] = _bodyBasis;

            foreach (var item in _bodyMap)
            {
                if (!joints.TryGetValue(item.Value, out var joint))
                    continue;

                var index = (int)item.Key;

                _bodyJoints[index] = joint;

                if (item.Key == FullBodyJointMETA.LeftFootAnkleMeta ||
                    item.Key == FullBodyJointMETA.RightFootAnkleMeta)
                {
                    var trackingBind = skeleton[index].Pose.ToPose3().Orientation;

                    _jointBasis[index] = Quaternion.Normalize(
                        Quaternion.Inverse(trackingBind) *
                        joint.WorldOrientation *
                        _footAnkleBasis);
                }
                else if (item.Key == FullBodyJointMETA.LeftFootBallMeta ||
                         item.Key == FullBodyJointMETA.RightFootBallMeta)
                {
                    var trackingBind = skeleton[index].Pose.ToPose3().Orientation;

                    var correction = Quaternion.Normalize(
                        Quaternion.Inverse(joint.Transform.Orientation) *
                        _footAnkleBasis *
                        joint.Transform.Orientation);

                    _jointBasis[index] = Quaternion.Normalize(
                        Quaternion.Inverse(trackingBind) *
                        joint.WorldOrientation *
                        correction);

                }
            }

            _jointUpdateOrder = Enumerable.Range(0, _bodyJoints.Length)
                .Where(i => _bodyJoints[i] != null)
                .OrderBy(i => _bodyJoints[i]!.Ancestors().Count())
                .ToArray();

            var rootIndex = (int)FullBodyJointMETA.RootMeta;
            var hipsIndex = (int)FullBodyJointMETA.HipsMeta;

            var avatarLength = Vector3.Distance(
                _bodyJoints[rootIndex]!.WorldPosition,
                _bodyJoints[hipsIndex]!.WorldPosition);

            var trackingLength = Vector3.Distance(
                skeleton[rootIndex].Pose.ToPose3().Position,
                skeleton[hipsIndex].Pose.ToPose3().Position);

            _bodyScale = avatarLength / trackingLength;
        }

        protected void UpdateBodyJoints(BodyJointLocationFB[] locations)
        {
            foreach (var i in _jointUpdateOrder)
            {
                ref var location = ref locations[i];

                if ((location.LocationFlags & SpaceLocationFlags.OrientationValidBit) != 0)
                {
                    _bodyJoints![i]!.WorldOrientation = Quaternion.Normalize(
                        location.Pose.ToPose3().Orientation * _jointBasis![i]);
                }
            }

            var rootIndex = (int)FullBodyJointMETA.RootMeta;
            var hipsIndex = (int)FullBodyJointMETA.HipsMeta;

            ref var rootLocation = ref locations[rootIndex];
            ref var hipsLocation = ref locations[hipsIndex];

            if ((rootLocation.LocationFlags & SpaceLocationFlags.PositionValidBit) == 0)
                return;

            var trackingRootPosition = rootLocation.Pose.ToPose3().Position;
            var rootPosition = trackingRootPosition + RootDelta;

            _bodyJoints![rootIndex]!.WorldPosition = rootPosition;

            if ((hipsLocation.LocationFlags & SpaceLocationFlags.PositionValidBit) != 0)
            {
                var hipsPosition = hipsLocation.Pose.ToPose3().Position;

                _bodyJoints[hipsIndex]!.WorldPosition =
                    rootPosition + (hipsPosition - trackingRootPosition) * _bodyScale;
            }
        }

        public override void Dispose()
        {
            _bodyTrack?.Dispose();
            _bodyTrack = null;

            base.Dispose();
        }

        [Range(0, 1, 0.005f)]
        public Vector3 RootDelta { get; set; }
    }
}