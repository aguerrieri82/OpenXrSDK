
using OpenXr.Framework;
using PhysX;
using PhysX.Framework;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrMath;
using IDrawGizmos = XrEngine.IDrawGizmos;

namespace XrSamples
{
    public unsafe class CarModelV2 : Behavior<Group3D>, IDrawGizmos
    {
        private Group3D? _chassis;
        private readonly Group3D _attachedGroup;

        private Joint? _steeringWheelJoint;
        private TriangleMesh? _mainTube;
        private TriangleMesh? _steeringWheelTube;
        private readonly IPbrMaterial _tubeMaterial;

        private PhysicsManager? _manager;
        private RigidBody? _carRigidBody;

        private nint _vehicleWorld;
        private nint _vehicle;
        private VehicleNative.VehicleState _vehicleState;

        private readonly Pose3[] _wheelVisualOffsets = new Pose3[4];
        private Pose3 _bodyToFrame;
        private Pose3 _bodyToAttached;
        private Pose3 _seatPosDiff;

        private float _steeringAngle;
        private float _carBodyDensity;
        private float _wheelFriction;

        private TriangleMesh? _gearBox;
        private TriangleMesh? _gearLever;
        private Dictionary<string, Vector2>? _gears;
        private string _curGear;
        private readonly CarSound _carSound;

        public CarModelV2()
        {
            CarBodyDensity = 140;
            SuspensionTravel = 0.12f;
            SuspensionStiffness = 20000;
            SuspensionDamping = 2500;
            FrameTubeSize = 0.05f;
            PosIterations = 50;
            SteeringRatio = 12;
            SteeringLimitRad = 0.9f;
            UseSteeringPhysics = true;
            WheelFriction = 0.8f;
            WheelMass = 20;

            MaxMotorTorque = 50;
            MaxBrakeTorque = 1500;
            MaxHandBrakeTorque = 2500;
            IdleMotorRpm = 800;
            MaxMotorRpm = 7000;
            DriveType = VehicleNative.VehicleDriveType.Rear;

            _tubeMaterial = MaterialFactory.CreatePbr("#00ff0080");
            _tubeMaterial.Metalness = 1;
            _tubeMaterial.Alpha = AlphaMode.Blend;

            _attachedGroup = new Group3D
            {
                Name = "attached"
            };

            _curGear = "1";
            _carSound = new CarSound();

            UpdatePriority = 1;
        }

        protected override void OnAttach()
        {
            _host.AddChild(_attachedGroup);
            base.OnAttach();
        }

        public void Create()
        {
            AttachWheels();
            CreateChassis();
            AttachSteering();
            AttachBody();
            CreateGearBox();

            CarBody!.AddComponent(_carSound);

            var bodyPose = CarBody.GetWorldPose();
            _bodyToFrame = bodyPose.Difference(_mainTube!.GetWorldPose());
            _bodyToAttached = bodyPose.Difference(_attachedGroup.GetWorldPose());
            _seatPosDiff = bodyPose.Difference(_host.GetWorldPose().Multiply(SeatLocalPose));

            InitializeWheelVisualOffsets();
        }

        protected void AttachBody()
        {
            Debug.Assert(CarBody != null);

            var collider = new PyMeshCollider
            {
                UseConvexMesh = true
            };

            if (CarBodyCollisionMeshes != null)
                collider.MeshObjects = () => CarBodyCollisionMeshes;

            var collider2 = new BoxColliderV2()
            {
                Size = new Vector3(0.1f),
                Pose = Pose3.Identity
            };

            CarBody.AddComponent(collider);

            _carRigidBody = CarBody.AddComponent(new RigidBody
{
    Type = PhysicsActorType.Dynamic,
    AutoTeleport = false,
    Density = CarBodyDensity,
    CollideGroup = RigidBodyGroup.Group1,
    LinearDamping = 0.05f,
    AngularDamping = 0.1f,
    PositionMode = PositionMode.Origin,
    SimulationDisabled = true,
    DebugVisible = false,
    Configure = rb =>
    {
        rb.Actor.SetFlag(PxActorFlag.DisableGravity, true);

        rb.DynamicActor.MaxLinearVelocity = 100;
        rb.DynamicActor.MaxAngularVelocity = 20;
        rb.DynamicActor.MaxDepenetrationVelocity = 1f;

        rb.DynamicActor.SolverIterations = new SolverIterations
        {
            MinPos = PosIterations,
            MinVel = 5
        };
    }
});

            _carRigidBody.Contact += OnContact;
        }

