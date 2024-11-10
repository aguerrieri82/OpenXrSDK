using PhysX;
using PhysX.Framework;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using XrMath;


namespace XrEngine.Physics
{
    public delegate void RigidBodyContactEventHandler(Object3D self, Object3D other, int otherIndex, ContactPair[] pairs);

    [Flags]
    public enum RigidBodyGroup
    {
        None = 0,
        Group1 = 0x1,
        Group2 = 0x2,
        Group3 = 0x4,
        Group4 = 0x8,
    }

    public class RigidBody : Behavior<Object3D>, IDisposable
    {
        private PhysicsManager? _manager;
        private PhysicsSystem? _system;
        private PhysicsRigidActor? _actor;
        private PhysicsMaterial? _material;
        private Pose3 _lastPose;

        private event RigidBodyContactEventHandler? _contactEvent;


        public RigidBody()
        {
            Type = PhysicsActorType.Dynamic;

            Material = new PhysicsMaterialInfo
            {
                DynamicFriction = 1f,
                StaticFriction = 1f,
                Restitution = 0.5f
            };

            Density = 100;
            LengthToleranceScale = 1;
            ContactOffset = 0.01f;
            ContactReportThreshold = 1f;
            EnableCCD = false;
            AutoTeleport = false;
        }

        public void Teleport(Vector3 worldPos)
        {
            _host!.WorldPosition = worldPos;
            _lastPose = GetPose();
            DynamicActor.GlobalPose = _lastPose;
            DynamicActor.Stop();
        }

        protected void SetPose(Pose3 pose)
        {
            Debug.Assert(_host != null);

            if (!pose.IsFinite())
                throw new InvalidOperationException();

            _host.SetWorldPoseIfChanged(pose);
        }

        protected Pose3 GetPose()
        {
            Debug.Assert(_host != null);

            return new Pose3
            {
                Position = _host.WorldPosition,
                Orientation = _host.WorldOrientation
            };
        }

        protected override void OnEnabled()
        {
            _actor?.Actor.SetActorFlagMut(PxActorFlag.DisableSimulation, false);
        }

        protected override void OnDisabled()
        {
            _actor?.Actor.SetActorFlagMut(PxActorFlag.DisableSimulation, true);
        }

        protected PhysicsGeometry? CreateGeometry(ICollider3D? collider, ref Pose3 pose, Vector3 scale)
        {
            Debug.Assert(_system != null);
            Debug.Assert(_host != null);

            PhysicsGeometry? result = null;

            if (collider == null)
            {
                var local = _host.Feature<ILocalBounds>();

                if (local == null)
                    return null;

                local.BoundUpdateMode = UpdateMode.Automatic;

                result = _system.CreateBox(local.LocalBounds.Size / 2 * scale);

                pose.Position = local.LocalBounds.Center;
            }
            else
            {
                if (!collider.IsEnabled)
                    return null;

                collider.Initialize();

                var host = (collider.Host as Object3D)!;

                var geo = host.Feature<Geometry3D>();

                if (collider is MeshCollider mc && mc.Geometry != null)
                {
                    mc.Geometry.EnsureIndices();

                    if (mc.UseConvexHull)
                    {
                        result = _system.CreateConvexMesh(
                              mc.Geometry.Indices,
                              mc.Geometry.ExtractPositions(),
                              scale);
                    }
                    else
                    {
                        result = _system.CreateTriangleMesh(
                            mc.Geometry.Indices,
                            mc.Geometry.ExtractPositions(),
                            scale, LengthToleranceScale);
                    }
                }
                else if (collider is PyMeshCollider py)
                {
                    if (collider.Host is not TriangleMesh mesh)
                        mesh = py.MeshObjects().OfType<TriangleMesh>().FirstOrDefault()!;

                    var geo3d = mesh?.Geometry;

                    if (geo3d != null)
                        result = geo3d.GetProp<PhysicsGeometry>("PyGeo");

                    if (mesh != null)
                        pose = (mesh.WorldMatrix * _host.WorldMatrixInverse).ToPose();
                }
                else if (collider is CapsuleCollider cap)
                {
                    result = _system.CreateCapsule(cap.Radius * scale.X, cap.Height * 0.5f * scale.Y);
                    pose.Position = cap.Pose.Position;
                    pose.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2) * cap.Pose.Orientation;
                }
                else if (collider is SphereCollider sphere)
                {
                    result = _system.CreateSphere(sphere.Radius * scale.X);
                    pose.Position = sphere.Center;
                }
                else if (collider is CylinderCollider cyl)
                {
                    var points = new List<Vector3>();

                    var halfHeight = cyl.Height / 2;

                    var numSegments = (cyl.Radius * 2 * MathF.PI) / 0.01f; //1cm

                    for (int i = 0; i < numSegments; i++)
                    {
                        var angle = (2f * MathF.PI * i / numSegments);
                        var x = cyl.Radius * MathF.Cos(angle);
                        var z = cyl.Radius * MathF.Sin(angle);

                        points.Add(new Vector3(x, halfHeight, z));
                        points.Add(new Vector3(x, -halfHeight, z));
                    }

                    result = _system.CreateConvexMesh(null, points.ToArray(), scale);

                    pose = cyl.Pose;
                }
                else if (collider is BoxCollider box)
                {
                    result = _system.CreateBox(box.Size / 2 * scale);
                    pose.Position = box.Center;
                }
            }

