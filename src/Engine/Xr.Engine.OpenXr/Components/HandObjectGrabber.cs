using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;
using Xr.Math;

namespace Xr.Engine.OpenXr
{
    public class HandObjectGrabber : BaseObjectGrabber<OculusHandView>
    {
        protected List<int> _distalIndices = [];
        protected int _thumbIndex;
        protected bool _isInit;

        public HandObjectGrabber()
        {
        }

        protected Sphere GetSphere(Object3D obj)
        {
            var geo = (Capsule3D)((TriangleMesh)obj).Geometry!;

            var center = obj.WorldPosition + obj.Forward * (geo.Height / 2f) * obj.Transform.Scale.X;
            var radius = geo.Radius * obj.Transform.Scale.X;

            return new Sphere { Center = center, Radius = radius };
        }

        protected override ObjectGrab IsGrabbing()
        {
            Debug.Assert(_host != null);

            var result = new ObjectGrab();
            result.IsValid = _host.HandInput.IsActive;

            if (!_isInit || !result.IsValid)
                return result;

            var thumbObj = _host!.Children[_thumbIndex];

            var thumb = GetSphere(thumbObj);

            result.IsValid = _host.HandInput.IsActive;
            result.Pose.Position = thumb.Center;
            result.Pose.Orientation = thumbObj.Transform.Orientation;

            foreach (var index in _distalIndices)
            {
                var otherObj = _host!.Children[index];
                var other = GetSphere(otherObj);

                other.Intersects(thumb, out var offset);

                result.IsGrabbing = offset < 0 || (offset < 0.01 && _grabStarted);

                if (result.IsGrabbing)
                {
                    var forward = Vector3.Normalize(Vector3.Cross(thumbObj.Forward, otherObj.Forward));
                    var up = Vector3.Cross(forward, thumbObj.Forward);
                    result.Pose.Orientation = MathUtils.QuatFromForwardUp(forward, up);
                    result.Pose.Position = (thumb.Center + other.Center) / 2;
                    break;
                }
            }

            return result;
        }

        protected override void Update(RenderContext ctx)
        {
            if (!_isInit && _host!.HandInput.Mesh != null)
            {
                var index = 0;
                foreach (var cap in _host!.HandInput.Capsules)
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
                _isInit = true;
            }

            base.Update(ctx);
        }
    }
}
