using OpenXr.Engine;
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
        private readonly Mesh _grabView;

        private Vector3 _startPosition;
        private Vector3 _startInputPos;
        private Quaternion _startInputOrientation;
        private Quaternion _startOrientation;

        public ObjectGrabber(XrInput<XrPose> input, XrHaptic vibrate, params XrInput<float>[] handlers)
        {
            _input = input;
            _handlers = handlers;
            _vibrate = vibrate;
            _grabView = new Mesh(Cube.Instance, new StandardMaterial { Color = new Color(0, 1, 1, 1) });
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
                        _startInputPos = _input.Value.Position;
                        _startInputOrientation = _input.Value.Orientation;
                        _startOrientation = _grabObject!.Transform.Orientation;
                        _startPosition = _grabObject.Transform.Position;
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
                var matrix = Matrix4x4.CreateScale(0.5f) *
                            Matrix4x4.CreateTranslation(1f, 0, 0);

                var vector = new Vector3(1, 1, 1);




            }

            base.Update(ctx);
        }
    }
}