            return result;
        }

        protected PhysicsShape? CreateShape(ICollider3D? collider, Vector3 scale)
        {
            Debug.Assert(_system != null);
            Debug.Assert(_host != null);

            PhysicsShape? shape = null;
            Pose3 pose = Pose3.Identity;

            PhysicsGeometry? pyGeo = CreateGeometry(collider, ref pose, scale);

            if (pyGeo != null)
            {
                shape = _system.CreateShape(new PhysicsShapeInfo
                {
                    Geometry = pyGeo,
                    Material = _material,
                    IsEsclusive = true,
                });

                shape.ContactOffset = ContactOffset;
                shape.NotCollideGroup = (uint)NotCollideGroup;
            }

            if (shape != null)
            {
                shape.Tag = collider;
                shape.LocalPose = pose;
            }

            return shape;
        }

        /*
        public void UpdateShape()
        {
            Debug.Assert(_host != null);

            if (_actor == null)
                return;

            foreach (var collider in _host.Components<ICollider3D>())
            {
                if (!collider.IsEnabled)
                    continue;

                Pose3 pose = Pose3.Identity;

                var shape = _actor!.GetShapes().FirstOrDefault(s => s.Tag == collider);
                if (shape != null)
                {
                    var geo = CreateGeometry(collider, ref pose);
                    if (geo != null)
                    {
                        shape.Geometry = geo;
                        shape.LocalPose = pose;
                    }
                }
            }
        }
        */

        protected void Destroy()
        {
            if (_system == null)
                return;

            if (_actor != null)
            {
                _actor.Dispose();
                _actor = null;
            }

            if (_material != null)
            {
                _material.Release();
                _material = null;
            }
        }

        protected void Create(RenderContext ctx)
        {
            Debug.Assert(_host?.Scene != null);

            _host.Scene.TryComponent(out _manager);

            _system = _manager?.System;

            if (_system == null)
                throw new NotSupportedException("Add PhysicsManager to the scene");

            Matrix4x4.Decompose(_host.WorldMatrix, out var scale, out var _, out var _);

            if (!scale.IsSameValue(10e-5f))
                throw new NotSupportedException("Not uniform scale is not supported");

            _material = _system.CreateOrGetMaterial(new PhysicsMaterialInfo
            {
                DynamicFriction = Material.DynamicFriction,
                StaticFriction = Material.StaticFriction,
                Restitution = Material.Restitution,
            });

            var shapes = new List<PhysicsShape>();

            foreach (var collider in _host.Components<ICollider3D>())
            {
                if (!collider.IsEnabled)
                    continue;

                if (collider is IRenderUpdate update)
                    update.Update(ctx);

                var shape = CreateShape(collider, scale);
                if (shape != null)
                    shapes.Add(shape);
            }

            if (shapes.Count == 0)
            {
                var boxShape = CreateShape(null, scale);
                if (boxShape == null)
                    throw new NotSupportedException("Object has no collider or local bounds");
                shapes.Add(boxShape);
            }

            _actor = _system.CreateActor(new PhysicsActorInfo
            {
                Density = Density,
                Shapes = shapes,
                Pose = GetPose(),
                Type = Type
            });

            /*
            //TODO: scale is set at shape creation
            if (scale.X != 1)
                _actor.SetScale(scale.X); 
            */

            _actor.Tag = _host;
            _actor.NotifyContacts = _contactEvent != null;
            _actor.Contact += OnActorContact;
            _actor.Name = _host.Name ?? string.Empty;

            UpdatePhysics();

            Configure?.Invoke(this);
        }

