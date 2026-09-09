using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine.OpenXr.Oculus
{
    public class AvatarTracker : Behavior<Avatar>, IDisposable
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
        protected BodySkeletonRetargeter? _bodyRetargeter;
        protected Joint3D? _xrScheleton;
        protected Dictionary<int, Joint3D>? _xrScheletonMap;

        public AvatarTracker()
        {
            BaseTransform = Matrix4x4.Identity;
            ShowSkeleton = true;
        }

        protected override void Update(RenderContext ctx)
        {
            _xrApp ??= XrApp.Current;

            if (_xrApp != null && _xrApp.IsStarted)
            {
                if (_bodyTrack == null)
                {
                    _bodyTrack = new XrBodyTrack(_xrApp);
                    _bodyTrack.Create(BodyJointSetFB.FullBodyMeta);
                }

                var locations = _bodyTrack.LocateJoints(_xrApp.ReferenceSpace, _xrApp.FramePredictedDisplayTime);

                if (_bodyTrack.IsActive && _bodyTrack.Skeleton != null)
                {
                    bool updateScheleton = false;

                    if (_bodyRetargeter == null)
                    {
                        BindBody();
                        updateScheleton = true;
                    }
                    else if (!_bodyRetargeter.IsBoundTo(_bodyTrack.Skeleton))
                    {
                        _bodyRetargeter.Rebind(_bodyTrack.Skeleton);
                        updateScheleton = true;
                    }

                    if (updateScheleton && ShowSkeleton)
                    {
                        _xrScheleton?.Remove();
                        _xrScheleton = _host.AddChild(_bodyTrack.Skeleton!.BuildScheleton("xr", out _xrScheletonMap));
                    }

                    _bodyRetargeter!.Update(locations, BaseTransform);
                }

                if (_xrScheleton != null)
                    FitScheleton(locations);
            }
        }

        protected void FitScheleton(BodyJointLocationFB[] locations)
        {
            for (var i = 0; i < locations.Length; i++)
            {
                var location = locations[i];

                var joint = _xrScheletonMap![i];

                var pose = location.Pose.ToPose3();

                if ((location.LocationFlags & SpaceLocationFlags.PositionValidBit) != 0)
                    joint.WorldPosition = pose.Position;

                if ((location.LocationFlags & SpaceLocationFlags.PositionValidBit) != 0)
                    joint.WorldOrientation = pose.Orientation;
            }
        }

        protected void BindBody()
        {
            var joints = _host.Descendants()
                .OfType<Joint3D>()
                .ToDictionary(a => a.Name!);

            var bodyJoints = new Joint3D?[(int)FullBodyJointMETA.CountMeta];

            foreach (var item in _bodyMap)
            {
                if (joints.TryGetValue(item.Value, out var joint))
                    bodyJoints[(int)item.Key] = joint;
            }

            _bodyRetargeter = new BodySkeletonRetargeter(
                _bodyTrack!.Skeleton ?? _bodyTrack.GetSkeleton(),
                bodyJoints,
                (int)FullBodyJointMETA.RootMeta,
                _host.DescendantsOrSelfComponents<MeshSkin>().ToArray());
        }


        public void Mirror(float distance)
        {
            BaseTransform = Matrix4x4.CreateTranslation(0, 0, distance) *
               Matrix4x4.CreateScale(1, 1, -1) *
               Matrix4x4.CreateTranslation(0, 0, -distance);

            foreach (var material in _host.MaterialsDeep<Material>())
                material.FrontFace = FrontFaceDir.CW;
        }

        public void Dispose()
        {
            _bodyTrack?.Dispose();
            _bodyTrack = null;
            _bodyRetargeter = null;

            GC.SuppressFinalize(this);
        }


        public Matrix4x4 BaseTransform { get; set; }

        public bool ShowSkeleton { get; set; }
    }
}
