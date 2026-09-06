
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
using XrInteraction;
using XrMath;
using IDrawGizmos = XrEngine.IDrawGizmos;

namespace XrSamples
{
    public unsafe class CarModelV2 : Behavior<Group3D>, IDrawGizmos
    {
        private readonly Group3D _attachedGroup;

        private PhysicsManager? _manager;
        private RigidBody? _carRigidBody;

        private nint _vehicleWorld;
        private nint _vehicle;
        private VehicleNative.VehicleState _vehicleState;

        private readonly Pose3[] _wheelVisualOffsets = new Pose3[4];
        private Pose3 _bodyToAttached;
        private Pose3 _seatPosDiff;

        private float _steeringAngle;
        private float _carBodyDensity;
        private float _wheelFriction;

        private bool _keyUp;
        private bool _keyDown;
        private bool _keyLeft;
        private bool _keyRight;

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
            PosIterations = 50;
            SteeringRatio = 12;
            SteeringLimitRad = 0.9f;
            WheelFriction = 0.8f;
            WheelMass = 20;

            MaxMotorTorque = 50;
            MaxBrakeTorque = 1500;
            MaxHandBrakeTorque = 2500;
            IdleMotorRpm = 800;
            MaxMotorRpm = 7000;
            DriveType = VehicleNative.VehicleDriveType.Rear;

            _attachedGroup = new Group3D
            {
                Name = "attached"
            };

            _curGear = "1";
            _carSound = new CarSound();

            UpdatePriority = 1;

            if (Context.TryRequire<IKeyboardEventSource>(out var keySource))
            {
                keySource.KeyDown += OnKeyDown;
                keySource.KeyUp += OnKeyUp;
            }
        }

        private void OnKeyDown(KeyboardEvent ev)
        {
            switch (ev.Key)
            {
                case KeyCode.Up:
                    _keyUp = true;
                    break;
                case KeyCode.Down:
                    _keyDown = true;
                    break;
                case KeyCode.Left:
                    _keyLeft = true;
                    break;
                case KeyCode.Right:
                    _keyRight = true;
                    break;
            }
        }

        private void OnKeyUp(KeyboardEvent ev)
        {
            switch (ev.Key)
            {
                case KeyCode.Up:
                    _keyUp = false;
                    break;
                case KeyCode.Down:
                    _keyDown = false;
                    break;
                case KeyCode.Left:
                    _keyLeft = false;
                    break;
                case KeyCode.Right:
                    _keyRight = false;
                    break;
            }

            var key = ev.Key.ToString();

            if (key == "R")
            {
                Gear = "R";
                return;
            }

            if (key == "N")
            {
                Gear = "N";
                return;
            }

            if (key.Length == 2 && key[0] == 'D' && key[1] >= '1' && key[1] <= '5')
            {
                Gear = key[1].ToString();
                return;
            }

            if (key.StartsWith("NumPad", StringComparison.Ordinal) &&
                key.Length == 7 &&
                key[6] >= '1' && key[6] <= '5')
            {
                Gear = key[6].ToString();
            }
        }

        protected override void OnAttach()
        {
            _host.AddChild(_attachedGroup);
            base.OnAttach();
        }

        public void Create()
        {
            AttachWheels();
            AttachSteering();
            AttachBody();
            CreateGearBox();

            CarBody!.AddComponent(_carSound);

            var bodyPose = CarBody.GetWorldPose();
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
                SimulationDisabled = false,
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

            _attachedGroup.SetWorldPoseIfChanged(bodyPose.Multiply(_bodyToAttached));
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
            SteeringAngle = SteeringWheel!.Component<InputRotateAxis>().Angle / SteeringRatio;
        }

        protected void SyncInput()
        {
            if (ShowHideBodyInput != null && ShowHideBodyInput.IsActive && ShowHideBodyInput.IsChanged && ShowHideBodyInput.Value)
                CarBody!.IsVisible = !CarBody.IsVisible;
        }

        protected void SyncKeys(float deltaTime)
        {
            AccInputSim = Math.Clamp(AccInputSim + (_keyUp ? deltaTime : -deltaTime), 0f, 1f);
            BrakeInputSim = Math.Clamp(BrakeInputSim + (_keyDown ? deltaTime : -deltaTime), 0f, 1f);

            if (_keyLeft != _keyRight)
            {
                var steerDir = _keyLeft ? -1f : 1f;
                SteeringAngle = Math.Clamp(SteeringAngle + steerDir * deltaTime, -SteeringLimitRad, SteeringLimitRad);
            }
            else
            {
                var steerStep = deltaTime;
                if (MathF.Abs(SteeringAngle) <= steerStep)
                    SteeringAngle = 0;
                else
                    SteeringAngle -= MathF.Sign(SteeringAngle) * steerStep;
            }
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

            var throttle = AccInput != null && AccInput.IsActive ? AccInput.Value : AccInputSim;
            var brake = BreakInput != null && BreakInput.IsActive ? BreakInput.Value : BrakeInputSim; ;
            var reverse = BackInput != null && BackInput.IsActive && BackInput.Value;

            var input = new VehicleNative.VehicleInput
            {
                Throttle = Math.Clamp(throttle, 0, 1),
                Brake = Math.Clamp(brake, 0, 1),
                Steering = SteeringLimitRad > 0 ? Math.Clamp(-SteeringAngle / SteeringLimitRad, -1, 1) : 0,
                HandBrake = 0,
                GearMode = VehicleNative.VehicleGearMode.Manual,
                Gear = reverse || _curGear == "R" ? -1 : (_curGear == "N" ? 0 : int.Parse(_curGear))
            };

            VehicleNative.VehicleUpdate(_vehicle, deltaTime, ref input, ref _vehicleState);
        }

        protected override void Update(RenderContext ctx)
        {
            if (ctx.Frame < 50)
                return;

            SyncSteering();
            SyncInput();
            SyncKeys((float)_deltaTime);
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

            _attachedGroup.AddChild(obj, false);
        }

        public void ConfigureInput(IXrBasicInteractionProfile input)
        {
            AccInput = input.Right!.TriggerValue;
            BreakInput = input.Left!.TriggerValue;
            BackInput = input.Right!.Button!.AClick;
            ShowHideBodyInput = input.Right!.Button!.BClick;

            var rotate = SteeringWheel!.Component<InputRotateAxis>();
            rotate.ConfigureInput(input);

            foreach (var item in _attachedGroup.Children)
            {
                if (item.TryComponent<InputRotatePivot>(out var rotatePivot))
                    rotatePivot.ConfigureInput(input);
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

                SteeringWheel?.Component<InputRotateAxis>().Angle = value * SteeringRatio;
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

        [Range(0, 1, 0.01f)]
        [Category("Control")]
        public float AccInputSim { get; set; }

        [Range(0, 1, 0.01f)]
        [Category("Control")]
        public float BrakeInputSim { get; set; }

        [Category("Control")]
        public string Gear
        {
            get => _curGear;
            set => _curGear = value;
        }

        public Pose3 SteeringLocalPose { get; set; }

        public float SteeringRatio { get; set; }

        public float SteeringLimitRad { get; set; }

        public Pose3 SeatLocalPose { get; set; }

        public XrFloatInput? AccInput { get; set; }

        public XrFloatInput? BreakInput { get; set; }

        public XrBoolInput? BackInput { get; set; }

        public XrBoolInput? ShowHideBodyInput { get; set; }

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