        public void UpdatePhysics()
        {
            if (_actor == null || _system == null || _host == null)
                return;

            Matrix4x4.Decompose(_host.WorldMatrix, out var scale, out var _, out var _);

            _actor.SetScale(scale.X);

            _actor.Name = _host.Name ?? string.Empty;

            _material?.Release();

            _material = _system.CreateOrGetMaterial(new PhysicsMaterialInfo
            {
                DynamicFriction = Material.DynamicFriction,
                StaticFriction = Material.StaticFriction,
                Restitution = Material.Restitution,
            });

            foreach (var shape in _actor.GetShapes())
            {
                shape.SetMaterials([_material]);
                shape.ContactOffset = ContactOffset;
                shape.NotCollideGroup = (uint)NotCollideGroup;
            }

            if (Type != PhysicsActorType.Static)
            {
                DynamicActor.ContactReportThreshold = ContactReportThreshold;
                //TODO for some reason UpdateMassAndInertia fails
                //var res = DynamicActor.UpdateMassAndInertia(Density);
                DynamicActor.AngularDamping = AngularDamping;
                DynamicActor.LockFlags = Lock;
            }

            if (Type == PhysicsActorType.Dynamic)
            {
                if (EnableCCD)
                    DynamicActor.RigidBodyFlags |= PxRigidBodyFlags.EnableCcd;
                else
                    DynamicActor.RigidBodyFlags &= ~PxRigidBodyFlags.EnableCcd;
            }

            else if (Type == PhysicsActorType.Kinematic)
            {
                if (EnableCCD)
                    DynamicActor.RigidBodyFlags |= PxRigidBodyFlags.EnableSpeculativeCcd;
                else
                    DynamicActor.RigidBodyFlags &= ~PxRigidBodyFlags.EnableSpeculativeCcd;
            }
        }

        public override void Reset(bool onlySelf = false)
        {
            Destroy();
        }

        protected override void Start(RenderContext ctx)
        {
            EnsureCreated(ctx);
            _lastPose = GetPose();
        }

        private void OnActorContact(PhysicsActor other, int otherIndex, ContactPair[] data)
        {
            _contactEvent?.Invoke(_host!, (Object3D)other.Tag!, otherIndex, data);
        }

        protected override void Update(RenderContext ctx)
        {
            if (_actor != null)
                _manager!.Execute(UpdateWork);
        }

        void UpdateWork()
        {
            Debug.Assert(_host != null);
            Debug.Assert(_actor != null);

            if (_host.GetActiveTool() == null)
            {
                var curPose = GetPose();

                if (Type == PhysicsActorType.Dynamic)
                {
                    if (AutoTeleport && !curPose.IsSimilar(_lastPose, 1e-4f))
                    {
                        _lastPose = curPose;
                        DynamicActor.GlobalPose = _lastPose;
                        DynamicActor.Stop();
                        Log.Checkpoint($"Pose reset: {_host!.Name}", "#ff0000");
                    }
                    else
                    {
                        if (DynamicActor.IsKinematic)
                            DynamicActor.IsKinematic = false;
                        SetPose(_actor.GlobalPose);
                        _lastPose = GetPose();
                    }
                }
                else if (Type == PhysicsActorType.Static)
                {
                    if (!Equals(_actor.GlobalPose, curPose))
                        _actor.GlobalPose = curPose;
                }
            }
            else
            {
                if (Type != PhysicsActorType.Static)
                {
                    if (Type == PhysicsActorType.Dynamic && !DynamicActor.IsKinematic)
                        DynamicActor.IsKinematic = true;

                    _lastPose = GetPose();
                    DynamicActor.KinematicTarget = _lastPose;
                }
                else
                    _actor.GlobalPose = GetPose();
            }
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Type), Type);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Type = container.Read<PhysicsActorType>(nameof(Type));
        }

        public unsafe void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        internal void EnsureCreated(RenderContext ctx)
        {
            if (_actor == null)
                Create(ctx);
        }

        public event RigidBodyContactEventHandler Contact
        {
            add
            {
                _contactEvent += value;
                if (_actor != null)
                    _actor.NotifyContacts = true;
            }
            remove
            {
                _contactEvent -= value;
            }
        }


        [Category("Advanced")]
        public float ContactReportThreshold { get; set; }

        [Category("Advanced")]
        public float LengthToleranceScale { get; set; }

        [Category("Advanced")]
        public float ContactOffset { get; set; }

        [Category("Advanced")]
        public bool EnableCCD { get; set; }

        [Category("Advanced")]
        public float AngularDamping { get; set; }

        public PxRigidDynamicLockFlags Lock { get; set; }

        public float Density { get; set; }

        public PhysicsActorType Type { get; set; }

        public PhysicsMaterialInfo Material { get; set; }

        public bool AutoTeleport { get; set; }

        public RigidBodyGroup NotCollideGroup { get; set; }

        public Action<RigidBody>? Configure { get; set; }

        public PhysicsRigidDynamic DynamicActor => (_actor as PhysicsRigidDynamic) ?? throw new ArgumentNullException();

        public PhysicsRigidStatic StaticActor => (_actor as PhysicsRigidStatic) ?? throw new ArgumentNullException();

        public PhysicsRigidActor Actor => _actor ?? throw new ArgumentNullException();

        public bool IsCreated => _actor != null;
    }
}
