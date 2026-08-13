using System.Numerics;
using System.Runtime.CompilerServices;

namespace XrEngine.OpenXr
{
    public sealed class DepthGeometryGeneratorParams
    {
        public DepthGeometryGeneratorParams()
        {
            EnableUvFilter = true;
            EnableUvAreaRatioFilter = true;

            MinUvWorldAreaRatio = 0.0f;
            MaxUvWorldAreaRatio = 25.0f;

            MinWorldTriangleArea = 0.000001f;
            MinUvTriangleArea = 0.00000001f;
        }

        /// <summary>
        /// Rejects depth-grid vertices whose reprojection into the RGB camera falls outside the color image.
        ///
        /// This is usually enabled because a generated depth triangle is useful only if it can also receive
        /// color from the paired camera frame.
        ///
        /// Disable only for debugging raw depth geometry without caring whether color projection is valid.
        /// </summary>
        public bool EnableUvFilter { get; set; }

        /// <summary>
        /// Rejects triangles whose world-space area and projected color-UV area are inconsistent.
        ///
        /// This catches triangles created across depth discontinuities or bad projection zones:
        /// a triangle may be valid in the depth grid but become extremely stretched either in world space or
        /// in color-camera UV space.
        ///
        /// Keep enabled for reconstruction input. Disable only when debugging why triangles are being removed.
        /// </summary>
        public bool EnableUvAreaRatioFilter { get; set; }

        /// <summary>
        /// Lower bound for the world-area / UV-area ratio test.
        ///
        /// Despite the property name, the current implementation computes:
        ///
        ///     worldArea / uvArea
        ///
        /// Values below this are rejected. Keeping this at 0 disables the lower-ratio rejection, which is
        /// normally fine because the dangerous case is usually the opposite: huge world triangles compressed
        /// into tiny UV regions.
        ///
        /// Suggested:
        /// 0.0 for normal use.
        /// </summary>
        public float MinUvWorldAreaRatio { get; set; }

        /// <summary>
        /// Upper bound for the world-area / UV-area ratio test.
        ///
        /// This rejects triangles that cover too much 3D surface for too little projected RGB-camera area.
        /// Those are usually long/stretched triangles across depth breaks, grazing surfaces, or projection
        /// artifacts that would smear color badly.
        ///
        /// Suggested:
        /// 20-30 for current reconstruction tests;
        /// lower if stretched triangles leak through;
        /// higher if valid large/flat surfaces are being cut too aggressively.
        /// </summary>
        public float MaxUvWorldAreaRatio { get; set; }

        /// <summary>
        /// Minimum accepted world-space triangle area, in square meters.
        ///
        /// Very tiny triangles are usually numerical debris from depth-grid discontinuities or duplicated
        /// samples. Removing them makes later voxel fusion, collapse and UV unwrap cleaner.
        ///
        /// Suggested:
        /// keep very small, around 1e-6, unless tiny noisy fragments are clearly visible.
        /// </summary>
        public float MinWorldTriangleArea { get; set; }

        /// <summary>
        /// Minimum accepted projected UV triangle area.
        ///
        /// Triangles that collapse to an almost-zero area in the RGB camera image cannot be textured reliably:
        /// many world positions would sample almost the same color pixel, producing smearing.
        ///
        /// Suggested:
        /// keep very small, around 1e-8.
        /// Raise only if very thin projected color slivers create visible artifacts.
        /// </summary>
        public float MinUvTriangleArea { get; set; }
    }

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
        private readonly int _maxVertexCount;

        private readonly GridVertex[] _grid;
        private readonly VertexData[] _vertexBuffer;
        private readonly uint[] _indexBuffer;

        private readonly float[] _uvX;
        private readonly float[] _uvY;
        private readonly float[] _clipX;
        private readonly float[] _clipY;
        private readonly int[] _sampleX;
        private readonly int[] _sampleY;

        private int _sampleDepthWidth;
        private int _sampleDepthHeight;

        private bool _enableUvFilter;
        private bool _enableUvAreaRatioFilter;

        private float _minUvWorldAreaRatio;
        private float _maxUvWorldAreaRatio;

        private float _minWorldTriangleArea;
        private float _minUvTriangleArea;

        private float _minWorldCrossLenSq;
        private float _minUvDet;
        private float _minRatioSq;
        private float _maxRatioSq;

