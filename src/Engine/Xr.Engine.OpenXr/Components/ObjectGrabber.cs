using OpenXr.Engine;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenXr
{
    public class ObjectGrabber : Behavior<Scene>
    {
        private XrInput<XrPose> _input;
        private XrInput<float>[] _handlers;
        private Object3D? _grabObject;
        private IGrabbable? _grabbable;
        private Vector3 _grabPositionLocal;
        private XrHaptic _vibrate;

        public ObjectGrabber(XrInput<XrPose> input, XrHaptic vibrate,  params XrInput<float>[] handlers)
        {
            _input = input;
            _handlers = handlers;
            _vibrate = vibrate; 
        }

        protected override void Update(RenderContext ctx)
        {
            var isGrabbing = _handlers.All(a => a.Value > 0.8);

            if (!isGrabbing && _grabObject != null)
            {
                _grabbable!.Release();
                _grabbable = null;
                _grabObject = null;
            }

            if (_grabObject == null)
            {
                foreach (var item in _host!.Scene!.VisibleDescendants<Object3D>())
                {
                    foreach (var grabbable in item.Components<IGrabbable>())
                    {
                        if (grabbable.CanGrab(_input.Value.Position))
                        {
                            _grabbable = grabbable;
                            _grabObject = item;
                            _vibrate.VibrateStart(100, 1, TimeSpan.FromMilliseconds(500));
                            break;
                        }

                    }
                    if (_grabObject != null)
                        break;
                }

                if (_grabbable != null)
                {
                    if (isGrabbing)
                    {
                        _vibrate.VibrateStop();
                        _grabbable.Grab();
                        _grabPositionLocal = _input.Value.Position.Transform(_grabObject!.WorldMatrixInverse);
                    }
                    else
                    {
                        _grabbable = null;
                        _grabObject = null;
                    }
                }
                else
                    _vibrate.VibrateStop();
            }

            if (isGrabbing && _grabObject != null)
            {
                _grabObject.Transform.Position = _input.Value.Position;
                _grabObject.Transform.Orientation = _input.Value.Orientation;
            }

            base.Update(ctx);
        }
    }
}
