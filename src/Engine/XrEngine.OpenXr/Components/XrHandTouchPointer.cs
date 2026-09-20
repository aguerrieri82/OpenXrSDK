using OpenXr.Framework;
using Silk.NET.OpenXR;
using XrInteraction;

namespace XrEngine.OpenXr
{
    public class XrHandTouchPointer : BaseComponent<OculusHandView>, ITouchPointer
    {

        public TouchPointerStatus GetPointerStatus()
        {
            var result = new TouchPointerStatus
            {
                IsActive = _host.HandInput.IsActive
            };

            if (result.IsActive)
            {
                var index = _host.HandInput.Joints![(int)HandJointEXT.IndexTipExt];

                if ((index.LocationFlags & SpaceLocationFlags.PositionValidBit) != 0)
                    result.Pose.Position = index.Pose.Position.ToVector3();
                else
                    result.IsActive = false;    
            }

            return result;
        }

        public int PointerId => _host.HandType == HandEXT.LeftExt ? 10 : 11;

        public string Name => _host.HandType == HandEXT.LeftExt ? "Hand Left" : "Hand Right";
    }
}
