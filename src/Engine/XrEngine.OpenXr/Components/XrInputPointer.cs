using OpenXr.Framework;
using System.Diagnostics;
using System.Numerics;
using XrInteraction;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrInputPointer : BaseComponent<Object3D>, IRayPointer
    {
        private int _captureCount;

        public XrInputPointer()
        {
            Name = "Controller";
        }

        public RayPointerStatus GetPointerStatus()
        {
            var result = new RayPointerStatus();

            if (LeftButton != null && LeftButton.IsActive && LeftButton.Value)
                result.Buttons |= Pointer2Button.Left;

            if (RightButton != null && RightButton.IsActive && RightButton.Value)
                result.Buttons |= Pointer2Button.Right;

            if (AButton != null && AButton.IsActive && AButton.Value)
                result.Buttons |= Pointer2Button.A;

            if (BButton != null && BButton.IsActive && BButton.Value)
                result.Buttons |= Pointer2Button.B;

            if (PoseInput != null && PoseInput.IsActive)
            {
                result.Ray = PoseInput.Value.ToRay();
                result.IsActive = true;
            }

            return result;
        }

        public void CapturePointer()
        {
            _captureCount = 1;
        }

        public void ReleasePointer()
        {
            _captureCount = 0;
        }

        public XrInput<Pose3>? PoseInput { get; set; }

        public XrInput<bool>? LeftButton { get; set; }

        public XrInput<bool>? RightButton { get; set; }

        public XrInput<bool>? AButton { get; set; }

        public XrInput<bool>? BButton { get; set; }

        public int PointerId => _host!.Id.Value.GetHashCode();

        public bool IsCaptured => _captureCount > 0;

        public string Name { get; set; }
    }
}
