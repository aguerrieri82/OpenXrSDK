using OpenXr.Framework;
using PhysX.Framework;
using System.Diagnostics;
using System.Numerics;
using XrEngine.Physics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class InputPhysicsGrabber : Behavior<Scene3D>, IObjectTool
    {
        private readonly TriangleMesh _grabView;
        private bool _grabStarted;
        private IGrabbable? _grabbable;
        private Object3D? _grabObject;
        private bool _isVibrating;
        private Joint? _joint;

        public InputPhysicsGrabber()
        {
            _grabView = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr(new Color(0, 1, 1, 1)));
            _grabView.Flags |= EngineObjectFlags.DisableNotifyChangedScene | EngineObjectFlags.Generated;
            _grabView.Name = "Grab View";
            _grabView.Transform.SetScale(0.01f);

            _grabView.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Kinematic
            });
        }

        public InputPhysicsGrabber(XrPoseInput input, XrHaptic? vibrate, params XrFloatInput[] handlers)
            : this()
        {
            Input = input;
            Handlers = handlers;
            Vibrate = vibrate;
            GrabThreshold = 0.8f;
        }

        protected override void Start(RenderContext ctx)
        {
            _host!.Scene!.AddChild(_grabView);
            base.Start(ctx);
        }

        protected Object3D? FindGrabbable(Vector3 worldPos, out IGrabbable? grabbable)
        {
            Debug.Assert(_host?.Scene != null);

            foreach (var item in _host.Scene.ObjectsWithComponent<IGrabbable>())
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

        protected virtual void StartGrabbing(IGrabbable grabbable, Object3D grabObj, Pose3 grabPoint, string grabber)
        {
            _grabStarted = true;
            _grabbable = grabbable;
            _grabObject = grabObj;

            //_grabObject.SetActiveTool(this, true);

            var pm = _host!.Scene!.Component<PhysicsManager>();

            _joint = pm.AddJoint(JointType.D6,
                        _grabView,
                        Pose3.Identity,
                        grabObj,
                        grabObj.GetWorldPose().Inverse().Multiply(grabPoint));

            var rb = grabObj.Component<RigidBody>();
            rb.IsEnabled = true;

            _grabbable.Grab(grabber);
        }

        protected virtual void MoveGrabbing(Pose3 grabPoint)
        {
            Debug.Assert(_grabObject != null);

            _grabView.Transform.Position = grabPoint.Position;
            _grabView.Transform.Orientation = grabPoint.Orientation;

            _grabbable?.NotifyMove();
        }

        protected virtual void StopGrabbing()
        {
            if (_joint != null)
            {
                _joint.Dispose();
                _joint = null;
            }

            if (_grabObject != null)
            {
                var rb = _grabObject.Component<RigidBody>();
                rb.IsEnabled = false;

                //_grabObject.SetActiveTool(this, false);
                _grabObject = null;
            }

            _grabbable = null;
            _grabStarted = false;
        }

        void IObjectTool.Deactivate()
        {
            StopGrabbing();
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
                    if (Vibrate != null)
                    {
                        Vibrate.VibrateStart(100, 1, TimeSpan.FromMilliseconds(500));
                        _isVibrating = true;
                    }

                    if (objGrab.IsGrabbing)
                        StartGrabbing(grabbable!, grabObj, objGrab.Pose, objGrab.Grabber ?? "");

                    _grabView.UpdateColor(new Color(0, 1, 0, 1));
                }
                else
                {
                    if (_isVibrating)
                    {
                        Vibrate?.VibrateStop();
                        _isVibrating = false;
                    }

                    _grabView.UpdateColor(new Color(0, 1, 1, 1));
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

        protected ObjectGrab IsGrabbing()
        {
            return new ObjectGrab
            {
                Pose = Input!.Value,
                IsGrabbing = Handlers!.All(a => a.Value > GrabThreshold),
                IsValid = Input.IsActive,
                Grabber = Input?.Name
            };
        }

        public float GrabThreshold { get; set; }

        public XrPoseInput? Input { get; set; }

        public IList<XrFloatInput>? Handlers { get; set; }

        public XrHaptic? Vibrate { get; set; }
    }
}
