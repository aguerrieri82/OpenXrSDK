using OpenXr.Framework;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public struct ObjectGrab
    {
        public Pose3 Pose;

        public bool IsGrabbing;

        public bool IsValid;
    }

    public abstract class BaseObjectGrabber<T> : Behavior<T> where T : Object3D
    {
        private Object3D? _grabObject;
        private IGrabbable? _grabbable;
        private readonly XrHaptic? _vibrate;
        private readonly TriangleMesh _grabView;
        private Quaternion _startInputOrientation;
        private Quaternion _startOrientation;
        private Vector3 _startPivot;
        private bool _isVibrating;
        protected bool _grabStarted;

        public BaseObjectGrabber(XrHaptic? vibrate = null)
        {
            _vibrate = vibrate;
            _grabView = new TriangleMesh(Cube3D.Instance, new StandardMaterial { Color = new Color(0, 1, 1, 1) });
            _grabView.Transform.SetScale(0.005f);
        }

        protected override void Start(RenderContext ctx)
        {
            _host!.Scene!.AddChild(_grabView);
        }

        protected abstract ObjectGrab IsGrabbing();

        //TODO: instance object are not included
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

        protected virtual void StartGrabbing(IGrabbable grabbable, Object3D grabObj, Pose3 grabPoint)
        {
            _grabStarted = true;
            _grabbable = grabbable;
            _grabObject = grabObj;

            _startPivot = _grabObject!.Transform.LocalPivot;
            _startInputOrientation = grabPoint.Orientation;
            _startOrientation = _grabObject!.Transform.Orientation;

            _grabObject?.Transform.SetLocalPivot(_grabObject!.ToLocal(grabPoint.Position), true);
            _grabObject?.IsManipulating(true);

            _grabbable.Grab();
        }

        protected virtual void MoveGrabbing(Pose3 grabPoint)
        {
            _grabObject!.WorldPosition = grabPoint.Position;
            _grabObject!.Transform.Orientation = MathUtils.QuatAdd(_startOrientation, MathUtils.QuatDiff(grabPoint.Orientation, _startInputOrientation));
        }

        protected virtual void StopGrabbing()
        {
            _grabObject?.Transform.SetLocalPivot(_startPivot, true);
            _grabObject?.IsManipulating(false);
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
            var objGrab = IsGrabbing();

            if (!objGrab.IsValid)
                return;

            _grabView.Transform.Position = objGrab.Pose.Position;
            _grabView.Transform.Orientation = objGrab.Pose.Orientation;


            if (!_grabStarted)
            {
                var grabObj = FindGrabbable(objGrab.Pose.Position, out var grabbable);

                if (grabObj != null)
                {
                    if (_vibrate != null)
                    {
                        _vibrate.VibrateStart(100, 1, TimeSpan.FromMilliseconds(500));
                        _isVibrating = true;
                    }

                    if (objGrab.IsGrabbing)
                        StartGrabbing(grabbable!, grabObj, objGrab.Pose);

                    ((StandardMaterial)_grabView.Materials[0]).Color = new Color(0, 1, 0, 1);
                }
                else
                {
                    if (_isVibrating)
                    {
                        _vibrate?.VibrateStop();
                        _isVibrating = false;
                    }

                    ((StandardMaterial)_grabView.Materials[0]).Color = new Color(0, 1, 1, 1);
                }
            }
            else
            {
                if (!objGrab.IsGrabbing)
                    StopGrabbing();
            }

            if (_grabStarted)
                MoveGrabbing(objGrab.Pose);
        }

        public Object3D GrabView => _grabView;
    }
}
