using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.FB;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenXr.Framework.Oculus
{
    public class XrBodyTrack : IDisposable
    {
        FBBodyTracking? _bodyTracking;
        private BodyTrackerFB _tracker;
        private bool _isActive;
        private BodyJointLocationFB[]? _joints;
        private long _time;
        private float _confidence;
        private int _skeletonChanges = -1;
        private BodySkeletonJointFB[]? _skeleton;
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
        }

        public void Create(BodyJointSetFB jointSet)
        {
            Initialize();

            var info = new BodyTrackerCreateInfoFB()
            {
                Type = StructureType.BodyTrackerCreateInfoFB,
                BodyJointSet = jointSet,
            };

            var result = new BodyTrackerFB();

            _app.CheckResult(_bodyTracking!.CreateBodyTrackerFB(_app.Session, ref info, ref result), "CreateBodyTrackerFB");

            _tracker = result;
            _skeletonChanges = -1;
        }

        public unsafe BodySkeletonJointFB[] GetSkeleton()
        {
            var result = new BodySkeletonFB()
            {
                Type = StructureType.BodySkeletonFB,
                JointCount = (uint)BodyJointFB.CountFB,
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
                JointCount = (uint)BodyJointFB.CountFB,
            };

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
    }
}
