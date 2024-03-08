using MagicPhysX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Xr.Engine.Colliders;


namespace Xr.Engine.Physics
{
    public class RigidBody : Behavior<Object3D>, IDisposable
    {
        private PhysicsSystem? _system;
        private PhysicsActor _actor;
        private Geometry3D? _geometry;
        private PhysicsMaterial _material;
        private bool _boundsTranslate;
        private PhysicsGeometryType _geoType;
        private List<PhysicsShape> _shapes = [];

        public RigidBody()
        {
            BodyType = PhysicsActorType.Dynamic;

            Material = new PhysicsMaterialInfo 
            {
                DynamicFriction = 0.8f,
                Restitution= 0.4f,
                StaticFriction = 0.8f
            };

            Density = 10;
        }

        protected void SetPose(PxTransform pose)
        {
            Debug.Assert(_host != null);

            var newTrans = _host.Transform.Clone();

            newTrans.Position = pose.p;
            newTrans.Orientation = pose.q;

            Matrix4x4 matrix;

            if (_geoType == PhysicsGeometryType.Box)
                matrix = Matrix4x4.CreateTranslation(-_geometry!.Bounds.Center) * newTrans.Matrix;
            else if (_geoType == PhysicsGeometryType.Capsule)
                matrix = Matrix4x4.CreateRotationY(-MathF.PI / 2) * newTrans.Matrix;
            else
                matrix = newTrans.Matrix;

            _host.Transform.SetMatrix(matrix * _host.Parent!.WorldMatrixInverse);
        }

        protected PxTransform GetPose()
        {
            //Debug.Assert(_geometry != null);

            Matrix4x4 matrix;

            if (_geoType == PhysicsGeometryType.Box)
                matrix = Matrix4x4.CreateTranslation(_geometry.Bounds.Center) * _host!.WorldMatrix;
            else if (_geoType == PhysicsGeometryType.Capsule)
                matrix = Matrix4x4.CreateRotationY(MathF.PI / 2) * _host!.WorldMatrix;
            else
                matrix = _host!.WorldMatrix;

            Matrix4x4.Decompose(matrix, out var _, out var rotation, out var translation);

            return new PxTransform
            {
                p = translation,
                q = rotation
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

            _geometry = _host.Feature<Geometry3D>();

            _material = _system.CreateMaterial(Material);

            foreach (var collider in _host!.Components<ICollider3D>())
            {
                if (!collider.IsEnabled)
                    continue;

                PhysicsGeometry? pyGeo = null;

                if (collider is MeshCollider mc)
                {
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
                else if (_host is ILocalBounds bounds)
                {
                    _boundsTranslate = true;
                    pyGeo = _system.CreateBox(bounds.LocalBounds.Size / 2 * scale);
                }

                if (pyGeo == null)
                    return;

                _geoType = pyGeo.Value.Type;


                var shape = _system.CreateShape(new PhysicsShapeInfo
                {
                    Geometry = pyGeo.Value,
                    Material = _material,
                });

                if (_host.Name != null)
                    shape.Name = _host.Name;

                _shapes.Add(shape);
            }



            _actor = _system.CreateActor(new PhysicsActorInfo
            {
                Density = Density,
                Shapes = _shapes,
                Transform = GetPose(),
                Type = BodyType
            });
        }

        protected override void Update(RenderContext ctx)
        {
            if (!_actor.IsValid)
                return;

            var isGrabbing = _host?.GetProp<bool>("IsGrabbing") == true;

            if (!isGrabbing)
            {
                if ( BodyType == PhysicsActorType.Dynamic)
                {
                    SetPose(_actor.GlobalPose);
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
                if (_actor.IsValid)
                {
                    _system.Scene.RemoveActorMut(_actor, false);
                    _actor.Release();
                }

                foreach (var shape in _shapes)
                    shape.Release();
            }
            GC.SuppressFinalize(this);  
        }

        public ref PhysicsActor Actor => ref _actor;

        public PhysicsActorType BodyType { get; set; }

        public PhysicsMaterialInfo Material { get; set; }

        public float Density { get; set; }

    }
}
