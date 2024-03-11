using PhysX;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
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
        private PhysicsGeometryType _geoType;
        private readonly List<PhysicsShape> _shapes = [];
        private event RigidBodyContactEventHandler? _contactEvent;

        public RigidBody()
        {
            BodyType = PhysicsActorType.Dynamic;

            Material = new PhysicsMaterialInfo
            {
                DynamicFriction = 0.8f,
                Restitution = 0.4f,
                StaticFriction = 0.8f
            };

            Density = 10;
        }

        protected void SetPose(Pose3 pose)
        {
            Debug.Assert(_host != null);

            var newTrans = _host.Transform.Clone();

            newTrans.Position = pose.Position;
            newTrans.Orientation = pose.Orientation;

            Matrix4x4 matrix;

            var bounds = _host?.Feature<ILocalBounds>();

            if (_geoType == PhysicsGeometryType.Box)
                matrix = Matrix4x4.CreateTranslation(-bounds!.LocalBounds.Center) * newTrans.Matrix;
            else if (_geoType == PhysicsGeometryType.Capsule)
                matrix = Matrix4x4.CreateRotationY(-MathF.PI / 2) * newTrans.Matrix;
            else
                matrix = newTrans.Matrix;

            _host!.Transform.SetMatrix(matrix * _host.Parent!.WorldMatrixInverse);
        }

        protected Pose3 GetPose()
        {
            Debug.Assert(_host != null);

            Matrix4x4 matrix;

            var bounds = _host.Feature<ILocalBounds>();

            if (_geoType == PhysicsGeometryType.Box)
                matrix = Matrix4x4.CreateTranslation(bounds!.LocalBounds.Center) * _host!.WorldMatrix;
            else if (_geoType == PhysicsGeometryType.Capsule)
                matrix = Matrix4x4.CreateRotationY(MathF.PI / 2) * _host!.WorldMatrix;
            else
                matrix = _host!.WorldMatrix;

            Matrix4x4.Decompose(matrix, out var _, out var orientation, out var translation);

            return new Pose3
            {
                Position = translation,
                Orientation = orientation
            };
        }

        protected override void OnEnabled()
        {
            if (_actor.IsValid)
                _actor.Actor.SetActorFlagMut(PxActorFlag.DisableSimulation, false);
        }

        protected override void OnDisabled()
        {
            if (_actor.IsValid)
                _actor.Actor.SetActorFlagMut(PxActorFlag.DisableSimulation, true);
        }

        protected override void Start(RenderContext ctx)
        {
            Debug.Assert(_host != null);

            var manager = _host.Scene!.Components<PhysicsManager>().FirstOrDefault();

            _system = manager?.System;

            if (_system == null)
                return;

            Matrix4x4.Decompose(_host.WorldMatrix, out var scale, out var _, out var _);

            _material = _system.CreateMaterial(Material);

            foreach (var collider in _host!.Components<ICollider3D>())
            {
                if (!collider.IsEnabled)
                    continue;

                PhysicsGeometry? pyGeo = null;

                if (collider is MeshCollider mc)
                {
                    mc.Geometry.EnsureIndices();

                    pyGeo = _system.CreateTriangleMesh(
                        mc.Geometry.Indices,
                        mc.Geometry.ExtractPositions(),
                       scale);
                }
                else if (collider is CapsuleCollider cap)
                {
                    pyGeo = _system.CreateCapsule(cap.Radius * scale.X, cap.Height * scale.X * 0.5f);
                }
                else if (collider is SphereCollider sphere)
                {
                    pyGeo = _system.CreateSphere(sphere.Radius * scale.X);
                }

                if (pyGeo == null)
                    return;

                _geoType = pyGeo.Type;

                var shape = _system.CreateShape(new PhysicsShapeInfo
                {
                    Geometry = pyGeo,
                    Material = _material,
                });
                shape.Tag = collider;

                if (_host.Name != null)
                    shape.Name = _host.Name;

                _shapes.Add(shape);
            }

            if (_shapes.Count == 0)
            {
                var bounds = _host.Feature<ILocalBounds>();

                if (bounds == null)
                    throw new NotSupportedException();

                var pyGeo = _system.CreateBox(bounds.LocalBounds.Size / 2 * scale);

                var shape = _system.CreateShape(new PhysicsShapeInfo
                {
                    Geometry = pyGeo,
                    Material = _material,
                });

                shape.Tag = bounds;

                _geoType = PhysicsGeometryType.Box;

                _shapes.Add(shape);
            }

            _actor = _system.CreateActor(new PhysicsActorInfo
            {
                Density = Density,
                Shapes = _shapes,
                Pose = GetPose(),
                Type = BodyType
            });

            _actor.Tag = _host;
            _actor.NotifyContacts = _contactEvent != null;
            _actor.Contact += OnActorContact;
        }

        private void OnActorContact(PhysicsActor other, int otherIndex, ContactPair[] data)
        {
            _contactEvent?.Invoke(_host!, (Object3D)other.Tag!, otherIndex, data);
        }

        protected override void Update(RenderContext ctx)
        {
            if (!_actor.IsValid)
                return;

            var isGrabbing = _host?.GetProp<bool>("IsGrabbing") == true;

            if (!isGrabbing)
            {
                if (BodyType == PhysicsActorType.Dynamic)
                {
                    SetPose(_actor.GlobalPose);
                    if (_actor.IsKinematic)
                        _actor.IsKinematic = false;
                }
            }
            else
            {
                if (BodyType == PhysicsActorType.Dynamic || BodyType == PhysicsActorType.Kinematic)
                {
                    if (BodyType == PhysicsActorType.Dynamic)
                        _actor.IsKinematic = true;
                    _actor.KinematicTarget = GetPose();
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
                    _system.Scene.RemoveActorMut(_actor, false);
                    _actor.Dispose();
                    _actor = null;
                }

                foreach (var shape in _shapes)
                    shape.Dispose();

                _shapes.Clear();

                _material?.Dispose();
                _material = null;
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

        public PhysicsActor Actor => _actor ?? throw new ArgumentNullException();

        public PhysicsActorType BodyType { get; set; }

        public PhysicsMaterialInfo Material { get; set; }

        public float Density { get; set; }

    }
}
