using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class HandGrabber : BaseObjectGrabber<OculusHandView>
    {
        protected List<int> _distalIndices = [];
        protected int _thumbIndex;
        protected bool _isInit;

        public HandGrabber()
        {
        }

        protected static Sphere GetSphere(Object3D obj)
        {
            Capsule3D geo = (Capsule3D)((TriangleMesh)obj).Geometry!;

            Vector3 center = obj.WorldPosition + obj.Forward * (geo.Height / 2f) * obj.Transform.Scale.X;
            float radius = geo.Radius * obj.Transform.Scale.X;

            return new Sphere { Center = center, Radius = radius };
        }

        protected override ObjectGrab IsGrabbing()
        {
            Debug.Assert(_host != null);

            ObjectGrab result = new ObjectGrab();
            result.IsValid = _host.HandInput.IsActive;

            if (!_isInit || !result.IsValid)
                return result;

            Object3D thumbObj = _host.Children[_thumbIndex];

            Sphere thumb = GetSphere(thumbObj);

            result.IsValid = _host.HandInput.IsActive;
            result.Pose.Position = thumb.Center;
            result.Pose.Orientation = thumbObj.Transform.Orientation;

            foreach (int index in _distalIndices)
            {
                Object3D otherObj = _host.Children[index];
                Sphere other = GetSphere(otherObj);

                other.Intersects(thumb, out float offset);

                result.IsGrabbing = offset < 0 || (offset < 0.01 && _grabStarted);

                if (result.IsGrabbing)
                {
                    Vector3 forward = Vector3.Normalize(Vector3.Cross(thumbObj.Forward, otherObj.Forward));
                    Vector3 up = Vector3.Cross(forward, thumbObj.Forward);
                    result.Pose.Orientation = MathUtils.QuatFromForwardUp(forward, up);
                    result.Pose.Position = (thumb.Center + other.Center) / 2;
                    break;
                }
            }

            return result;
        }

        protected override void Update(RenderContext ctx)
        {
            if (!_isInit && _host?.HandInput.Mesh != null)
            {
                int index = 0;
                foreach (HandCapsuleFB cap in _host.HandInput.Capsules)
                {
                    if (cap.Joint == HandJointEXT.ThumbDistalExt)
                        _thumbIndex = index;
                    else if (//cap.Joint == HandJointEXT.RingDistalExt ||
                             //cap.Joint == HandJointEXT.LittleDistalExt ||
                        cap.Joint == HandJointEXT.IndexDistalExt
                        /*cap.Joint == HandJointEXT.MiddleDistalExt*/)
                    {
                        _distalIndices.Add(index);
                    }
                    index++;
                }
                _isInit = _distalIndices.Count > 0;
            }
            base.Update(ctx);
        }
    }
}
