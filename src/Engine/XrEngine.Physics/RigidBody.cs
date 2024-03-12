using PhysX;
using PhysX.Framework;
using System.Diagnostics;
using System.Numerics;
using XrEngine.Colliders;
using XrMath;


namespace XrEngine.Physics
{
    public delegate void RigidBodyContactEventHandler(Object3D self, Object3D other, int otherIndex, ContactPair[] pairs);

    public class RigidBody : Behavior<Object3D>, IDisposable
    {
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

            Density = 10;

            Tolerance = 10;
        }

        public void Teleport(Vector3 worldPos)
        {
            _host!.WorldPosition = worldPos;
            DynamicActor.GlobalPose = GetPose();
        }

        protected void SetPose(Pose3 pose)
        {
            Debug.Assert(_host != null);

            var newTrans = _host.Transform.Clone();

            newTrans.Position = pose.Position;
            newTrans.Orientation = pose.Orientation;

            _host.Transform.SetMatrix(newTrans.Matrix * _host.Parent!.WorldMatrixInverse);
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

        protected PhysicsShape? CreateShape(ICollider3D? collider)
        {
            Debug.Assert(_system != null);
            Debug.Assert(_host != null);

            PhysicsShape? shape = null;
            PhysicsGeometry? pyGeo = null;
            EngineObject? shapeHost = null;
            Pose3 pose = Pose3.Identity;

            if (collider == null)
            {
                var local = _host.Feature<ILocalBounds>();

                if (local == null)
                    return null;

                pyGeo = _system.CreateBox(local.LocalBounds.Size / 2);

                pose.Position = local.LocalBounds.Center;

                shapeHost = _host;
            }
            else
            {
                if (!collider.IsEnabled)
                    return null;

                var host = (collider.Host as Object3D)!;

                var geo = host.Feature<Geometry3D>();

                if (geo != null)
                    shape = geo.GetProp<PhysicsShape>("PhysicsShape");

                if (shape == null)
                {
                    if (collider is MeshCollider mc && mc.Geometry != null)
                    {
                        mc.Geometry.EnsureIndices();

                        pyGeo = _system.CreateTriangleMesh(
                            mc.Geometry.Indices,
                            mc.Geometry.ExtractPositions(),
                            Vector3.One, Tolerance
                        );
                    }
                    else if (collider is CapsuleCollider cap)
                    {
                        pyGeo = _system.CreateCapsule(cap.Radius, cap.Height * 0.5f);
                        pose.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
                    }
                    else if (collider is SphereCollider sphere)
                    {
                        pyGeo = _system.CreateSphere(sphere.Radius);
                    }
                    else if (collider is BoxCollider box)
                    {
                        pyGeo = _system.CreateBox(box.Size / 2);
                    }

                    shapeHost = geo;
                }
            }

            if (pyGeo != null)
            {
                shape = _system.CreateShape(new PhysicsShapeInfo
                {
                    Geometry = pyGeo,
                    Material = _material,
                });
            }

            if (shape != null)
            {
                shapeHost?.SetProp("PhysicsShape", shape);

                shape.Tag = collider;

                shape.LocalPose = pose;
            }

            return shape;
        }

        protected void Create()
        {
            Debug.Assert(_host?.Scene != null);

            var manager = _host.Scene.Components<PhysicsManager>().FirstOrDefault();

            _system = manager?.System;

            if (_system == null)
                throw new NotSupportedException("Add PhysicsManager to the scene");

            Matrix4x4.Decompose(_host.WorldMatrix, out var scale, out var _, out var _);

            if (!scale.IsSameValue(10e-5f))
                throw new NotSupportedException("Not uniform scale is not supported");

            _material = _system.CreateOrGetMaterial(new PhysicsMaterialInfo
            {
                DynamicFriction = 1f,
                StaticFriction = 1f,
                Restitution = 0.5f
            });

            var shapes = new List<PhysicsShape>();

            foreach (var collider in _host.Components<ICollider3D>())
            {
                if (!collider.IsEnabled)
                    continue;

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

            _actor.SetScale(scale.X);

            _actor.Tag = _host;
            _actor.NotifyContacts = _contactEvent != null;
            _actor.Contact += OnActorContact;
            _actor.Name = _host.Name ?? string.Empty;
        }

        protected override void Start(RenderContext ctx)
        {
            Create();
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

            if (!_host.IsManipulating())
            {
                if (Type == PhysicsActorType.Dynamic)
                {
                    SetPose(_actor.GlobalPose);
                    if (DynamicActor.IsKinematic)
                        DynamicActor.IsKinematic = false;
                }
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
        }

        public unsafe void Dispose()
        {
            if (_system != null)
            {
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

        public float Tolerance { get; set; }

        public PhysicsDynamicActor DynamicActor => (_actor as PhysicsDynamicActor) ?? throw new ArgumentNullException();

        public PhysicsStaticActor StaticActor => (_actor as PhysicsStaticActor) ?? throw new ArgumentNullException();

        public PhysicsActorType Type { get; set; }

        public PhysicsMaterialInfo Material { get; set; }

        public float Density { get; set; }

    }
}
