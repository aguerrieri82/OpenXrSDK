﻿
using OpenXr.Framework;
using PhysX;
using PhysX.Framework;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrMath;
using IDrawGizmos = XrEngine.IDrawGizmos;

namespace XrSamples
{
    public class EngineModel
    {
        // Constants
        readonly double M = 1500;           // Vehicle mass (kg)
        readonly double r = 0.3;            // Wheel radius (m)
        readonly double T_engine_max = 50; // Max engine torque (Nm)
        readonly double FDR = 4.0;          // Final drive ratio
        readonly double efficiency = 0.9;   // Transmission efficiency


        // Resistance constants
        readonly double C_rr = 0.015;       // Rolling resistance coefficient
        readonly double g = 9.81;           // Gravitational acceleration (m/s^2)
        readonly double rho = 1.225;        // Air density (kg/m^3)
        readonly double Cd = 0.8;           // Drag coefficient 
        readonly double A = 2.2;            // Frontal area (m^2)

        // Initial conditions
        double v = 0.0;            // Vehicle speed (m/s)
        double s = 0.0;            // Distance traveled (m)

        double omega_wheel;
        double engineRPM;



        public void Step(float dt)
        {
            // 1. Engine torque
            double T_engine = Acceleration * T_engine_max;

            // 2. Torque at wheels
            double T_wheel = T_engine * Gear * FDR * efficiency;

            // 3. Force at wheels
            double F_wheel = T_wheel / r;

            // 4a. Rolling resistance
            double F_rolling = C_rr * M * g;

            // 4b. Aerodynamic drag
            double F_drag = 0.5 * rho * Cd * A * v * v;

            // 5. Net force
            double F_net = F_wheel - (F_rolling + F_drag);

            // 6. Acceleration
            double a = F_net / M;

            // 7. Update speed
            v += a * dt;
            if (v < 0)
                v = 0; // Ensure speed doesn't become negative

            // 8. Update position
            s += v * dt;

            // 9a. Wheel angular velocity and RPM
            omega_wheel = v / r; // (rad/s)

            // 9b. Engine angular velocity and RPM
            double omega_engine = omega_wheel * Gear * FDR;
            engineRPM = omega_engine * (60 / (2 * Math.PI));
        }


        public float Acceleration = 0;

        public float Gear = 3.5f;

        public float OmegaWheel => (float)omega_wheel;

        public float EngineRPM => (float)engineRPM;


    }

    public class CarModel : Behavior<Group3D>, IDrawGizmos
    {
        private Group3D? _chassis;
        private readonly Group3D _attachedGroup;

        private Joint? _steerLeft;
        private Joint? _steerRight;
        private Joint? _rotateLeft;
        private Joint? _rotateRight;
        private Joint? _steeringWheelJoint;

        private float _wheelBase;
        private float _trackWidth;
        private float _wheelRadius;

        private TriangleMesh? _mainTube;
        private TriangleMesh? _steeringWheelTube;
        private RigidBody? _carRigidBody;
        private readonly IPbrMaterial _tubeMaterial;
        private readonly float _tubeSize;

        private float _lastAngle;
        private float _steeringAngle;
        private float _wheelSpeedRad;
        private bool _isWheelChanged;
        private readonly CarSound _carSound;
        private Pose3 _attachedPosDiff;
        private Pose3 _seatPosDiff;

        private PhysicsManager? _manager;
        private float _wheelDensity;
        private float _chassisDensity;
        private float _carBodyDensity;
        private float _wheelFriction;
        private TriangleMesh? _gearBox;
        private TriangleMesh? _gearLever;
        private Dictionary<string, Vector2>? _gears;
        private string _curGear;
        private readonly EngineModel _engine;

