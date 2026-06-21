using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine.OpenXr
{
    public sealed unsafe class DepthGeometryGenerator
    {
        private struct GridVertex
        {
            public Vector3 Pos;
            public Vector2 UV;
            public bool Valid;
        }


        private readonly int _gridWidth;
        private readonly int _gridHeight;

        private readonly GridVertex[] _grid;
        private readonly List<VertexData> _vertices;

        public DepthGeometryGenerator(int gridWidth, int gridHeight)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;

            _grid = new GridVertex[gridWidth * gridHeight];
            _vertices = new List<VertexData>((gridWidth - 1) * (gridHeight - 1) * 6);

            EnableUvFilter = true;
            EnableUvAreaRatioFilter = true;

            MinUvWorldAreaRatio = 0;
            MaxUvWorldAreaRatio = 25;

            MinWorldTriangleArea = 0.000001f;
            MinUvTriangleArea = 0.00000001f;
        }

        public Geometry3D CreateGeometry(ushort* depth,
            int depthWidth,
            int depthHeight,
            Matrix4x4 depthViewProjInv,
            Matrix4x4 colorViewPro)
        {
            var geo = new Geometry3D();
            UpdateGeometry(geo, depth, depthWidth, depthHeight, depthViewProjInv, colorViewPro);
            return geo;
        }

        public void UpdateGeometry(
            Geometry3D geometry,
            ushort* depth,
            int depthWidth,
            int depthHeight,
            Matrix4x4 depthViewProjInv,
            Matrix4x4 colorViewProj)
        {
            BuildGrid(
                depth,
                depthWidth,
                depthHeight,
                depthViewProjInv,
                colorViewProj);

            BuildTriangles();

            geometry.Vertices = _vertices.ToArray();

            geometry.ActiveComponents =
                VertexComponent.Position |
                VertexComponent.UV0 |
                VertexComponent.Normal;

            geometry.ComputeNormals();
            geometry.UpdateBounds();
            geometry.ComputeIndices();
        }

        private void BuildGrid(
            ushort* depth,
            int depthWidth,
            int depthHeight,
            Matrix4x4 depthViewProjInv,
            Matrix4x4 colorViewProj)
        {
            var invGridW = 1.0f / (_gridWidth - 1);
            var invGridH = 1.0f / (_gridHeight - 1);

            var maxDepthX = depthWidth - 1;
            var maxDepthY = depthHeight - 1;

            for (var y = 0; y < _gridHeight; y++)
            {
                var uvY = y * invGridH;
                var dy = (int)MathF.Round(uvY * maxDepthY);

                for (var x = 0; x < _gridWidth; x++)
                {
                    var uvX = x * invGridW;
                    var dx = (int)MathF.Round(uvX * maxDepthX);

                    var index = y * _gridWidth + x;
                    ref var gv = ref _grid[index];

                    gv.Valid = false;

                    var rawD = depth[dy * depthWidth + dx];

                    if (rawD == 0 || rawD == ushort.MaxValue)
                        continue;

                    var d = rawD / (float)ushort.MaxValue;

                    var clip = new Vector4(
                        uvX * 2.0f - 1.0f,
                        uvY * 2.0f - 1.0f,
                        d * 2.0f - 1.0f,
                        1.0f);

                    var world4 = Vector4.Transform(clip, depthViewProjInv);

                    if (world4.W == 0.0f)
                        continue;

                    var invW = 1.0f / world4.W;

                    var world = new Vector3(
                        world4.X * invW,
                        world4.Y * invW,
                        world4.Z * invW);

                    var colorClip = Vector4.Transform(new Vector4(world, 1.0f), colorViewProj);

                    if (colorClip.W == 0.0f)
                        continue;

                    var invColorW = 1.0f / colorClip.W;

                    var colorUv = new Vector2(
                        colorClip.X * invColorW * 0.5f + 0.5f,
                        colorClip.Y * invColorW * 0.5f + 0.5f);

                    colorUv.Y = 1.0f - colorUv.Y;

                    gv.Pos = world;
                    gv.UV = colorUv;
                    gv.Valid = true;
                }
            }
        }

        private void BuildTriangles()
        {
            _vertices.Clear();

            for (var y = 0; y < _gridHeight - 1; y++)
            {
                var row0 = y * _gridWidth;
                var row1 = row0 + _gridWidth;

                for (var x = 0; x < _gridWidth - 1; x++)
                {
                    var i0 = row0 + x;
                    var i1 = i0 + 1;
                    var i2 = row1 + x;
                    var i3 = i2 + 1;

                    EmitTriangle(i0, i1, i2);
                    EmitTriangle(i1, i3, i2);
                }
            }
        }

        private void EmitTriangle(int i0, int i1, int i2)
        {
            ref var a = ref _grid[i0];
            ref var b = ref _grid[i1];
            ref var c = ref _grid[i2];

            if (!a.Valid || !b.Valid || !c.Valid)
                return;

            if (EnableUvFilter)
            {
                if (!IsUvCovered(a.UV) ||
                    !IsUvCovered(b.UV) ||
                    !IsUvCovered(c.UV))
                    return;
            }

            if (EnableUvAreaRatioFilter)
            {
                var worldArea = TriangleArea3D(a.Pos, b.Pos, c.Pos);

                if (worldArea < MinWorldTriangleArea)
                    return;

                var uvArea = TriangleArea2D(a.UV, b.UV, c.UV);

                if (uvArea < MinUvTriangleArea)
                    return;

                var ratio = worldArea / uvArea;

                if (ratio < MinUvWorldAreaRatio ||
                    ratio > MaxUvWorldAreaRatio)
                    return;
            }

            _vertices.Add(new VertexData
            {
                Pos = a.Pos,
                UV = a.UV
            });

            _vertices.Add(new VertexData
            {
                Pos = b.Pos,
                UV = b.UV
            });

            _vertices.Add(new VertexData
            {
                Pos = c.Pos,
                UV = c.UV
            });
        }

        private static bool IsUvCovered(Vector2 uv)
        {
            return uv.X >= 0.0f && uv.X <= 1.0f &&
                   uv.Y >= 0.0f && uv.Y <= 1.0f;
        }

        private static float TriangleArea3D(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(b - a, c - a).Length() * 0.5f;
        }

        private static float TriangleArea2D(Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = b - a;
            var ac = c - a;

            return MathF.Abs(ab.X * ac.Y - ab.Y * ac.X) * 0.5f;
        }

        public bool EnableUvFilter { get; set; } 
        public bool EnableUvAreaRatioFilter { get; set; } 

        public float MinUvWorldAreaRatio { get; set; } 
        public float MaxUvWorldAreaRatio { get; set; } 

        public float MinWorldTriangleArea { get; set; } 
        public float MinUvTriangleArea { get; set; }

    }
}
