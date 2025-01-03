﻿using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.EXT;

namespace OpenXr.Framework
{
    public class XrHandInput : IDisposable
    {
        protected const int XR_HAND_JOINT_COUNT_EXT = 26;
        protected HandTrackerEXT _tracker;
        protected bool _isActive;
        protected readonly XrApp _app;
        protected HandJointLocationEXT[]? _joints;
        protected HandEXT _handType;

        public XrHandInput(XrApp app)
        {
            _app = app;
        }

        protected internal void Initialize(HandEXT hand)
        {
            var info = new HandTrackerCreateInfoEXT
            {
                Type = StructureType.HandTrackerCreateInfoExt,
                Hand = hand,
                HandJointSet = HandJointSetEXT.DefaultExt
            };

            _app.CheckResult(_app._handTracking!.CreateHandTracker(_app.Session, in info, ref _tracker), "CreateHandTracker");
            _handType = hand;
        }

        public virtual unsafe HandJointLocationEXT[] LocateHandJoints(Space space, long time)
        {
            return LocateHandJoints(space, time, null);
        }

        protected unsafe HandJointLocationEXT[] LocateHandJoints(Space space, long time, void* next)
        {
            var info = new HandJointsLocateInfoEXT()
            {
                Type = StructureType.HandJointsLocateInfoExt,
                BaseSpace = space,
                Time = time
            };

            var data = new HandJointLocationEXT[XR_HAND_JOINT_COUNT_EXT];

            var result = new HandJointLocationsEXT()
            {
                Type = StructureType.HandJointLocationsExt,
                JointCount = XR_HAND_JOINT_COUNT_EXT,
                Next = next
            };

            fixed (HandJointLocationEXT* pData = data)
            {
                result.JointLocations = pData;
                _app.CheckResult(_app._handTracking!.LocateHandJoints(_tracker, in info, ref result), "LocateHandJoints");
            }

            _isActive = result.IsActive != 0;
            _joints = data;

            return data;
        }

        public void Destroy()
        {
            _app.CheckResult(_app._handTracking!.DestroyHandTracker(_tracker), "DestroyHandTracker");
            _tracker.Handle = 0;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        protected ExtHandTracking HandTracking => _app._handTracking!;

        public HandEXT HandType => _handType;

        public HandTrackerEXT Tracker => _tracker;

        public bool IsActive => _isActive;

        public HandJointLocationEXT[]? Joints => _joints;
    }
}