        public CarModel()
        {
            WheelDensity = 50;
            ChassisDensity = 10000;
            CarBodyDensity = 1;
            SteeringStiffness = 3000;
            PosIterations = 50;
            UseDifferential = true;
            SteeringRatio = 12;
            SteeringLimitRad = 0.9f;
            UseSteeringPhysics = true;
            WheelFriction = 0.8f;

            _tubeMaterial = MaterialFactory.CreatePbr("#00ff0080");
            _tubeMaterial.Metalness = 1;
            _tubeMaterial.Alpha = AlphaMode.Blend;
            _tubeSize = 0.05f;

            _attachedGroup = new Group3D
            {
                Name = "attached"
            };

            _curGear = "1";

            _carSound = new CarSound();

            _engine = new EngineModel();

            UpdatePriority = 1;
        }

        protected override void OnAttach()
        {
            _host!.AddChild(_attachedGroup);
            base.OnAttach();
        }

        public void Create()
        {
            AttachWheels();
            CreateChassis();
            AttachSteering();
            AttachBody();
            CreateGearBox(false);

            CarBody!.AddComponent(_carSound);

            _attachedPosDiff = _mainTube!.GetWorldPose().Difference(_attachedGroup.GetWorldPose());
        }


        protected void AttachBody()
        {

            Debug.Assert(CarBody != null && _mainTube != null);

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
                CollideGroup = RigidBodyGroup.Group1,
                AngularDamping = 100,

            });

            _carRigidBody.Contact += OnContact;

            var joint = AddFixed(_mainTube, CarBody, _mainTube.WorldBounds.Center);