        protected void AttachWheels()
        {
            Debug.Assert(WheelFL != null && WheelFR != null && WheelBL != null && WheelBR != null);

            WheelBL.Name = "wheel-back-left";
            WheelBR.Name = "wheel-back-right";
            WheelFL.Name = "wheel-front-left";
            WheelFR.Name = "wheel-front-right";

            foreach (var wheel in Wheels())
                wheel.Transform.SetLocalPivot(wheel.ToLocal(wheel.WorldBounds.Center), true);
        }

        protected void AttachSteering()
        {
            Debug.Assert(SteeringWheel != null);

            SteeringWheel.Name = "steering-wheel";

            SteeringWheel.AddComponent(new PyMeshCollider
            {
                UseConvexMesh = false
            });

            if (UseSteeringPhysics)
            {
                var worldPose = SteeringWheel.GetWorldPose().Multiply(SteeringLocalPose);
                var dir = -Vector3.UnitZ.Transform(worldPose.Orientation);

                _steeringWheelTube = AddSteeringTube(worldPose.Position, worldPose.Position + dir * 1f);
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

            _host.AddChild(_chassis);

            Debug.Assert(WheelFL != null && WheelFR != null && WheelBL != null && WheelBR != null);

            var p1 = WheelFL.WorldBounds.Center;
            var p2 = WheelFR.WorldBounds.Center;
            var l1 = new Line3(p1, p2);

            var p3 = l1.PointAt(WheelFL.WorldBounds.Size.X / 2);
            var p4 = l1.PointAt(l1.Length() - WheelFL.WorldBounds.Size.X / 2);
            var p5 = l1.Center();

            var p6 = WheelBL.WorldBounds.Center;
            var p7 = WheelBR.WorldBounds.Center;
            var l2 = new Line3(p6, p7);
            var p8 = l2.Center();

            var l3 = new Line3(p5, p8).Expand(-FrameTubeSize / 2f, -FrameTubeSize / 2f);
            p5 = l3.From;
            p8 = l3.To;

            var frameOrigin = (p5 + p8) * 0.5f;
            var builder = new MeshBuilder();

            AddFrameTube(ref builder, p1 - frameOrigin, p3 - frameOrigin);
            AddFrameTube(ref builder, p2 - frameOrigin, p4 - frameOrigin);
            AddFrameTube(ref builder, p3 - frameOrigin, p4 - frameOrigin);
            AddFrameTube(ref builder, p6 - frameOrigin, p7 - frameOrigin);
            AddFrameTube(ref builder, p5 - frameOrigin, p8 - frameOrigin);

            _mainTube = new TriangleMesh(builder.ToGeometry(), (Material)_tubeMaterial)
            {
                Name = "frame",
                WorldPosition = frameOrigin
            };

            _chassis.AddChild(_mainTube, true);
        }

        void AddFrameTube(ref MeshBuilder builder, Vector3 p1, Vector3 p2)
        {
            var line = new Line3(p1, p2);
            var orientation = Vector3.UnitZ.RotationTowards(line.Direction());
            var matrix = Matrix4x4.CreateFromQuaternion(orientation) * Matrix4x4.CreateTranslation(line.Center());

            var part = new MeshBuilder();
            part.AddCube(Vector3.Zero, new Vector3(FrameTubeSize, FrameTubeSize, line.Length()));

            foreach (var source in part.Vertices)
            {
                var vertex = source;
                vertex.Pos = Vector3.Transform(vertex.Pos, matrix);
                vertex.Normal = Vector3.TransformNormal(vertex.Normal, matrix).Normalize();
                builder.Vertices.Add(vertex);
            }
        }

        Object3D[] Wheels()
        {
            return [WheelFL!, WheelFR!, WheelBL!, WheelBR!];
        }

        void InitializeWheelVisualOffsets()
        {
            Debug.Assert(CarBody != null && WheelFL != null && WheelFR != null && WheelBL != null && WheelBR != null);

            var bodyPose = CarBody.GetWorldPose();
            var wheels = Wheels();

            for (var i = 0; i < wheels.Length; i++)
            {
                var localPosition = CarBody.ToLocal(wheels[i].WorldBounds.Center);
                var vehiclePose = bodyPose.Multiply(new Pose3(localPosition));
                _wheelVisualOffsets[i] = vehiclePose.Difference(wheels[i].GetWorldPose());
            }
        }

        VehicleNative.VehicleWheelDesc CreateWheelDesc(Object3D wheel)
        {
            Debug.Assert(CarBody != null);

            var radius = wheel.WorldBounds.Size.Y * 0.5f;
            var width = wheel.WorldBounds.Size.X;

            return new VehicleNative.VehicleWheelDesc
            {
                Position = CarBody.ToLocal(wheel.WorldBounds.Center),
                Radius = radius,
                Width = width,
                Mass = WheelMass
            };
        }

        VehicleNative.VehicleAxleSimpleDesc CreateAxleDesc(Object3D left, Object3D right)
        {
            return new VehicleNative.VehicleAxleSimpleDesc
            {
                LeftWheel = CreateWheelDesc(left),
                RightWheel = CreateWheelDesc(right),
                SuspensionTravel = SuspensionTravel,
                SuspensionStiffness = SuspensionStiffness,
                SuspensionDamping = SuspensionDamping,
                TireFriction = WheelFriction
            };
        }

        void EnsureVehicle()
        {
            if (_vehicle != 0 || _carRigidBody == null || !_carRigidBody.IsCreated)
                return;

            _manager ??= _host.Scene!.Component<PhysicsManager>();
            var system = _manager.System;
            if (system == null || _carRigidBody.Material == null)
                return;

            var worldDesc = new VehicleNative.VehicleWorldDesc
            {
                Physics = (nint)Unsafe.AsPointer(ref system.Physics),
                Scene = (nint)system.Scene.Handle,
                DefaultMaterial = (nint)_carRigidBody.Material.Handle
            };

            _vehicleWorld = VehicleNative.VehicleWorldCreate(ref worldDesc);
            if (_vehicleWorld == 0)
                throw new InvalidOperationException("VehicleWorldCreate failed");

            var desc = new VehicleNative.VehicleSimpleDesc
            {
                FrontAxle = CreateAxleDesc(WheelFL!, WheelFR!),
                RearAxle = CreateAxleDesc(WheelBL!, WheelBR!),
                MaxSteeringAngle = SteeringLimitRad,
                DriveType = DriveType,
                MaxMotorTorque = MaxMotorTorque,
                MaxBrakeTorque = MaxBrakeTorque,
                MaxHandBrakeTorque = MaxHandBrakeTorque,
                IdleMotorRpm = IdleMotorRpm,
                MaxMotorRpm = MaxMotorRpm
            };

            _vehicle = VehicleNative.VehicleCreateSimple(_vehicleWorld, (nint)_carRigidBody.DynamicActor.Handle, ref desc);
            if (_vehicle == 0)
            {
                VehicleNative.VehicleWorldDestroy(_vehicleWorld);
                _vehicleWorld = 0;
                throw new InvalidOperationException("VehicleCreateSimple failed");
            }
        }

        void DestroyVehicle()
        {
            if (_vehicle != 0)
            {
                VehicleNative.VehicleDestroy(_vehicle);
                _vehicle = 0;
            }

            if (_vehicleWorld != 0)
            {
                VehicleNative.VehicleWorldDestroy(_vehicleWorld);
                _vehicleWorld = 0;
            }
        }

        protected Pose3 GetPoseRef(Pose3 deltaRef, Pose3 lastPose)
        {
            var curPose = CarBody!.GetWorldPose();

            if (XrApp.Current != null && _carRigidBody?.IsCreated == true)
            {
                var curVel = _carRigidBody.DynamicActor.LinearVelocity;
                curPose.Position += (float)XrApp.Current.FramePredictedDisplayPeriod.TotalSeconds * curVel;
            }

            var newPose = curPose.Multiply(deltaRef);
            return lastPose.Lerp(newPose, 0.9f);
        }

        private void OnContact(Object3D self, Object3D other, int otherIndex, ContactPair[] pairs)
        {
            Log.Debug(this, "Contact {0} with {1}", self.Name, other.Name);
        }

        protected void SyncCarBody()
        {
            var bodyPose = CarBody!.GetWorldPose();

            _mainTube!.SetWorldPoseIfChanged(bodyPose.Multiply(_bodyToFrame));
            _attachedGroup.SetWorldPoseIfChanged(bodyPose.Multiply(_bodyToAttached));

            if (UseSteeringPhysics && _steeringWheelTube?.TryComponent<RigidBody>(out var rb) == true && rb.IsCreated)
                rb.DynamicActor.KinematicTarget = _steeringWheelTube.GetWorldPose();
        }

        void SyncWheels()
        {
            if (_vehicle == 0)
                return;

            var wheels = Wheels();
            for (var i = 0; i < wheels.Length; i++)
                wheels[i].SetWorldPoseIfChanged(_vehicleState.WheelPoses[i].Multiply(_wheelVisualOffsets[i]));
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
                wheelAngle = _steeringWheelJoint!.D6Joint.SwingZAngle * 0.5f;
            else
                wheelAngle = SteeringWheel!.Component<InputRotateAxis>().Angle / SteeringRatio;

            SteeringAngle = wheelAngle;
        }

        protected void SyncInput()
        {
            if (ShowHideBodyInput != null && ShowHideBodyInput.IsActive && ShowHideBodyInput.IsChanged && ShowHideBodyInput.Value)
                CarBody!.IsVisible = !CarBody.IsVisible;
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
            var gear = _vehicle != 0 ? Math.Max(1, Math.Abs(_vehicleState.Gear)) : (_curGear == "R" ? 1 : int.Parse(_curGear));
            _carSound.Engine.Gear = gear;
            _carSound.Engine.Rpm = 40 + (int)(_vehicle != 0 ? _vehicleState.MotorRpm : 0);
        }

        void UpdateVehicle(float deltaTime)
        {
            if (_vehicle == 0)
                return;

            var throttle = AccInput != null && AccInput.IsActive ? AccInput.Value : AccInputSim / 1000f;
            var reverse = BackInput != null && BackInput.IsActive && BackInput.Value;

            var input = new VehicleNative.VehicleInput
            {
                Throttle = Math.Clamp(throttle, 0, 1),
                Brake = 0,
                Steering = SteeringLimitRad > 0 ? Math.Clamp(SteeringAngle / SteeringLimitRad, -1, 1) : 0,
                HandBrake = 0,
                GearMode = VehicleNative.VehicleGearMode.Manual,
                Gear =  reverse || _curGear == "R" ? -1 : (_curGear == "N" ? 0 : int.Parse(_curGear))
            };

            VehicleNative.VehicleUpdate(_vehicle, deltaTime, ref input, ref _vehicleState);
        }

        protected override void Update(RenderContext ctx)
        {
            SyncSteering();
            SyncInput();
            SyncGear();

            _manager ??= _host.Scene!.Component<PhysicsManager>();

            _manager.Execute(() =>
            {
                EnsureVehicle();
                UpdateVehicle((float)_deltaTime);
            });

            SyncCarBody();
            SyncWheels();
            SyncSound();
            SyncCamera();
        }

        public override void Reset(bool onlySelf = false)
        {
            DestroyVehicle();
            base.Reset(onlySelf);
        }

        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
        }

