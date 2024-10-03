using PhysX;
using PhysX.Framework;
using System.Diagnostics;
using System.Numerics;
using XrMath;


namespace XrEngine.Physics
{
    public delegate void RigidBodyContactEventHandler(Object3D self, Object3D other, int otherIndex, ContactPair[] pairs);

    public class RigidBody : Behavior<Object3D>, IDisposable
    {
        private PhysicsManager? _manager;
        private PhysicsSystem? _system;
        private PhysicsActor? _actor;
        private PhysicsMaterial? _material;
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
        }

        public void Teleport(Vector3 worldPos)
        {
            _host!.WorldPosition = worldPos;
            DynamicActor.GlobalPose = GetPose();
        }

        protected void SetPose(Pose3 pose)
        {
            Debug.Assert(_host != null);

            _host.SetGlobalPoseIfChanged(pose);
        }

        protected Pose3 GetPose()
        {
            Debug.Assert(_host != null);

            Matrix4x4.Decompose(_host.WorldMatrix, out var _, out var orientation, out var translation);

            return new Pose3
            {
                Position = translation,
                Orientation = orientation
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

        protected PhysicsGeometry? CreateGeometry(ICollider3D? collider, ref Pose3 pose)
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

                result = _system.CreateBox(local.LocalBounds.Size / 2);

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

                    result = _system.CreateTriangleMesh(
                        mc.Geometry.Indices,
                        mc.Geometry.ExtractPositions(),
                        Vector3.One, LengthToleranceScale
                    );
                    /*
                    pyGeo = _system.CreateConvexMesh(
                          mc.Geometry.Indices,
                          mc.Geometry.ExtractPositions(),
                          Vector3.One
                      );
                    */
                }
                else if (collider is CapsuleCollider cap)
                {
                    result = _system.CreateCapsule(cap.Radius, cap.Height * 0.5f);
                    pose.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
                }
                else if (collider is SphereCollider sphere)
                {
                    result = _system.CreateSphere(sphere.Radius);
                }
                else if (collider is BoxCollider box)
                {
                    result = _system.CreateBox(box.Size / 2);
                    pose.Position = box.Center;
                }
            }

            return result;
        }

        protected PhysicsShape? CreateShape(ICollider3D? collider)
        {
            Debug.Assert(_system != null);
            Debug.Assert(_host != null);

            PhysicsShape? shape = null; ;
            Pose3 pose = Pose3.Identity;

            PhysicsGeometry? pyGeo = CreateGeometry(collider, ref pose);

            if (pyGeo != null)
            {
                shape = _system.CreateShape(new PhysicsShapeInfo
                {
                    Geometry = pyGeo,
                    Material = _material,
                    IsEsclusive = true,
                });

                shape.ContactOffset = ContactOffset;
            }

            if (shape != null)
            {
                shape.Tag = collider;
                shape.LocalPose = pose;
            }

            return shape;
        }

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

            _manager = _host.Scene.Components<PhysicsManager>().FirstOrDefault();

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

                var shape = CreateShape(collider);
                if (shape != null)
                    shapes.Add(shape);
            }

            if (shapes.Count == 0)
            {
                var boxShape = CreateShape(null);
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

            if (scale.X != 1)
                _actor.SetScale(scale.X);

            _actor.Tag = _host;
            _actor.NotifyContacts = _contactEvent != null;
            _actor.Contact += OnActorContact;
            _actor.Name = _host.Name ?? string.Empty;

            if (Type != PhysicsActorType.Static)
                DynamicActor.ContactReportThreshold = ContactReportThreshold;

            if (EnableCCD)
            {
                if (Type == PhysicsActorType.Dynamic)
                    DynamicActor.RigidBodyFlags |= PxRigidBodyFlags.EnableCcd;

                if (Type == PhysicsActorType.Kinematic)
                    DynamicActor.RigidBodyFlags |= PxRigidBodyFlags.EnableSpeculativeCcd;
            }

            Configure?.Invoke(this);
        }

        protected void UpdatePhysics()
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
            }


            if (Type != PhysicsActorType.Static)
            {
                DynamicActor.ContactReportThreshold = ContactReportThreshold;
                DynamicActor.UpdateMassAndInertia(Density, Vector3.Zero);
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

        protected override void Start(RenderContext ctx)
        {
            Destroy();
            Create(ctx);
        }

        private void OnActorContact(PhysicsActor other, int otherIndex, ContactPair[] data)
        {
            _contactEvent?.Invoke(_host!, (Object3D)other.Tag!, otherIndex, data);
        }

        protected override void Update(RenderContext ctx)
        {
            if (_actor == null)
                return;

            Debug.Assert(_host != null);

            _manager!.Execute(() =>
            {
                if (!_host.IsManipulating())
                {
                    if (Type == PhysicsActorType.Dynamic)
                    {
                        SetPose(_actor.GlobalPose);
                        if (DynamicActor.IsKinematic)
                            DynamicActor.IsKinematic = false;
                    }
                    else if (Type == PhysicsActorType.Static)
                        _actor.GlobalPose = GetPose();
                }
                else
                {
                    if (Type != PhysicsActorType.Static)
                    {
                        if (Type == PhysicsActorType.Dynamic && !DynamicActor.IsKinematic)
                            DynamicActor.IsKinematic = true;

                        DynamicActor.KinematicTarget = GetPose();
                    }
                    else
                        _actor.GlobalPose = GetPose();
                }
            });

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

        public Action<RigidBody>? Configure { get; set; }

        public float ContactReportThreshold { get; set; }

        public float LengthToleranceScale { get; set; }

        public float ContactOffset { get; set; }

        public float Density { get; set; }

        public bool EnableCCD { get; set; }

        public PhysicsActorType Type { get; set; }

        public PhysicsMaterialInfo Material { get; set; }

        public PhysicsDynamicActor DynamicActor => (_actor as PhysicsDynamicActor) ?? throw new ArgumentNullException();

        public PhysicsStaticActor StaticActor => (_actor as PhysicsStaticActor) ?? throw new ArgumentNullException();

        public PhysicsActor Actor => _actor ?? throw new ArgumentNullException();

        public bool IsCreated => _actor != null;
    }
}
