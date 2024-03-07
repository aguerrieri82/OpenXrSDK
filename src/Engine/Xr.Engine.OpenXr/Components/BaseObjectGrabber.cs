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
        protected bool _grabStarted;

        public BaseObjectGrabber(XrHaptic vibrate)
        {
            _vibrate = vibrate;
            _grabView = new TriangleMesh(Cube3D.Instance, new StandardMaterial { Color = new Color(0, 1, 1, 1) });
            _grabView.Transform.SetScale(0.01f);
        }

        protected override void Start(RenderContext ctx)
        {
           _host!.Scene!.AddChild(_grabView);
        }

        protected abstract bool IsGrabbing(out XrPose? grabPoint);

        protected Object3D? FindGrabbable(Vector3 worldPos, out IGrabbable? grabbable)
        {
            foreach (var item in _host!.Scene!.ObjectsWithComponent<IGrabbable>())
            {
                foreach (var comp in item.Components<IGrabbable>())
                {
                    if (comp.IsEnabled && comp.CanGrab(worldPos))
                    {
                        grabbable = comp;
                        return item;
                    }
                }
            }

            grabbable = null;
            return null;
        }

        protected virtual void StartGrabbing(IGrabbable grabbable, Object3D grabObj, XrPose grabPoint)
        {
            _grabStarted = true;
            _grabbable = grabbable;
            _grabObject = grabObj;

            _startPivot = _grabObject!.Transform.LocalPivot;
            _startInputOrientation = grabPoint.Orientation;
            _startOrientation = _grabObject!.Transform.Orientation;

            _grabObject?.Transform.SetLocalPivot(_grabObject!.ToLocal(grabPoint.Position), true);
            _grabObject?.SetProp("IsGrabbing", true);

            _grabbable.Grab();
        }

        protected virtual void MoveGrabbing(XrPose grabPoint)
        {
            _grabObject!.WorldPosition = grabPoint.Position;
            _grabObject!.Transform.Orientation = MathUtils.QuatAdd(_startOrientation, MathUtils.QuatDiff(grabPoint.Orientation, _startInputOrientation));
        }

        protected virtual void StopGrabbing()
        {
            _grabObject?.Transform.SetLocalPivot(_startPivot, true);
            _grabObject?.SetProp("IsGrabbing", false);
            _grabbable = null;
            _grabObject = null;
            _grabStarted = false;
        }

        protected override void OnDisabled()
        {
            StopGrabbing();
        }

        protected override void Update(RenderContext ctx)
        {
            bool isGrabbing = IsGrabbing(out var grabPoint);

            if (grabPoint == null)
                return;

            _grabView.Transform.Position = grabPoint.Position;
            _grabView.Transform.Orientation = grabPoint.Orientation;

            if (!_grabStarted)
            {
                var grabObj = FindGrabbable(grabPoint.Position, out var grabbable);

                if (grabObj != null)
                {
                   // _vibrate.VibrateStart(100, 1, TimeSpan.FromMilliseconds(500));
                    _isVibrating = true;
                    if (isGrabbing)
                        StartGrabbing(grabbable!, grabObj, grabPoint);
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
            else
            {
                if (!isGrabbing)
                    StopGrabbing();
            }

            if (_grabStarted)
                MoveGrabbing(grabPoint);
        }

        public Object3D GrabView => _grabView;
    }
}
