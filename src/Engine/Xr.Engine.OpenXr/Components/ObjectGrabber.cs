using OpenXr.Framework;
using System.Numerics;

namespace Xr.Engine.OpenXr
{
    public class ObjectGrabber : Behavior<Scene>
    {
        private readonly XrInput<XrPose> _input;
        private readonly XrInput<float>[] _handlers;
        private Object3D? _grabObject;
        private IGrabbable? _grabbable;
        private readonly XrHaptic _vibrate;
        private readonly TriangleMesh _grabView;
        private Quaternion _startInputOrientation;
        private Quaternion _startOrientation;
        private Vector3 _startPivot;

        public ObjectGrabber(XrInput<XrPose> input, XrHaptic vibrate, params XrInput<float>[] handlers)
        {
            _input = input;
            _handlers = handlers;
            _vibrate = vibrate;
            _grabView = new TriangleMesh(Cube.Instance, new StandardMaterial { Color = new Color(0, 1, 1, 1) });
            _grabView.Transform.SetScale(0.005f);
        }

        public override void Start(RenderContext ctx)
        {
            _host!.AddChild(_grabView);

        }

        protected override void Update(RenderContext ctx)
        {
            if (_input.Value == null)
                return;

            _grabView.Transform.Position = _input.Value.Position;
            _grabView.Transform.Orientation = _input.Value.Orientation;

            var isGrabbing = _handlers.All(a => a.Value > 0.8);

            if (!isGrabbing && _grabObject != null)
            {
                _grabbable!.Release();
                _grabbable = null;
                _grabObject = null;
            }

            if (_grabObject == null)
            {

                foreach (var item in _host!.Scene!.ObjectsWithComponent<IGrabbable>())
                {
                    foreach (var grabbable in item.Components<IGrabbable>())
                    {
                        if (grabbable.IsEnabled && grabbable.CanGrab(_input.Value.Position))
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

                        _startPivot = _grabObject!.Transform.LocalPivot;
                        _startInputOrientation = _input.Value.Orientation;
                        _startOrientation = _grabObject!.Transform.Orientation;

                        _grabObject?.Transform.SetLocalPivot(_grabObject!.ToLocal(_input.Value.Position), true);
                    }
                    else
                    {
                        _grabObject?.Transform.SetLocalPivot(_startPivot, true);
                        _grabbable = null;
                        _grabObject = null;
                    }
                }
                else
                    _vibrate.VibrateStop();
            }


            if (isGrabbing && _grabObject != null)
            {
                _grabObject!.WorldPosition = _input.Value.Position;
                _grabObject!.Transform.Orientation = MathUtils.QuatAdd(_startOrientation, MathUtils.QuatDiff(_input.Value.Orientation, _startInputOrientation));
            }

            base.Update(ctx);
        }
    }
}
