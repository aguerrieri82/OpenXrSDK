﻿using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OpenXr.Engine
{
    public class MeshCollider : Behavior<Mesh>, ICollider
    {
        long _version;
        Triangle3[]? _triangles;

        void Update()
        {
            _triangles = _host!.Geometry!.Triangles().ToArray();
            _version = _host!.Geometry!.Version;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            if (_version != _host!.Geometry!.Version)
                Update();

            var tRay = ray.Transform(_host!.WorldMatrixInverse);

            var span = _triangles.AsSpan();

            for (var i = 0; i < span.Length; i++)
            {
                var point = span[i].RayIntersect(ref tRay, out var _);
                if (point != null)
                {
                    var worldPoint = point.Value.Transform(_host.WorldMatrix);
                    return new Collision
                    {
                        Distance = Vector3.Distance(worldPoint, ray.Origin),
                        Object = _host,
                        LocalPoint = point.Value,
                        Point = worldPoint,
                        UV = null
                    };
                }
            }

            return null;

        }
    }
}