        protected void UpdateDensity()
        {
            if (_carRigidBody == null)
                return;

            _carRigidBody.Density = CarBodyDensity;
            if (_carRigidBody.IsCreated)
                _carRigidBody.DynamicActor.UpdateMassAndInertia(CarBodyDensity);
        }

        protected void RecreateVehicle()
        {
            if (_vehicle == 0)
                return;

            _manager ??= _host.Scene!.Component<PhysicsManager>();
            _manager.Execute(() =>
            {
                DestroyVehicle();
                EnsureVehicle();
            });
        }

        TriangleMesh AddSteeringTube(Vector3 p1, Vector3 p2)
        {
            Debug.Assert(_chassis != null);

            var line = new Line3(p1, p2);
            var cube = new Cube3D(new Vector3(0.05f, 0.05f, line.Length()));

            var mesh = new TriangleMesh(cube, (Material)_tubeMaterial)
            {
                WorldPosition = line.Center(),
                Forward = -line.Direction(),
                Name = "ts"
            };

            mesh.AddComponent<BoxCollider>();
            mesh.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Kinematic,
                CollideGroup = RigidBodyGroup.Group1
            });

            _chassis.AddChild(mesh);

            return mesh;
        }

        Joint AddFixed(Object3D obj0, Object3D obj1, Vector3 point)
        {
            var manager = _host.Scene!.Component<PhysicsManager>();

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

        Joint AddRotation(Object3D obj0, Object3D obj1, Vector3 point, Vector3 axis)
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

        void CreateGearBox()
        {
            var lines = new List<Vector2>();

            var xSize = 0.08f * 1;
            var ySize = 0.07f * 1;
            var padSize = 0.010f;
            var lineSize = 0.008f;
            var leverHeight = 0.3f;
            var leverOffset = 0.1f;

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
        [Category("Control")]
        public float SteeringAngle
        {
            get => _steeringAngle;
            set
            {
                _steeringAngle = value;

                if (!UseSteeringPhysics && SteeringWheel != null)
                    SteeringWheel.Component<InputRotateAxis>().Angle = value * SteeringRatio;
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
                RecreateVehicle();
            }
        }

        [Range(0.1f, 100, 0.1f)]
        public float WheelMass { get; set; }

        [Range(0, 100, 0.5f)]
        [Category("Control")]
        public float AccInputSim { get; set; }

        [Category("Control")]
        public string Gear
        {
            get => _curGear;
            set => _curGear = value;
        }

        public bool UseSteeringPhysics { get; set; }

        public Pose3 SteeringLocalPose { get; set; }

        public float SteeringRatio { get; set; }

        public float SteeringLimitRad { get; set; }

        public Pose3 SeatLocalPose { get; set; }

        public XrFloatInput? AccInput { get; set; }

        public XrBoolInput? BackInput { get; set; }

        public XrBoolInput? ShowHideBodyInput { get; set; }

        [Range(0.01f, 0.3f, 0.005f)]
        public float FrameTubeSize { get; set; }

        [Range(0.01f, 0.5f, 0.005f)]
        public float SuspensionTravel { get; set; }

        [Range(0, 100000, 100)]
        public float SuspensionStiffness { get; set; }

        [Range(0, 20000, 100)]
        public float SuspensionDamping { get; set; }

        public uint PosIterations { get; set; }

        public VehicleNative.VehicleDriveType DriveType { get; set; }

        public float MaxMotorTorque { get; set; }

        public float MaxBrakeTorque { get; set; }

        public float MaxHandBrakeTorque { get; set; }

        public float IdleMotorRpm { get; set; }

        public float MaxMotorRpm { get; set; }

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
