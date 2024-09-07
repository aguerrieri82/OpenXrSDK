using OpenXr.Framework;
using System.Numerics;
using XrInteraction;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrInputPointer : BaseComponent<Object3D>, IRayPointer
    {
        public RayPointerStatus GetPointerStatus()
        {
            var result = new RayPointerStatus();

            if (LeftButton != null && LeftButton.IsActive && LeftButton.Value)
                result.Buttons |= Pointer2Button.Left;

            if (RightButton != null && RightButton.IsActive && RightButton.Value)
                result.Buttons |= Pointer2Button.Right;

            if (PoseInput != null && PoseInput.IsActive)
            {
                result.Ray = new Ray3
                {
                    Origin = PoseInput.Value.Position,
                    Direction = (PoseInput.Value.Transform(new Vector3(0, 0, -1)) -
                                 PoseInput.Value.Transform(Vector3.Zero))
                                 .Normalize()
                };

                result.IsActive = true;
            }

            return result;
        }

        public void CapturePointer()
        {

        }

        public void ReleasePointer()
        {
        }

        public XrInput<Pose3>? PoseInput { get; set; }

        public XrInput<bool>? LeftButton { get; set; }

        public XrInput<bool>? RightButton { get; set; }

        public int PointerId => _host!.Id.Value.GetHashCode();
    }
}