            _seatPosDiff = _mainTube.GetWorldPose().Difference(_host!.GetWorldPose().Multiply(SeatLocalPose));
        }

        protected void AttachWheels()
        {
            Object3D[] wheels = [WheelBL!, WheelBR!, WheelFR!, WheelFL!];

            WheelBL!.Name = "wheel-back-left";
            WheelBR!.Name = "wheel-back-right";
            WheelFL!.Name = "wheel-front-left";
            WheelFR!.Name = "wheel-front-right";

            var pyMaterial = new PhysicsMaterialInfo
            {
                DynamicFriction = WheelFriction,
                StaticFriction = WheelFriction,
                Restitution = 0.3f,
                ForceNew = true
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
                    MaterialInfo = pyMaterial,
                    IsEnabled = true,
                    CollideGroup = RigidBodyGroup.Group1,
                    AutoTeleport = false,
                    Density = WheelDensity,
                    PositionMode = PositionMode.LocalPivot,
                    Configure = rb =>
                    {
                        rb.DynamicActor.MaxDepenetrationVelocity = 1f;
                        rb.DynamicActor.SolverIterations = new SolverIterations
                        {
                            MinPos = PosIterations,
                            MinVel = 5
                        };
                    }
                });

                wheel.Transform.SetLocalPivot(wheel.ToLocal(wheel.WorldBounds.Center), true);
            }
        }

        protected void AttachSteering()
        {
            Debug.Assert(SteeringWheel != null && _mainTube != null);

            SteeringWheel.Name = "steering-wheel";

            SteeringWheel.AddComponent(new PyMeshCollider
            {
                UseConvexMesh = false
            });

            if (UseSteeringPhysics)
            {
                var worldPose = SteeringWheel.GetWorldPose().Multiply(SteeringLocalPose);

                var dir = -Vector3.UnitZ.Transform(worldPose.Orientation);

                var p2 = worldPose.Position + dir * 1f;

                _steeringWheelTube = AddTube("ts", worldPose.Position, worldPose.Position + dir * 1f, PhysicsActorType.Kinematic);

                _attachedGroup.AddChild(_steeringWheelTube, true);

                SteeringWheel.AddComponent<ForceTarget>();

                _steeringWheelJoint = AddRotation(SteeringWheel, _steeringWheelTube, worldPose.Position, Vector3.UnitZ);
                _steeringWheelJoint.Pose0 = SteeringLocalPose;

                var options = (D6JointOptions)_steeringWheelJoint.Options!;

                options.MotionSwing2 = PxD6Motion.Limited;
                options.SwingLimit = new PxJointLimitCone
                {
                    zAngle = MathF.PI * 2,
                    stiffness = 1000,
                    damping = 100,
                    bounceThreshold = 100
                };
                options.DriveSwing = null;
            }
            else
            {
                var rotate = new InputRotateAxis
                {
                    RotationAxis = new Ray3
                    {
                        Origin = SteeringLocalPose.Position,
                        Direction = -Vector3.UnitZ.Transform(SteeringLocalPose.Orientation).Normalize()
                    },
                    MinAngle = -SteeringLimitRad * SteeringRatio,
                    MaxAngle = SteeringLimitRad * SteeringRatio,
                    MaxDistance = 0.30f
                };

                SteeringWheel.AddComponent(rotate);

                _attachedGroup.AddChild(SteeringWheel, true);
            }
        }

        protected void CreateChassis()
        {
            _chassis = new Group3D
            {
                Name = "chassis"
            };


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

            var l3 = new Line3(p5, p8).Expand(-_tubeSize / 2f, -_tubeSize / 2f);
            p5 = l3.From;
            p8 = l3.To;

            _wheelBase = l3.Length();
            _trackWidth = Vector3.Distance(p3, p4);

            var t1 = AddTube("t1", p1, p3);
            var t2 = AddTube("t2", p2, p4);
            var t3 = AddTube("t3", p3, p4);
            var t4 = AddTube("t4", p6, p7);
            var t5 = AddTube("t5", p5, p8);

            _rotateLeft = AddRotationV2(WheelFL, t1, p1, Vector3.UnitY, true);
            _rotateRight = AddRotationV2(WheelFR, t2, p2, Vector3.UnitY, true);

            //_rotateLeft.Options.InvInertiaScale0 = 2;
            //_rotateRight.Options.InvInertiaScale0 = 2f;

            AddRotationV2(WheelBL, t4, p6, Vector3.UnitX);
            AddRotationV2(WheelBR, t4, p7, Vector3.UnitX);

            AddFixedV2(t5, t4, p8);
            AddFixedV2(t5, t3, p5);

            _steerLeft = AddFixedV2(t1, t3, p3);
            _steerRight = AddFixedV2(t2, t3, p4);

            _host!.AddChild(_chassis);

            _mainTube = t5;
        }


        private void OnContact(Object3D self, Object3D other, int otherIndex, ContactPair[] pairs)
        {
            Log.Debug(this, "Contact {0} with {1}", self.Name, other.Name);
        }

        TriangleMesh AddTube(string name, Vector3 p1, Vector3 p2, PhysicsActorType type = PhysicsActorType.Dynamic, float size = 0.05f)
        {
            Debug.Assert(_chassis != null);

            var line = new Line3(p1, p2);
            var cube = new Cube3D(new Vector3(size, size, line.Length()));

            var mesh = new TriangleMesh(cube, (Material)_tubeMaterial)
            {
                WorldPosition = line.Center(),
                Forward = -line.Direction(),
                Name = name
            };

            mesh.AddComponent<BoxCollider>();
            mesh.AddComponent(new RigidBody
            {
                Type = type,
                AutoTeleport = type == PhysicsActorType.Static,
                CollideGroup = RigidBodyGroup.Group1,
                Density = ChassisDensity,
                Configure = rb =>
                {
                    if (type == PhysicsActorType.Static)
                        return;

                    rb.DynamicActor.MaxLinearVelocity = 100;
                    rb.DynamicActor.MaxAngularVelocity = 100;
                    rb.DynamicActor.MaxDepenetrationVelocity = 1f;
                    rb.DynamicActor.SolverIterations = new SolverIterations
                    {
                        MinPos = PosIterations,
                        MinVel = 5
                    };
                }
            });

            _chassis.AddChild(mesh);

            return mesh;
        }

        Joint AddFixedV2(Object3D obj0, Object3D obj1, Vector3 point)
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

            var joint = manager.AddJoint(JointType.Fixed, obj0, pose0, obj1, pose1);

            return joint;
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

            var joint = manager.AddJoint(JointType.D6, obj0, pose0, obj1, pose1);

            var drive = new PxD6JointDrive
            {
                forceLimit = 1000,
                stiffness = 100000,
                damping = 10
            };

            joint.Options = new D6JointOptions()
            {
                DriveX = drive,
                DriveY = drive,
                DriveZ = drive,
                DriveSlerp = drive,
                DriveSwing = drive,
                DriveTwist = drive,
            };

            return joint;
        }

        Joint AddRotation(Object3D obj0, Object3D obj1, Vector3 point, Vector3 axis, bool motor = false)
        {
            var joint = AddFixed(obj0, obj1, point);

            var opt = (D6JointOptions)joint.Options!;

            if (axis == Vector3.UnitX)
            {
                opt.MotionTwist = PxD6Motion.Free;
            }
            else if (axis == Vector3.UnitY)
            {
                opt.MotionSwing1 = PxD6Motion.Free;
            }
            else if (axis == Vector3.UnitZ)
            {
                opt.MotionSwing2 = PxD6Motion.Free;
            }

            opt.ConstraintFlags = PxConstraintFlags.CollisionEnabled;

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

            var joint = manager.AddJoint(JointType.Revolute, obj0, pose0, obj1, pose1);

            var options = new RevoluteJointOptions();
            if (motor)
                options.RevoluteJointFlags |= PxRevoluteJointFlags.DriveEnabled;

            options.DriveGearRatio = 0.01f;

            joint.Options = options;

            return joint;
        }

        Joint AddSpherical(Object3D obj0, Object3D obj1, Vector3 point)
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

            var joint = manager.AddJoint(JointType.Spherical, obj0, pose0, obj1, pose1);

            joint.Options = new SphericalJointOptions();

            return joint;
        }


        protected Pose3 GetPoseRef(Pose3 deltaRef, Pose3 lastPose)
        {
            var curPose = _mainTube!.GetWorldPose();

            if (XrApp.Current != null)
            {
                var rb = _mainTube!.Component<RigidBody>();

                if (rb.IsCreated)
                {
                    var curVel = rb.DynamicActor.LinearVelocity;
                    var nextPos = (float)XrApp.Current.FramePredictedDisplayPeriod.TotalSeconds * curVel;
                    curPose.Position += nextPos;
                }
            }

            var newPose = curPose.Multiply(deltaRef);

            return lastPose.Lerp(newPose, 0.9f);
        }

        void ApplySteering()
        {
            if (_steerLeft == null || _steerRight == null || !_steerLeft.IsCreated || !_steerRight.IsCreated)
                return;

            _steerLeft.BaseJoint.LocalPose0 = new Pose3
            {
                Position = _steerLeft.BaseJoint.LocalPose0.Position,
                Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _steeringAngle - MathF.PI / 2)
            };

            _steerRight.BaseJoint.LocalPose0 = new Pose3
            {
                Position = _steerRight.BaseJoint.LocalPose0.Position,
                Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _steeringAngle + MathF.PI / 2)
            };
        }

        void ApplyDifferential(float avgAngle)
        {
            if (_rotateLeft == null || _rotateRight == null || !_rotateLeft.IsCreated || !_rotateRight.IsCreated)
                return;

            float ratio = 1;

            if (!float.IsNaN(avgAngle) && avgAngle != 0 && UseDifferential)
            {
                var turnRadius = _wheelBase / MathF.Tan(avgAngle);

                var rInner = turnRadius - (_trackWidth / 2);
                var rOuter = turnRadius + (_trackWidth / 2);

                ratio = rOuter / rInner;
            }

            _rotateLeft!.RevoluteJoint.DriveVelocity = WheelSpeedRad * ratio;
            _rotateRight!.RevoluteJoint.DriveVelocity = WheelSpeedRad;
        }

        protected void SyncCarBody()
        {
            var newPose = _mainTube!.GetWorldPose().Multiply(_attachedPosDiff);

            _attachedGroup.SetWorldPoseIfChanged(newPose);

            if (UseSteeringPhysics)
                _steeringWheelTube!.Component<RigidBody>().DynamicActor.KinematicTarget = _steeringWheelTube!.GetWorldPose();

        }

        protected void SyncCamera()
        {
            if (XrApp.Current == null)
                return;

            XrApp.Current.ReferenceFrame = GetPoseRef(_seatPosDiff, XrApp.Current.ReferenceFrame);
        }


        protected void SyncSteering()
        {
            float wheelAngle;
            if (UseSteeringPhysics)
            {
                wheelAngle = _steeringWheelJoint!.D6Joint.SwingZAngle * 0.5f;
            }
            else
            {
                var input = SteeringWheel!.Component<InputRotateAxis>();
                wheelAngle = input.Angle / SteeringRatio;
            }

            SteeringAngle = wheelAngle;
        }

        protected void SyncInput()
        {
            var dir = BackInput != null && (BackInput.IsActive && BackInput.Value || _curGear == "R") ? -1 : 1;

            if (AccInput != null && AccInput.IsActive)
                WheelSpeedRad = AccInput.Value * 10f * dir;
            /*
            WheelSpeedRad = _engine.OmegaWheel * dir;
            */

            if (ShowHideBodyInput != null && ShowHideBodyInput.IsActive && ShowHideBodyInput.IsChanged && ShowHideBodyInput.Value)
                CarBody!.IsVisible = !CarBody!.IsVisible;
        }



        protected void SyncGear()
        {
            var topFace = _gearBox!.LocalBounds.Faces().Front;
            var plane = topFace.ToPlane();

            var ray = new Ray3(_gearLever!.WorldPosition, _gearLever.Forward);
            var localRay = ray.Transform(_gearBox.WorldMatrixInverse);

            if (localRay.Intersects(plane, out var localPoint))
            {
                var uv = topFace.LocalPointAt(localPoint) / topFace.Size;
                uv.Y = 1 - uv.Y;
                foreach (var gear in _gears!)
                {
                    if ((uv - gear.Value).Length() < 0.1)
                    {
                        _curGear = gear.Key;
                        return;
                    }
                }
            }
        }

        protected void SyncSound()
        {
            var gear = _curGear == "R" ? 1 : int.Parse(_curGear);

            _carSound.Engine.Gear = gear;
            _carSound.Engine.Rpm = 40 + (int)_engine.EngineRPM;
        }

        protected override void Update(RenderContext ctx)
        {
            _engine.Acceleration = AccInput != null && AccInput.IsActive ? AccInput.Value : AccInputSim / 100f;
            _engine.Step((float)_deltaTime);

            SyncSteering();

            SyncInput();

            SyncGear();

            SyncSound();

            _manager ??= _host!.Scene!.Component<PhysicsManager>();

            _manager.Execute(() =>
            {
                if (_steerLeft != null && _steerLeft.IsCreated)
                {
                    //var avgAngle = (_steerLeft!.D6Joint.SwingYAngle + _steerRight!.D6Joint.SwingYAngle) / 2;
                    var avgAngle = _steeringAngle;
                    if (_isWheelChanged || MathF.Abs(_lastAngle - avgAngle) > 0.01f)
                    {
                        ApplyDifferential(avgAngle);
                        _lastAngle = avgAngle;
                    }
                }

                if (_isWheelChanged)
                {
                    ApplySteering();
                    _isWheelChanged = false;
                }
            });

            SyncCarBody();

            SyncCamera();
        }

        public void DrawGizmos(Canvas3D canvas)
        {

        }

        protected void UpdateDensity(Object3D? obj, float density)
        {
            if (obj == null || !obj.TryComponent<RigidBody>(out var actor))
                return;

            actor.Density = density;

            if (actor.IsCreated)
                actor.DynamicActor.UpdateMassAndInertia(density);

            Log.Info(this, "New Mass {1} {0}", actor.DynamicActor.Mass, obj.Name);
        }

        protected void UpdateFriction()
        {
            foreach (var wheel in new Object3D?[] { WheelBL, WheelBR, WheelFL, WheelFR })
            {
                if (wheel == null || !wheel.TryComponent<RigidBody>(out var actor))
                    continue;

                if (actor.Material == null)
                {
                    actor.MaterialInfo = new PhysicsMaterialInfo
                    {
                        DynamicFriction = _wheelFriction,
                        StaticFriction = _wheelFriction,
                        Restitution = 0.3f,

                    };
                }
                else
                {
                    actor.Material.DynamicFriction = _wheelFriction;
                    actor.Material.StaticFriction = _wheelFriction;
                }
            }
        }

        protected void UpdateDensity()
        {
            foreach (var wheel in new Object3D?[] { WheelBL, WheelBR, WheelFL, WheelFR })
                UpdateDensity(wheel, _wheelDensity);

            if (_chassis != null)
            {
                foreach (var item in _chassis.Children)
                    UpdateDensity(item, _chassisDensity);
            }

            UpdateDensity(CarBody, _carBodyDensity);
        }

        void CreateGearBox(bool usePhysic)
        {
            var lines = new List<Vector2>();

            var xSize = 0.08f * 1;
            var ySize = 0.07f * 1;
            var padSize = 0.010f;
            var lineSize = 0.008f;
            var leverHeight = 0.3f;
            var leverOffset = 0.1f;


            var centerY = ySize / 2;
            var boxSizeX = (xSize - lineSize * 3 - padSize * 2) / 2;
            var boxSizeY = (ySize - lineSize - padSize * 2) / 2;

            lines.Add(new Vector2(0, 0));
            lines.Add(new Vector2(xSize, padSize));

            lines.Add(new Vector2(0, ySize));
            lines.Add(new Vector2(xSize, ySize - padSize));

            lines.Add(new Vector2(0, padSize));
            lines.Add(new Vector2(padSize, ySize - padSize));

            lines.Add(new Vector2(xSize, padSize));
            lines.Add(new Vector2(xSize - padSize, ySize - padSize));

            var b0 = new Vector2(padSize + lineSize, padSize);
            var b1 = new Vector2(padSize + lineSize + boxSizeX, padSize + boxSizeY);

            lines.Add(b0);
            lines.Add(b1);

            var b2 = b0 + new Vector2(boxSizeX + lineSize, 0);
            var b3 = b1 + new Vector2(boxSizeX + lineSize, 0);
            lines.Add(b2);
            lines.Add(b3);

            var b4 = b0 + new Vector2(0, boxSizeY + lineSize);
            var b5 = b1 + new Vector2(0, boxSizeY + lineSize);
            lines.Add(b4);
            lines.Add(b5);

            b4 += new Vector2(boxSizeX + lineSize, 0);
            b5 += new Vector2(boxSizeX + lineSize, 0);
            lines.Add(b4);
            lines.Add(b5);


            var boxMesh = new TriangleMesh();
            var builder = new MeshBuilder();
            var offset = new Vector2(xSize, ySize) / -2f;
            for (var i = 0; i < lines.Count; i += 2)
            {
                var l0 = lines[i];
                var l1 = lines[i + 1];
                var size = l1 - l0;
                var center = (l0 + l1) / 2;

                builder.AddCube(new Vector3(center + offset, 0), new Vector3(size, 0.01f));
            }

            var mat = MaterialFactory.CreatePbr("#000000");
            mat.DoubleSided = true;
            mat.Roughness = 0.2f;
            mat.Metalness = 1;

            boxMesh.Geometry = builder.ToGeometry();
            boxMesh.Materials.Add((Material)mat);
            boxMesh.SetWorldPose(GearBoxPose);
            builder.AddColliders(boxMesh);

            _attachedGroup.AddChild(boxMesh);


            builder = new MeshBuilder();
            builder.AddCylinder(Vector3.Zero, (lineSize * 1.5f / 2f) - 0.001f, leverHeight, 10, UVMode.Normalized)
                   .AddSphere(Vector3.Zero, 0.02f, 20);

            var leverMesh = new TriangleMesh(builder.ToGeometry());
            leverMesh.Geometry!.SmoothNormals();
            leverMesh.Materials.Add((Material)mat);
            leverMesh.SetWorldPose(GearBoxPose.Multiply(new Pose3(new Vector3(0, 0, -leverOffset))));
            builder.AddColliders(leverMesh);

            if (usePhysic)
            {
                leverMesh.AddComponent(new ForceTarget());

                boxMesh.AddComponent(new RigidBody
                {
                    Type = PhysicsActorType.Static,
                    EnableCCD = true,
                    CollideGroup = RigidBodyGroup.Group1 | RigidBodyGroup.Group2
                });

                leverMesh.AddComponent(new RigidBody
                {
                    Type = PhysicsActorType.Dynamic,
                    CollideGroup = RigidBodyGroup.Group1 | RigidBodyGroup.Group2,
                    Density = 1000,
                    EnableCCD = true,
                    Configure = rb =>
                    {
                        rb.DynamicActor.SolverIterations = new SolverIterations
                        {
                            MinPos = 30
                        };
                    }
                });

                var point = leverMesh.WorldBounds.Faces().Bottom.Center();

                var joint = AddSpherical(leverMesh, boxMesh!, point);

                var opt = (SphericalJointOptions)joint.Options!;
                opt.SphericalFlags |= PxSphericalJointFlags.LimitEnabled;
                opt.Limit = new PxJointLimitCone
                {
                    yAngle = MathF.PI,
                    zAngle = MathF.PI,
                    bounceThreshold = 10f,
                    stiffness = 1000000,
                    damping = 30000,
                };

                _chassis!.AddChild(leverMesh);
            }
            else
            {
                var point = leverMesh.WorldBounds.Faces().Bottom.Center();

                leverMesh.AddComponent(new InputRotatePivot
                {
                    LocalPivot = new Vector3(0, 0, leverHeight),
                    Normal = Vector3.UnitZ,
                    ValidateOrientation = (worldOri) =>
                    {
                        var dir = (-Vector3.UnitZ).Transform(worldOri).Normalize();
                        var line = new Line3(leverMesh.WorldPosition, leverMesh.WorldPosition + dir * leverHeight);
                        var localLine = line.Transform(boxMesh.WorldMatrixInverse);

                        var topFace = _gearBox!.LocalBounds.Faces().Front;
                        var plane = topFace.ToPlane();
                        var ray = new Ray3(leverMesh!.WorldPosition, dir);
                        var localRay = ray.Transform(_gearBox.WorldMatrixInverse);

                        if (!localRay.Intersects(plane, out var localPoint))
                            return false;

                        var uv = topFace.LocalPointAt(localPoint) / topFace.Size;
                        if (uv.X < 0 || uv.X > 1 || uv.Y < 0 || uv.Y > 1)
                            return false;

                        foreach (var collider in boxMesh.Components<BoxCollider>())
                        {
                            var bounds = new Bounds3()
                            {
                                Max = collider.Center + collider.Size / 2,
                                Min = collider.Center - collider.Size / 2,
                            };

                            if (bounds.Intersects(localLine, out _))
                                return false;
                        }

                        return true;
                    }
                });

                _attachedGroup.AddChild(leverMesh);
            }

            _gearBox = boxMesh;
            _gearLever = leverMesh;

            _gears = new Dictionary<string, Vector2>
            {
                ["R"] = new Vector2(0.8f, 0.8f),
                ["1"] = new Vector2(0.2f, 0.2f)
            };
        }

        public void AddMirror(Group3D obj, Ray3 worldPivot)
        {
            var mirror = (TriangleMesh)obj.Children[0];
            mirror.Materials.Clear();
            mirror.Materials.Add(new MirrorMaterial
            {
                TextureSize = 512,
                Mode = MirrorMode.Full,
                DoubleSided = false
            });

            obj.AddComponent(new PyMeshCollider
            {

            });

            obj.AddComponent(new InputRotatePivot
            {
                LocalPivot = worldPivot.Origin,
                Normal = worldPivot.Direction
            });

            _attachedGroup.AddChild(obj, true);
        }

        public void ConfigureInput(IXrBasicInteractionProfile input)
        {
            AccInput = input.Right!.TriggerValue;
            BackInput = input.Right!.Button!.AClick;
            ShowHideBodyInput = input.Right!.Button!.BClick;

            if (!UseSteeringPhysics)
            {
                var rotate = SteeringWheel!.Component<InputRotateAxis>();
                rotate.ConfigureInput(input);
            }

            foreach (var item in _attachedGroup.Children)
            {
                if (item.TryComponent<InputRotatePivot>(out var rotate))
                    rotate.ConfigureInput(input);
            }
        }


        [Range(-1, 1, 0.01f)]
        public float SteeringAngle
        {
            get => _steeringAngle;
            set
            {
                _steeringAngle = value;

                if (!UseSteeringPhysics)
                    SteeringWheel!.Component<InputRotateAxis>().Angle = value * SteeringRatio;

                _isWheelChanged = true;
            }
        }


        [Range(0, 10, 0.1f)]
        public float WheelSpeedRad
        {
            get => _wheelSpeedRad;
            set
            {
                _wheelSpeedRad = value;
                _isWheelChanged = true;
            }
        }


        [Range(0, 50000, 1)]
        public float WheelDensity
        {
            get => _wheelDensity;
            set
            {
                _wheelDensity = value;
                UpdateDensity();
            }
        }

        [Range(0, 50000, 1)]
        public float ChassisDensity
        {
            get => _chassisDensity;
            set
            {
                _chassisDensity = value;
                UpdateDensity();
            }
        }

        [Range(0, 50000, 1)]
        public float CarBodyDensity
        {
            get => _carBodyDensity;
            set
            {
                _carBodyDensity = value;
                UpdateDensity();
            }
        }

        [Range(0, 5, 0.01f)]
        public float WheelFriction
        {
            get => _wheelFriction;
            set
            {
                _wheelFriction = value;
                UpdateFriction();
            }
        }

        [Range(0, 100, 0.5f)]
        public float AccInputSim { get; set; }

        public bool UseDifferential { get; set; }

        public bool UseSteeringPhysics { get; set; }

        public Pose3 SteeringLocalPose { get; set; }

        public float SteeringStiffness { get; set; }

        public float SteeringRatio { get; set; }

        public float SteeringLimitRad { get; set; }

        public Pose3 SeatLocalPose { get; set; }

        public XrFloatInput? AccInput { get; set; }

        public XrBoolInput? BackInput { get; set; }

        public XrBoolInput? ShowHideBodyInput { get; set; }

        public uint PosIterations { get; set; }

        public Object3D? WheelFL { get; set; }

        public Object3D? WheelFR { get; set; }

        public Object3D? WheelBL { get; set; }

        public Object3D? WheelBR { get; set; }

        public Object3D? SteeringWheel { get; set; }

        public Object3D? CarBody { get; set; }

        public Pose3 GearBoxPose { get; set; }

        public IEnumerable<TriangleMesh>? CarBodyCollisionMeshes { get; set; }
    }
}