        public DepthGeometryGenerator(int gridWidth, int gridHeight)
        {
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;

            _maxVertexCount = (gridWidth - 1) * (gridHeight - 1) * 6;

            _grid = new GridVertex[gridWidth * gridHeight];
            _vertexBuffer = new VertexData[_maxVertexCount];
            _indexBuffer = new uint[_maxVertexCount];

            _uvX = new float[gridWidth];
            _uvY = new float[gridHeight];
            _clipX = new float[gridWidth];
            _clipY = new float[gridHeight];
            _sampleX = new int[gridWidth];
            _sampleY = new int[gridHeight];

            var invGridW = 1.0f / (gridWidth - 1);
            var invGridH = 1.0f / (gridHeight - 1);

            for (var x = 0; x < gridWidth; x++)
            {
                var uv = x * invGridW;

                _uvX[x] = uv;
                _clipX[x] = uv * 2.0f - 1.0f;
            }

            for (var y = 0; y < gridHeight; y++)
            {
                var uv = y * invGridH;

                _uvY[y] = uv;
                _clipY[y] = uv * 2.0f - 1.0f;
            }

            SetParams(new DepthGeometryGeneratorParams());
        }

        public void SetParams(DepthGeometryGeneratorParams parameters)
        {
            _enableUvFilter = parameters.EnableUvFilter;
            _enableUvAreaRatioFilter = parameters.EnableUvAreaRatioFilter;

            _minUvWorldAreaRatio = parameters.MinUvWorldAreaRatio;
            _maxUvWorldAreaRatio = parameters.MaxUvWorldAreaRatio;

            _minWorldTriangleArea = parameters.MinWorldTriangleArea;
            _minUvTriangleArea = parameters.MinUvTriangleArea;

            var minWorldCrossLen = _minWorldTriangleArea * 2.0f;

            _minWorldCrossLenSq = minWorldCrossLen * minWorldCrossLen;
            _minUvDet = _minUvTriangleArea * 2.0f;
            _minRatioSq = _minUvWorldAreaRatio * _minUvWorldAreaRatio;
            _maxRatioSq = _maxUvWorldAreaRatio * _maxUvWorldAreaRatio;
        }

        public SimpleGeometry3D CreateGeometry(
            ushort* depth,
            int depthWidth,
            int depthHeight,
            Matrix4x4 depthViewProjInv,
            Matrix4x4 colorViewPro)
        {
            var geo = new SimpleGeometry3D();

            UpdateGeometry(
                geo,
                depth,
                depthWidth,
                depthHeight,
                depthViewProjInv,
                colorViewPro);

            return geo;
        }

        public void UpdateGeometry(
            Geometry3D<VertexData> geometry,
            ushort* depth,
            int depthWidth,
            int depthHeight,
            Matrix4x4 depthViewProjInv,
            Matrix4x4 colorViewProj)
        {
            UpdateDepthSampling(depthWidth, depthHeight);

            BuildGrid(
                depth,
                depthWidth,
                depthViewProjInv,
                colorViewProj);

            var vertexCount = BuildTriangles();

            if (vertexCount == 0)
            {
                geometry.VerticesArray = Array.Empty<VertexData>();
                geometry.Indices = Array.Empty<uint>();
            }
            else
            {
                var vertices = GC.AllocateUninitializedArray<VertexData>(vertexCount);
                var indices = GC.AllocateUninitializedArray<uint>(vertexCount);

                Array.Copy(_vertexBuffer, vertices, vertexCount);
                Array.Copy(_indexBuffer, indices, vertexCount);

                geometry.VerticesArray = vertices;
                geometry.Indices = indices;
            }

            geometry.ActiveComponents =
                VertexComponent.Position |
                VertexComponent.UV0 |
                VertexComponent.Normal;

            geometry.UpdateBounds();
        }

        private void UpdateDepthSampling(int depthWidth, int depthHeight)
        {
            if (_sampleDepthWidth == depthWidth &&
                _sampleDepthHeight == depthHeight)
                return;

            _sampleDepthWidth = depthWidth;
            _sampleDepthHeight = depthHeight;

            var maxDepthX = depthWidth - 1;
            var maxDepthY = depthHeight - 1;

            for (var x = 0; x < _gridWidth; x++)
                _sampleX[x] = (int)MathF.Round(_uvX[x] * maxDepthX);

            for (var y = 0; y < _gridHeight; y++)
                _sampleY[y] = (int)MathF.Round(_uvY[y] * maxDepthY);
        }

