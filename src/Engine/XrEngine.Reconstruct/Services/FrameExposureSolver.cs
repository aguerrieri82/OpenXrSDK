using Common.Interop;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace XrEngine.Reconstruct
{
    public sealed class FrameExposureSolverParams
    {
        public FrameExposureSolverParams()
        {
            BytesPerPixel = 4;
            TriangleStep = 10;
            PatchRadius = 1;

            MaxSamplesPerPair = 2048;
            MinSamplesPerPair = 3;

            MinLuma = 0.03f;
            MaxLuma = 0.97f;

            SolverIterations = 30;
            SolverRelaxation = 0.65f;

            MinExposure = MathF.Log(0.25f);
            MaxExposure = MathF.Log(4f);
        }

        /// <summary>
        /// Number of bytes per source texture pixel.
        ///
        /// Current capture data is RGBA8, so this should normally stay 4.
        /// Change only if the raw color buffer layout changes.
        /// </summary>
        public int BytesPerPixel { get; set; }

        /// <summary>
        /// Subsampling step over projected mesh triangles.
        ///
        /// The exposure solver does not need every triangle. It only needs enough overlapping color samples
        /// between frame pairs to estimate relative brightness.
        ///
        /// Lower values increase samples and accuracy but cost more CPU.
        /// Higher values are faster but may miss overlap regions.
        ///
        /// Suggested:
        /// 5 for more accurate/offline solve;
        /// 10 good default;
        /// 20 for faster rough estimates.
        /// </summary>
        public int TriangleStep { get; set; }

        /// <summary>
        /// Pixel radius around each projected sample used to compare local color patches.
        ///
        /// 0 compares a single pixel.
        /// 1 samples a small 3x3 neighborhood, reducing noise and tiny projection mismatches.
        /// Larger values blur the comparison and can mix across edges.
        ///
        /// Suggested:
        /// 1 as default;
        /// 0 only for very sharp/debug comparison;
        /// 2 only if projection jitter is visibly noisy.
        /// </summary>
        public int PatchRadius { get; set; }

        /// <summary>
        /// Maximum number of valid overlap samples kept for each frame pair.
        ///
        /// Limits solver cost when two frames have a lot of shared visible geometry.
        /// More samples improve robustness but quickly become redundant.
        ///
        /// Suggested:
        /// 1024-2048 for normal use;
        /// 4096 for slower but more stable solves.
        /// </summary>
        public int MaxSamplesPerPair { get; set; }

        /// <summary>
        /// Minimum number of valid overlap samples required before a frame pair contributes to the solve.
        ///
        /// Very weak overlap pairs are noisy and can destabilize exposure estimation.
        ///
        /// Suggested:
        /// 3-8. Raise if tiny accidental overlaps produce bad exposure links.
        /// </summary>
        public int MinSamplesPerPair { get; set; }

        /// <summary>
        /// Minimum accepted luminance for exposure comparison.
        ///
        /// Very dark pixels are unreliable because noise, compression and quantization dominate.
        ///
        /// Suggested:
        /// 0.02-0.05.
        /// </summary>
        public float MinLuma { get; set; }

        /// <summary>
        /// Maximum accepted luminance for exposure comparison.
        ///
        /// Very bright pixels are often clipped or near saturation, so they no longer contain reliable
        /// exposure information.
        ///
        /// Suggested:
        /// 0.95-0.98.
        /// </summary>
        public float MaxLuma { get; set; }

        /// <summary>
        /// Number of relaxation iterations used by the exposure graph solver.
        ///
        /// More iterations allow exposure offsets to propagate through the frame-overlap graph.
        /// Too few iterations can leave inconsistent brightness between distant but connected frames.
        ///
        /// Suggested:
        /// 20-30 normally;
        /// 50 if the frame graph is long/sparse.
        /// </summary>
        public int SolverIterations { get; set; }

        /// <summary>
        /// Relaxation factor for each exposure solver iteration.
        ///
        /// Lower values converge more slowly but are more stable.
        /// Higher values converge faster but can oscillate if pair estimates are noisy.
        ///
        /// Suggested:
        /// 0.5-0.7.
        /// </summary>
        public float SolverRelaxation { get; set; }

        /// <summary>
        /// Lower clamp for solved exposure correction, stored in log space.
        ///
        /// MathF.Log(0.25) means a frame can be darkened/brightened down to 25% relative scale.
        /// Keeps bad overlap estimates from producing extreme corrections.
        /// </summary>
        public float MinExposure { get; set; }

        /// <summary>
        /// Upper clamp for solved exposure correction, stored in log space.
        ///
        /// MathF.Log(4) means a frame can be corrected up to 4x relative scale.
        /// Keeps bad overlap estimates from producing extreme corrections.
        /// </summary>
        public float MaxExposure { get; set; }
    }

    public sealed unsafe class FrameExposureSolver
    {
        #region Private Structs

        private sealed class PairSamples
        {
            public PairSamples(int capacity)
            {
                Values = new float[capacity];
            }

            public readonly float[] Values;

            public int Count;
        }

        private struct ExposureEdge
        {
            public int A;
            public int B;
            public float Delta;
            public float Weight;
        }

        #endregion

        private const float OneThird = 0.33333334f;

        private FrameExposureSolverParams _params;

        private MemoryLock<byte>[]? _imageLocks;

        private PairSamples?[]? _pairs;

        private int _width;
        private int _height;
        private int _frameCount;
        private int _stride;

        private int _bytesPerPixel;
        private int _triangleStep;
        private int _patchRadius;

        private int _maxSamplesPerPair;
        private int _minSamplesPerPair;

        private float _minLuma;
        private float _maxLuma;

        private int _solverIterations;
        private float _solverRelaxation;

        private float _minExposure;
        private float _maxExposure;

        public FrameExposureSolver()
        {
            SetParams(new FrameExposureSolverParams());
        }

        [MemberNotNull(nameof(_params))]
        public void SetParams(FrameExposureSolverParams parameters)
        {
            _params = parameters;

            _bytesPerPixel = parameters.BytesPerPixel;
            _triangleStep = parameters.TriangleStep;
            _patchRadius = parameters.PatchRadius;

            _maxSamplesPerPair = parameters.MaxSamplesPerPair;
            _minSamplesPerPair = parameters.MinSamplesPerPair;

            _minLuma = parameters.MinLuma;
            _maxLuma = parameters.MaxLuma;

            _solverIterations = parameters.SolverIterations;
            _solverRelaxation = parameters.SolverRelaxation;

            _minExposure = parameters.MinExposure;
            _maxExposure = parameters.MaxExposure;
        }

        public float[] Compute(
            Geometry3D<VertexData> geometry,
            IMemoryBuffer<byte>[] images,
            int width,
            int height)
        {
            _width = width;
            _height = height;
            _frameCount = images.Length;
            _stride = width * _bytesPerPixel;

            _pairs = new PairSamples[_frameCount * _frameCount];
            _imageLocks = new MemoryLock<byte>[_frameCount];

            try
            {
                for (var i = 0; i < images.Length; i++)
                    _imageLocks[i] = images[i].MemoryLock();

                var vertices = geometry.Vertices!;
                var indices = geometry.Indices;

                fixed (VertexData* vertexPtr = vertices)
                {
                    if (indices != null && indices.Length >= 3)
                    {
                        fixed (uint* indexPtr = indices)
                        {
                            var triIndex = 0;

                            for (var i = 0; i + 2 < indices.Length; i += 3, triIndex++)
                            {
                                if (triIndex % _triangleStep != 0)
                                    continue;

                                CollectTriangle(
                                    vertexPtr[(int)indexPtr[i + 0]],
                                    vertexPtr[(int)indexPtr[i + 1]],
                                    vertexPtr[(int)indexPtr[i + 2]]);
                            }
                        }
                    }
                    else
                    {
                        var triIndex = 0;

                        for (var i = 0; i + 2 < vertices.Length; i += 3, triIndex++)
                        {
                            if (triIndex % _triangleStep != 0)
                                continue;

                            CollectTriangle(
                                vertexPtr[i + 0],
                                vertexPtr[i + 1],
                                vertexPtr[i + 2]);
                        }
                    }
                }

                var edges = BuildEdges();
                var exposure = Solve(edges);

                Exposures = exposure;

                return exposure;
            }
            finally
            {
                if (_imageLocks != null)
                {
                    for (var i = 0; i < _imageLocks.Length; i++)
                        _imageLocks[i].Dispose();
                }

                _imageLocks = null;
                _pairs = null;
            }
        }

        private void CollectTriangle(VertexData a, VertexData b, VertexData c)
        {
            var image0 = (int)a.Tangent.X;
            var image1 = (int)a.Tangent.Y;

            if (image1 < 0 || image0 == image1)
                return;

            var pairA = image0;
            var pairB = image1;
            var sign = 1.0f;

            if (pairA > pairB)
            {
                pairA = image1;
                pairB = image0;
                sign = -1.0f;
            }

            var pairIndex = pairA * _frameCount + pairB;
            var pair = _pairs![pairIndex];

            if (pair == null)
            {
                pair = new PairSamples(_maxSamplesPerPair);
                _pairs[pairIndex] = pair;
            }
            else if (pair.Count >= _maxSamplesPerPair)
            {
                return;
            }

            var uv0 = (a.UV + b.UV + c.UV) * OneThird;
            var uv1 = (a.UV1 + b.UV1 + c.UV1) * OneThird;

            var imagePtr0 = _imageLocks![image0].Data;
            var imagePtr1 = _imageLocks![image1].Data;

            if (!TrySampleLuma(imagePtr0, uv0, out var luma0) ||
                !TrySampleLuma(imagePtr1, uv1, out var luma1))
            {
                return;
            }

            pair.Values[pair.Count++] = (MathF.Log(luma0) - MathF.Log(luma1)) * sign;
        }

        private List<ExposureEdge> BuildEdges()
        {
            var edges = new List<ExposureEdge>();

            for (var a = 0; a < _frameCount - 1; a++)
            {
                for (var b = a + 1; b < _frameCount; b++)
                {
                    var pair = _pairs![a * _frameCount + b];

                    if (pair == null || pair.Count < _minSamplesPerPair)
                        continue;

                    Array.Sort(pair.Values, 0, pair.Count);

                    var median = pair.Values[pair.Count / 2];

                    edges.Add(new ExposureEdge
                    {
                        A = a,
                        B = b,
                        Delta = Math.Clamp(median, _minExposure, _maxExposure),
                        Weight = MathF.Sqrt(pair.Count)
                    });
                }
            }

            return edges;
        }

        private float[] Solve(List<ExposureEdge> edges)
        {
            var exposure = new float[_frameCount];
            var next = new float[_frameCount];
            var weightSum = new float[_frameCount];

            if (edges.Count == 0)
                return exposure;

            for (var iteration = 0; iteration < _solverIterations; iteration++)
            {
                Array.Clear(next, 0, next.Length);
                Array.Clear(weightSum, 0, weightSum.Length);

                foreach (var edge in edges)
                {
                    var a = edge.A;
                    var b = edge.B;
                    var w = edge.Weight;

                    next[a] += (exposure[b] - edge.Delta) * w;
                    next[b] += (exposure[a] + edge.Delta) * w;

                    weightSum[a] += w;
                    weightSum[b] += w;
                }

                for (var i = 0; i < _frameCount; i++)
                {
                    if (weightSum[i] == 0.0f)
                        continue;

                    var solved = next[i] / weightSum[i];

                    exposure[i] += (solved - exposure[i]) * _solverRelaxation;
                }

                NormalizeMedian(exposure);
            }

            for (var i = 0; i < exposure.Length; i++)
                exposure[i] = Math.Clamp(exposure[i], _minExposure, _maxExposure);

            return exposure;
        }

        private bool TrySampleLuma(byte* image, Vector2 uv, out float luma)
        {
            var x = (int)(uv.X * (_width - 1) + 0.5f);
            var y = (int)(uv.Y * (_height - 1) + 0.5f);

            if (_patchRadius == 0)
            {
                var ptr = image + y * _stride + x * _bytesPerPixel;

                luma =
                    ptr[0] * 0.0008337255f +
                    ptr[1] * 0.0028047059f +
                    ptr[2] * 0.00028313726f;

                return luma >= _minLuma && luma <= _maxLuma;
            }

            var x0 = Math.Max(0, x - _patchRadius);
            var y0 = Math.Max(0, y - _patchRadius);
            var x1 = Math.Min(_width - 1, x + _patchRadius);
            var y1 = Math.Min(_height - 1, y + _patchRadius);

            var sum = 0.0f;
            var count = 0;

            for (var py = y0; py <= y1; py++)
            {
                var row = image + py * _stride;

                for (var px = x0; px <= x1; px++)
                {
                    var ptr = row + px * _bytesPerPixel;

                    var sample =
                        ptr[0] * 0.0008337255f +
                        ptr[1] * 0.0028047059f +
                        ptr[2] * 0.00028313726f;

                    if (sample < _minLuma || sample > _maxLuma)
                        continue;

                    sum += sample;
                    count++;
                }
            }

            if (count == 0)
            {
                luma = 0.0f;
                return false;
            }

            luma = sum / count;
            return true;
        }

        private static void NormalizeMedian(float[] values)
        {
            var sorted = new float[values.Length];

            Array.Copy(values, sorted, values.Length);
            Array.Sort(sorted);

            var median = sorted[sorted.Length / 2];

            for (var i = 0; i < values.Length; i++)
                values[i] -= median;
        }

        public FrameExposureSolverParams Params
        {
            get => _params;
            set => SetParams(value);
        }

        public float[] Exposures { get; private set; } = [];
    }
}