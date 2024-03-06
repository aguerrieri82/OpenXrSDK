using OpenXr.Framework;
using System.Numerics;

namespace Xr.Engine.OpenXr
{
    public abstract class BaseObjectGrabber<T> : Behavior<T>  where T : Object3D
    {

        private Object3D? _grabObject;
        private IGrabbable? _grabbable;
        private readonly XrHaptic _vibrate;
        private readonly TriangleMesh _grabView;
        private Quaternion _startInputOrientation;
        private Quaternion _startOrientation;
        private Vector3 _startPivot;
        private bool _isVibrating;
        private bool _grabStarted;

        public BaseObjectGrabber(XrHaptic vibrate)
        {
            _vibrate = vibrate;
            _grabView = new TriangleMesh(Cube3D.Instance, new StandardMaterial { Color = new Color(0, 1, 1, 1) });
            _grabView.Transform.SetScale(0.01f);
        }

        protected override void Start(RenderContext ctx)
        {
           // _host!.Scene!.AddChild(_grabView);
        }

        protected abstract bool IsGrabbing(out XrPose? grabPoint);

        protected override void Update(RenderContext ctx)
        {
            bool isGrabbing = IsGrabbing(out var grabPoint);

            if (grabPoint == null)
                return;

            _grabView.Transform.Position = grabPoint.Position;
            _grabView.Transform.Orientation = grabPoint.Orientation;

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
                        if (grabbable.IsEnabled && grabbable.CanGrab(grabPoint.Position))
                        {
                            _grabbable = grabbable;
                            _grabObject = item;
                            _vibrate.VibrateStart(100, 1, TimeSpan.FromMilliseconds(500));
                            _isVibrating = true;
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
                        _startInputOrientation = grabPoint.Orientation;
                        _startOrientation = _grabObject!.Transform.Orientation;

                        _grabObject?.Transform.SetLocalPivot(_grabObject!.ToLocal(grabPoint.Position), true);
                        _grabStarted = true;
                        _grabObject?.SetProp("IsGrabbing", true);
                    }
                    else
                    {
                        _grabObject?.Transform.SetLocalPivot(_startPivot, true);
                        _grabObject?.SetProp("IsGrabbing", false);
                        _grabbable = null;
                        _grabObject = null;
                        _grabStarted = false;
                    }
                }
                else
                {
                    if (_isVibrating)
                    {
                        _vibrate.VibrateStop();
                        _isVibrating = false;
                    }
                }
            }


            if (isGrabbing && _grabObject != null)
            {

                _grabObject!.WorldPosition = grabPoint.Position;
                _grabObject!.Transform.Orientation = MathUtils.QuatAdd(_startOrientation, MathUtils.QuatDiff(grabPoint.Orientation, _startInputOrientation));
            }

            base.Update(ctx);
        }
    }
}
