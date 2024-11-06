using PhysX.Framework;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.Physics;
using XrMath;
using IDrawGizmos = XrEngine.IDrawGizmos;

namespace XrSamples.Components
{
    public class CarModel : Behavior<Group3D>, IDrawGizmos
    {
        private Group3D? _chassis;
        private Joint? _steerLeft;
        private Joint? _steerRight;

        private Joint? _rotateLeft;
        private Joint? _rotateRight;
        private float _wheelBase;
        private float _trackWidth;
        private float _wheelRadius;
        private float _steeringAngle;
        private bool _isChanged;
        private float _wheelSpeedRad;
        private float _lastAngle;
        private TriangleMesh? _mainTube;
        private Pose3 _bodyMainDeltaPose;
        private RigidBody? _carRigidBody;

        public CarModel()
        {
            WheelMass = 2;
            ChassisDensity = 5000;
            CarBodyDensity = 10;
            SteeringStiffness = 1500;
        }


        public void Create()
        {
            AttachWheels();
            CreateChassis();
            AttachBody();
        }

        protected void AttachBody()
        {
            Debug.Assert(CarBody != null && _mainTube != null);

            _bodyMainDeltaPose = _mainTube.GetWorldPose().Difference(CarBody.GetWorldPose());

            var collider = new PyMeshCollider()
            {
                UseConvexMesh = true
            };

            if (CarBodyCollisionMeshes != null)
                collider.MeshObjects = () => CarBodyCollisionMeshes;

            CarBody.AddComponent(collider);

            _carRigidBody = CarBody.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Dynamic,
                AutoTeleport = false,
                Density = CarBodyDensity,
                NotCollideGroup = RigidBodyGroup.Group1
            });

            _carRigidBody.Contact += OnContact;

