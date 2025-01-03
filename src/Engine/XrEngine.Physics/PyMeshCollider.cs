﻿using PhysX.Framework;
using System.Numerics;
using XrMath;

namespace XrEngine.Physics
{
    public class PyMeshCollider : Behavior<Object3D>, ICollider3D
    {
        private PhysicsManager? _manager;
        private PhysicsSystem? _system;
        private bool _isInit;

        public PyMeshCollider()
        {
            MeshObjects = () => _host!.DescendantsOrSelf();
            Tolerance = 0.01f;
        }

        protected override void Start(RenderContext ctx)
        {
            _manager = _host!.Scene!.Components<PhysicsManager>().FirstOrDefault();
            if (_manager == null)
                throw new Exception("PhysicsManager not found in Scene");
            _system = _manager.System;
        }

        public bool ContainsPoint(Vector3 globalPoint, float tolerance = 0.001f)
        {
            if (_system == null)
                return false;

            Initialize();

            foreach (var item in MeshObjects())
            {
                var geo = item.Feature<Geometry3D>();
                if (geo == null)
                    continue;

                var pyGeo = geo.GetProp<PhysicsGeometry>("PyGeo")!;

                var distance = pyGeo.DistanceFrom(globalPoint, item.WorldMatrix.ToPose(), 0, out var _);

                if (pyGeo.Type == PhysX.PxGeometryType.Trianglemesh)
                    distance *= pyGeo.TriangleMesh.scale.scale.x;

                if (distance < tolerance)
                    return true;
            }

            return false;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            if (_system == null)
                return null;

            Initialize();

            foreach (var item in MeshObjects())
            {
                var geo = item.Feature<Geometry3D>();
                if (geo == null)
                    continue;

                var pyGeo = geo.GetProp<PhysicsGeometry>("PyGeo")!;

                var localRay = ray.Transform(item.WorldMatrixInverse);

                if (!geo.Bounds.Intersects(localRay.ToLine(1000f), out _))
                    continue;

                var result = pyGeo.Raycast(ray, item.WorldMatrix.ToPose(), 1000, PhysX.PxHitFlags.Normal, 2);

                if (result.Length == 0)
                    continue;

                var min = result.Min(a => a.Distance);

                var minRes = result.FirstOrDefault(a => a.Distance == min);

                return new Collision
                {
                    Distance = min,
                    Normal = minRes.Normal.ToDirection(item.WorldMatrixInverse),
                    Point = minRes.Position,
                    LocalPoint = item.ToLocal(minRes.Position),
                    UV = minRes.UV,
                    Object = item
                };
            }
            return null;
        }

        protected override void Update(RenderContext ctx)
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInit)
                return;
            foreach (var item in MeshObjects())
            {
                var geo = item.Feature<Geometry3D>();
                if (geo == null)
                    continue;

                geo.GetOrCreateProp("PyGeo", () =>
                {
                    geo.EnsureIndices();

                    Matrix4x4.Decompose(item.WorldMatrix, out var scale, out _, out _);

                    if (UseConvexMesh)
                    {
                        return _system!.CreateConvexMesh(
                          geo.Indices,
                          geo.ExtractPositions(),
                          scale);
                    }

                    return _system!.CreateTriangleMesh(
                        geo.Indices,
                        geo.ExtractPositions(),
                        scale,
                        Tolerance);
                });
            }

            _isInit = true;
        }

        public override void GetState(IStateContainer container)
        {
            container.Write(nameof(UseConvexMesh), UseConvexMesh);
            container.Write(nameof(Tolerance), Tolerance);

            base.GetState(container);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Tolerance = container.Read<float>(nameof(Tolerance));
            UseConvexMesh = container.Read<bool>(nameof(UseConvexMesh));
        }


        public Func<IEnumerable<Object3D>> MeshObjects { get; set; }

        public bool UseConvexMesh { get; set; }

        public float Tolerance { get; set; }

    }
}
