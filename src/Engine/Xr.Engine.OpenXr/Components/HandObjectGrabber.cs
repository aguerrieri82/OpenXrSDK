using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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

        protected override bool IsGrabbing(out XrPose? grabPoint)
        {
            if (!_isInit)
            {
                grabPoint = null;
                return false;
            }

            var thumbObj = _host!.Children[_thumbIndex];

            var thumb = GetSphere(thumbObj);

            grabPoint = new XrPose
            {
                Position = thumb.Center,
                Orientation = thumbObj.Transform.Orientation,
            };

            foreach (var index in _distalIndices)
            {
                var otherObj = _host!.Children[index];
                var other = GetSphere(otherObj);

                other.Intersects(thumb, out var offset);

                bool isGrabbing = offset < 0 || (offset < 0.01 && _grabStarted);

                if (isGrabbing)
                {
                    var forward = Vector3.Normalize(Vector3.Cross(thumbObj.Forward, otherObj.Forward));
                    var up = Vector3.Cross(forward, thumbObj.Forward);
                    grabPoint.Orientation = MathUtils.QuatFromForwardUp(forward, up);
                    grabPoint.Position = (thumb.Center + other.Center) / 2;
                    return true;
                }
            }

            return false;
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