        private void BuildGrid(
            ushort* depth,
            int depthWidth,
            Matrix4x4 depthViewProjInv,
            Matrix4x4 colorViewProj)
        {
            var depthScale = 1.0f / ushort.MaxValue;
            var enableUvFilter = _enableUvFilter;

            fixed (GridVertex* grid = _grid)
            fixed (int* sampleX = _sampleX)
            fixed (int* sampleY = _sampleY)
            fixed (float* clipX = _clipX)
            fixed (float* clipY = _clipY)
            {
                for (var y = 0; y < _gridHeight; y++)
                {
                    var depthRow = sampleY[y] * depthWidth;
                    var cy = clipY[y];
                    var gridRow = y * _gridWidth;

                    for (var x = 0; x < _gridWidth; x++)
                    {
                        var index = gridRow + x;
                        var gv = grid + index;

                        gv->Valid = false;

                        var rawD = depth[depthRow + sampleX[x]];

                        if (rawD == 0 || rawD == ushort.MaxValue)
                            continue;

                        var clip = new Vector4(
                            clipX[x],
                            cy,
                            rawD * depthScale * 2.0f - 1.0f,
                            1.0f);

                        var world4 = Vector4.Transform(clip, depthViewProjInv);

                        if (world4.W == 0.0f)
                            continue;

                        var invW = 1.0f / world4.W;

                        var world = new Vector3(
                            world4.X * invW,
                            world4.Y * invW,
                            world4.Z * invW);

                        var colorClip = Vector4.Transform(
                            new Vector4(world, 1.0f),
                            colorViewProj);

                        if (colorClip.W == 0.0f)
                            continue;

                        var invColorW = 1.0f / colorClip.W;

                        var u = colorClip.X * invColorW * 0.5f + 0.5f;
                        var v = 0.5f - colorClip.Y * invColorW * 0.5f;

                        if (enableUvFilter)
                        {
                            if (u < 0.0f || u > 1.0f ||
                                v < 0.0f || v > 1.0f)
                                continue;
                        }

                        gv->Pos = world;
                        gv->UV = new Vector2(u, v);
                        gv->Valid = true;
                    }
                }
            }
        }

        private int BuildTriangles()
        {
            var vertexCount = 0;

            fixed (GridVertex* grid = _grid)
            fixed (VertexData* vertices = _vertexBuffer)
            fixed (uint* indices = _indexBuffer)
            {
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

                        EmitTriangle(grid, vertices, indices, i0, i1, i2, ref vertexCount);
                        EmitTriangle(grid, vertices, indices, i1, i3, i2, ref vertexCount);
                    }
                }
            }

            return vertexCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitTriangle(
            GridVertex* grid,
            VertexData* vertices,
            uint* indices,
            int i0,
            int i1,
            int i2,
            ref int vertexCount)
        {
            var a = grid + i0;
            var b = grid + i1;
            var c = grid + i2;

            if (!a->Valid || !b->Valid || !c->Valid)
                return;

            var ab = b->Pos - a->Pos;
            var ac = c->Pos - a->Pos;

            var faceNormal = Vector3.Cross(ab, ac);
            var crossLenSq = faceNormal.LengthSquared();

            if (crossLenSq <= 0.0000000001f)
                return;

            if (_enableUvAreaRatioFilter)
            {
                if (crossLenSq < _minWorldCrossLenSq)
                    return;

                var uvAb = b->UV - a->UV;
                var uvAc = c->UV - a->UV;

                var uvDet = MathF.Abs(uvAb.X * uvAc.Y - uvAb.Y * uvAc.X);

                if (uvDet < _minUvDet)
                    return;

                var ratioSq = crossLenSq / (uvDet * uvDet);

                if (ratioSq < _minRatioSq ||
                    ratioSq > _maxRatioSq)
                    return;
            }

            var normal = faceNormal * (1.0f / MathF.Sqrt(crossLenSq));
            var start = (uint)vertexCount;

            vertices[vertexCount + 0] = new VertexData
            {
                Pos = a->Pos,
                UV = a->UV,
                Normal = normal
            };

            vertices[vertexCount + 1] = new VertexData
            {
                Pos = b->Pos,
                UV = b->UV,
                Normal = normal
            };

            vertices[vertexCount + 2] = new VertexData
            {
                Pos = c->Pos,
                UV = c->UV,
                Normal = normal
            };

            indices[vertexCount + 0] = start + 0;
            indices[vertexCount + 1] = start + 1;
            indices[vertexCount + 2] = start + 2;

            vertexCount += 3;
        }
    }
}