            var joint = AddFixed(_mainTube, CarBody, _mainTube.WorldBounds.Center);
            joint.Options = new D6JointOptions()
            {

            };
        }

        private void OnContact(Object3D self, Object3D other, int otherIndex, ContactPair[] pairs)
        {
            Log.Debug(this, "Contact {0} with {1}", self.Name, other.Name);
        }

        protected void AttachWheels()
        {
            Object3D[] wheels = [WheelBL!, WheelBR!, WheelFR!, WheelFL!];

            var pyMaterial = new PhysicsMaterialInfo
            {
                DynamicFriction = 1f,
                StaticFriction = 1f,
                Restitution = 0.3f
            };

            foreach (var wheel in wheels)
            {
                _wheelRadius = wheel.WorldBounds.Size.Y / 2;

                var collider = new CylinderCollider();
                collider.Height = wheel.WorldBounds.Size.X;
                collider.Radius = wheel.WorldBounds.Size.Y / 2;
                collider.Pose = new Pose3
                {
                    Position = Vector3.Zero,
                    Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2)
                };

                wheel.AddComponent(collider);

                wheel.AddComponent(new RigidBody
                {
                    Type = PhysicsActorType.Dynamic,
                    Material = pyMaterial,
                    IsEnabled = true,
                    NotCollideGroup = RigidBodyGroup.Group1,
                    AutoTeleport = false,
                    Configure = rb =>
                    {
                        var targetMass = 2;
                        var ratio = rb.DynamicActor.Mass / targetMass;
                        rb.DynamicActor.MaxDepenetrationVelocity = 1f;
                        rb.DynamicActor.Mass = targetMass;
                        rb.DynamicActor.MassSpaceInertiaTensor /= ratio;
                        rb.DynamicActor.SolverIterations = new SolverIterations
                        {
                            MinPos = 30,
                            MinVel = 5
                        };
                    }
                });

                wheel.Transform.SetLocalPivot(wheel.ToLocal(wheel.WorldBounds.Center), true);
            }
        }

        Joint AddFixed(Object3D obj0, Object3D obj1, Vector3 point)
        {
            var manager = _host!.Scene!.Component<PhysicsManager>();

            var pose0 = new Pose3
            {
                Position = point,
                Orientation = Quaternion.Identity
            };

            var pose1 = new Pose3
            {
                Position = point,
                Orientation = Quaternion.Identity
            };

            pose0 = obj0.GetWorldPose().Inverse().Multiply(pose0);
            pose1 = obj1.GetWorldPose().Inverse().Multiply(pose1);

            // pose0.Position -= obj0.Transform.LocalPivot;
            // pose1.Position -= obj1.Transform.LocalPivot;

            var joint = manager.AddJoint(JointType.D6, obj0, pose0, obj1, pose1);

            return joint;
        }

        Joint AddRotation(Object3D obj0, Object3D obj1, Vector3 point, Vector3 axis, bool motor = false)
        {
            var joint = AddFixed(obj0, obj1, point);

            var opt = new D6JointOptions();

            if (axis == Vector3.UnitX)
            {
                opt.MotionTwist = PhysX.PxD6Motion.Free;
            }
            else if (axis == Vector3.UnitY)
            {
                opt.MotionSwing1 = PhysX.PxD6Motion.Free;
            }

            opt.ConstraintFlags = PhysX.PxConstraintFlags.CollisionEnabled;

            joint.Options = opt;

            return joint;
        }


        Joint AddRotationV2(Object3D obj0, Object3D obj1, Vector3 point, Vector3 axis, bool motor = false)
        {
            var manager = _host!.Scene!.Component<PhysicsManager>();

            var pose0 = new Pose3
            {
                Position = point,
                Orientation = Quaternion.Identity
            };

            var pose1 = new Pose3
            {
                Position = point,
                Orientation = Quaternion.Identity
            };

            pose0 = obj0.GetWorldPose().Inverse().Multiply(pose0);
            pose1 = obj1.GetWorldPose().Inverse().Multiply(pose1);

            // pose0.Position -= obj0.Transform.LocalPivot;
            // pose1.Position -= obj1.Transform.LocalPivot;

            var joint = manager.AddJoint(JointType.Revolute, obj0, pose0, obj1, pose1);

            var options = new RevoluteJointOptions();
            if (motor)
                options.RevoluteJointFlags |= PhysX.PxRevoluteJointFlags.DriveEnabled | PhysX.PxRevoluteJointFlags.DriveFreespin;

            joint.Options = options;

            return joint;
        }


        protected void CreateChassis()
        {

            var size = 0.05f;

            _chassis = new Group3D
            {
                Name = "Chassis"
            };

            var mat = MaterialFactory.CreatePbr("#00ff0080");
            mat.Metalness = 1;
            mat.Alpha = AlphaMode.Blend;

            TriangleMesh AddTube(Vector3 p1, Vector3 p2)
            {
                var line = new Line3(p1, p2);
                var cube = new Cube3D(new Vector3(size, size, line.Length()));

                var mesh = new TriangleMesh(cube, (Material)mat)
                {
                    WorldPosition = line.Center(),
                    Forward = line.Direction()
                };

                mesh.AddComponent<BoxCollider>();
                mesh.AddComponent(new RigidBody
                {
                    Type = PhysicsActorType.Dynamic,
                    AutoTeleport = false,
                    NotCollideGroup = RigidBodyGroup.Group1,
                    Density = ChassisDensity,
                    Configure = rb =>
                    {
                        rb.DynamicActor.MaxDepenetrationVelocity = 1f;
                        rb.DynamicActor.SolverIterations = new SolverIterations
                        {
                            MinPos = 10,
                            MinVel = 5
                        };
                        //rb.DynamicActor.MaxLinearVelocity = 1f;
                        //rb.DynamicActor.MaxAngularVelocity = 1f;
                    }
                });

                _chassis.AddChild(mesh);
                return mesh;
            }




            Debug.Assert(WheelFL != null && WheelFR != null && WheelBL != null && WheelBR != null);

            /*
             t1  |   t3  | t2
            p1--p3--p5--p4--p2
                     |
                     | t5
                     |
             p6-----p8------p7
                    t4
           */

            var p1 = WheelFL.WorldBounds.Center;// + new Vector3(WheelFL.WorldBounds.Size.X / 2, 0, 0);

            var p2 = WheelFR.WorldBounds.Center;// - new Vector3(WheelFL.WorldBounds.Size.X / 2, 0, 0); ;

            var l1 = new Line3(p1, p2);

            var p3 = l1.PointAt(WheelFL.WorldBounds.Size.X / 2);

            var p4 = l1.PointAt(l1.Length() - WheelFL.WorldBounds.Size.X / 2);

            var p5 = l1.Center();

            var p6 = WheelBL.WorldBounds.Center;

            var p7 = WheelBR.WorldBounds.Center;

            var l2 = new Line3(p6, p7);

            var p8 = l2.Center();

            var l3 = new Line3(p5, p8).Expand(-size / 2f, -size / 2f);
            p5 = l3.From;
            p8 = l3.To;

            _wheelBase = l3.Length();
            _trackWidth = Vector3.Distance(p3, p4);


            var t1 = AddTube(p1, p3);
            var t2 = AddTube(p2, p4);
            var t3 = AddTube(p3, p4);
            var t4 = AddTube(p6, p7);
            var t5 = AddTube(p5, p8);

            _rotateLeft = AddRotationV2(WheelFL, t1, p1, Vector3.UnitY, true);
            _rotateRight = AddRotationV2(WheelFR, t2, p2, Vector3.UnitY, true);

            AddRotationV2(WheelBL, t4, p6, Vector3.UnitX);
            AddRotationV2(WheelBR, t4, p7, Vector3.UnitX);

            AddFixed(t5, t4, p8);
            AddFixed(t5, t3, p5);

            D6JointOptions options;

            _steerLeft = AddRotation(t1, t3, p3, Vector3.UnitY, false);

            options = (D6JointOptions)_steerLeft.Options!;
            options.MotionSwing1 = PhysX.PxD6Motion.Limited;
            options.SwingLimit = new PhysX.PxJointLimitCone
            {
                yAngle = 1,
            };
            options.DriveSwing = new PhysX.PxD6JointDrive
            {
                stiffness = SteeringStiffness,
                damping = 10,
                forceLimit = 1e6f
            };

            _steerRight = AddRotation(t2, t3, p4, Vector3.UnitY, false);

            options = (D6JointOptions)_steerRight.Options!;
            options.MotionSwing1 = PhysX.PxD6Motion.Limited;
            options.SwingLimit = new PhysX.PxJointLimitCone
            {
                yAngle = 1,
            };
            options.DriveSwing = new PhysX.PxD6JointDrive
            {
                stiffness = SteeringStiffness,
                damping = 10,
                forceLimit = 1e6f
            };

            //t1.Transform.LocalPivot = t1.ToLocal(p3);
            //t2.Transform.LocalPivot = t2.ToLocal(p4);

            _host!.AddChild(_chassis);

            _mainTube = t5;
        }


        void ApplySteering()
        {
            if (_steerLeft == null || _steerRight == null || !_steerLeft.IsCreated || !_steerRight.IsCreated)
                return;

            if (_steerLeft.D6Joint.DriveSwing.stiffness != SteeringStiffness)
            {
                var curLimit = _steerLeft.D6Joint.DriveSwing;
                curLimit.stiffness = SteeringStiffness;
                _steerLeft.D6Joint.DriveSwing = curLimit;

                curLimit = _steerRight.D6Joint.DriveSwing;
                curLimit.stiffness = SteeringStiffness;
                _steerRight.D6Joint.DriveSwing = curLimit;
            }

            _steerLeft.D6Joint.DrivePosition = new Pose3
            {
                Position = new Vector3(0, 0, 0),
                Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, SteeringAngle)
            };

            _steerRight.D6Joint.DrivePosition = new Pose3
            {
                Position = new Vector3(0, 0, 0),
                Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, SteeringAngle)
            };
        }

        void ApplyDifferential(float avgAngle)
        {
            if (_rotateLeft == null || _rotateRight == null || !_rotateLeft.IsCreated || !_rotateRight.IsCreated)
                return;

            float ratio = 1;

            if (!float.IsNaN(avgAngle))
            {
                var turnRadius = _wheelBase / MathF.Tan(avgAngle);

                var rInner = turnRadius - (_trackWidth / 2);
                var rOuter = turnRadius + (_trackWidth / 2);

                ratio = rOuter / rInner;
            }

            _rotateLeft!.RevoluteJoint.DriveVelocity = WheelSpeedRad * ratio;
            _rotateRight!.RevoluteJoint.DriveVelocity = WheelSpeedRad;

            // Log.Value("Left", WheelSpeedRad * ratio);
            // Log.Value("Right", WheelSpeedRad);
        }

        protected void SyncCarBody()
        {

            //var newPose = _mainTube!.GetWorldPose().Multiply(_bodyMainDeltaPose);
            //_carRigidBody!.DynamicActor.KinematicTarget = newPose;
            //CarBody!.SetWorldPoseIfChanged(newPose);
        }

        protected override void Update(RenderContext ctx)
        {
            if (_steerLeft != null && _steerLeft.IsCreated)
            {
                var avgAngle = (_steerLeft!.D6Joint.SwingYAngle + _steerRight!.D6Joint.SwingYAngle) / 2;

                if (_isChanged || MathF.Abs(_lastAngle - avgAngle) > 0.01f)
                {
                    ApplyDifferential(avgAngle);
                    _lastAngle = avgAngle;
                }
            }

            if (_isChanged)
            {
                ApplySteering();
                _isChanged = false;
            }

            SyncCarBody();
        }

        public void DrawGizmos(Canvas3D canvas)
        {

        }

        [Range(-1, 1, 0.01f)]
        public float SteeringAngle
        {
            get => _steeringAngle;
            set
            {
                _steeringAngle = value;
                _isChanged = true;
            }
        }


        [Range(0, 10, 0.1f)]
        public float WheelSpeedRad
        {
            get => _wheelSpeedRad;
            set
            {
                _wheelSpeedRad = value;
                _isChanged = true;
            }
        }

        public float SteeringStiffness { get; set; }

        public float WheelMass { get; set; }

        public float ChassisDensity { get; set; }

        public float CarBodyDensity { get; set; }

        public Object3D? WheelFL { get; set; }

        public Object3D? WheelFR { get; set; }

        public Object3D? WheelBL { get; set; }

        public Object3D? WheelBR { get; set; }

        public Object3D? SteeringWheel { get; set; }

        public Object3D? CarBody { get; set; }

        public IEnumerable<TriangleMesh>? CarBodyCollisionMeshes { get; set; }
    }
}
