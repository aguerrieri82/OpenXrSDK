using Common.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using XrMath;
using static XrEngine.Reconstruct.DepthSnapeshot;

namespace XrEngine.Reconstruct
{
    public sealed class FrameExposureSolverParams
    {
        public FrameExposureSolverParams()
        {
            BytesPerPixel = 4;
            TriangleStep = 4;
            PatchRadius = 1;

            MaxSamplesPerPair = 512;
            MinSamplesPerEdge = 24;

            BoundsPadding = 0.05f;

            MinLuma = 0.03f;
            MaxLuma = 0.97f;

            SolverIterations = 48;
            SolverRelaxation = 0.65f;

            MinExposure = -1.3862944f;
            MaxExposure = 1.3862944f;
        }

        public int BytesPerPixel { get; set; }

        public int TriangleStep { get; set; }

        public int PatchRadius { get; set; }

        public int MaxSamplesPerPair { get; set; }

        public int MinSamplesPerEdge { get; set; }

        public float BoundsPadding { get; set; }

        public float MinLuma { get; set; }

        public float MaxLuma { get; set; }

        public int SolverIterations { get; set; }

        public float SolverRelaxation { get; set; }

        public float MinExposure { get; set; }

        public float MaxExposure { get; set; }
    }

    public sealed unsafe class FrameExposureSolver
    {
        #region Private Structs

        private struct MeshInfo
        {
            public int Index;
            public int ImageIndex;
            public TriangleMesh Mesh;
            public Geometry3D Geometry;
            public CaptureFrame Frame;
            public DepthFrameMeta Meta;
            public Matrix4x4 WorldMatrix;
            public Matrix4x4 CameraViewProj;
            public Bounds3 Bounds;
            public List<Sample> Samples;
        }

        private struct ImageView
        {
            public byte* Data;
            public int Width;
            public int Height;
            public int Stride;
        }

        private struct Sample
        {
            public Vector3 WorldPos;
            public float Luma;
        }

        private struct ExposureEdge
        {
            public int A;
            public int B;
            public float Delta;
            public float Weight;
        }

        #endregion

        private const bool FlipProjectedY = false;
        private const bool FlipImageY = false;

        private int _bytesPerPixel;
        private int _triangleStep;
        private int _patchRadius;

        private int _maxSamplesPerPair;
        private int _minSamplesPerEdge;

        private float _boundsPadding;

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

        public void SetParams(FrameExposureSolverParams p)
        {
            _bytesPerPixel = p.BytesPerPixel;
            _triangleStep = p.TriangleStep;
            _patchRadius = p.PatchRadius;

            _maxSamplesPerPair = p.MaxSamplesPerPair;
            _minSamplesPerEdge = p.MinSamplesPerEdge;

            _boundsPadding = p.BoundsPadding;

            _minLuma = p.MinLuma;
            _maxLuma = p.MaxLuma;

            _solverIterations = p.SolverIterations;
            _solverRelaxation = p.SolverRelaxation;

            _minExposure = p.MinExposure;
            _maxExposure = p.MaxExposure;
        }

        public float[] Compute(IReadOnlyList<TriangleMesh> meshes, IMemoryBuffer<byte>[] images)
        {
            var locks = new MemoryLock<byte>[images.Length];
            var locked = new bool[images.Length];

            try
            {
                var imageViews = new ImageView[images.Length];

                for (var i = 0; i < images.Length; i++)
                {
                    locks[i] = images[i].MemoryLock();
                    locked[i] = true;
                }

                var infos = CollectMeshes(meshes, images, locks, imageViews);

                for (var i = 0; i < infos.Count; i++)
                    infos[i] = BuildSamples(infos[i], imageViews[infos[i].ImageIndex]);

                var edges = BuildEdges(infos, imageViews);
                var exposure = Solve(infos.Count, edges);

                var result = new float[images.Length];

                for (var i = 0; i < infos.Count; i++)
                {
                    var value = exposure[i];

                    infos[i].Frame.Exposure = value;
                    result[infos[i].ImageIndex] = value;
                }

                return result;
            }
            finally
            {
                for (var i = 0; i < locks.Length; i++)
                {
                    if (locked[i])
                        locks[i].Dispose();
                }
            }
        }

        private List<MeshInfo> CollectMeshes(
            IReadOnlyList<TriangleMesh> meshes,
            IMemoryBuffer<byte>[] images,
            MemoryLock<byte>[] locks,
            ImageView[] imageViews)
        {
            var result = new List<MeshInfo>();

            foreach (var mesh in meshes)
            {
                if (!mesh.TryComponent<CaptureFrame>(out var frame))
                    continue;

                if (frame.Meta == null)
                    continue;

                var imageIndex = frame.Meta.Frame;

                if (imageIndex < 0 || imageIndex >= images.Length)
                    continue;

                var geometry = mesh.Geometry;

                if (geometry?.Vertices == null || geometry.Vertices.Length == 0)
                    continue;

                imageViews[imageIndex] = new ImageView
                {
                    Data = locks[imageIndex].Data,
                    Width = frame.Meta.ColorWidth,
                    Height = frame.Meta.ColorHeight,
                    Stride = frame.Meta.ColorWidth * _bytesPerPixel
                };

                result.Add(new MeshInfo
                {
                    Index = result.Count,
                    ImageIndex = imageIndex,
                    Mesh = mesh,
                    Geometry = geometry,
                    Frame = frame,
                    Meta = frame.Meta,
                    WorldMatrix = mesh.WorldMatrix,
                    CameraViewProj = frame.Meta.CameraView * frame.Meta.CameraProj,
                    Bounds = new Bounds3
                    {
                        Min = mesh.WorldBounds.Min,
                        Max = mesh.WorldBounds.Max,
                    },
                    Samples = new List<Sample>()
                });
            }

            return result;
        }

        private MeshInfo BuildSamples(MeshInfo info, ImageView image)
        {
            var vertices = info.Geometry.Vertices;
            var indices = info.Geometry.Indices;

            if (indices == null || indices.Length < 3)
                return info;

            var triIndex = 0;

            for (var i = 0; i + 2 < indices.Length; i += 3, triIndex++)
            {
                if (triIndex % _triangleStep != 0)
                    continue;

                var i0 = (int)indices[i + 0];
                var i1 = (int)indices[i + 1];
                var i2 = (int)indices[i + 2];

                if (i0 < 0 || i0 >= vertices.Length ||
                    i1 < 0 || i1 >= vertices.Length ||
                    i2 < 0 || i2 >= vertices.Length)
                    continue;

                var v0 = vertices[i0];
                var v1 = vertices[i1];
                var v2 = vertices[i2];

                var uv = (v0.UV + v1.UV + v2.UV) / 3.0f;

                if (!IsUvDefined(uv))
                    continue;

                if (!TrySampleLuma(image, uv, out var luma))
                    continue;

                var pos = (v0.Pos + v1.Pos + v2.Pos) / 3.0f;

                info.Samples.Add(new Sample
                {
                    WorldPos = Vector3.Transform(pos, info.WorldMatrix),
                    Luma = luma
                });
            }

            return info;
        }

        private List<ExposureEdge> BuildEdges(List<MeshInfo> infos, ImageView[] images)
        {
            var edges = new List<ExposureEdge>();

            for (var i = 0; i < infos.Count; i++)
            {
                for (var j = i + 1; j < infos.Count; j++)
                {
                    if (!BoundsIntersect(infos[i].Bounds, infos[j].Bounds, _boundsPadding))
                        continue;

                    if (TryCreateEdge(infos[i], infos[j], images, out var edge))
                        edges.Add(edge);
                }
            }

            return edges;
        }

        private bool TryCreateEdge(MeshInfo a, MeshInfo b, ImageView[] images, out ExposureEdge edge)
        {
            edge = default;

            var deltas = new List<float>(_maxSamplesPerPair * 2);

            CollectDeltas(a, b, images[b.ImageIndex], 1.0f, deltas);
            CollectDeltas(b, a, images[a.ImageIndex], -1.0f, deltas);

            if (deltas.Count < _minSamplesPerEdge)
                return false;

            deltas.Sort();

            var median = deltas[deltas.Count / 2];

            edge = new ExposureEdge
            {
                A = a.Index,
                B = b.Index,
                Delta = Math.Clamp(median, _minExposure, _maxExposure),
                Weight = MathF.Sqrt(deltas.Count)
            };

            return true;
        }

        private void CollectDeltas(
            MeshInfo source,
            MeshInfo target,
            ImageView targetImage,
            float sign,
            List<float> deltas)
        {
            var count = 0;

            foreach (var sample in source.Samples)
            {
                if (!TryProject(sample.WorldPos, target.CameraViewProj, out var targetUv))
                    continue;

                if (!TrySampleLuma(targetImage, targetUv, out var targetLuma))
                    continue;

                deltas.Add((MathF.Log(sample.Luma) - MathF.Log(targetLuma)) * sign);

                count++;

                if (count >= _maxSamplesPerPair)
                    break;
            }
        }

        private float[] Solve(int count, List<ExposureEdge> edges)
        {
            var exposure = new float[count];
            var next = new float[count];
            var weightSum = new float[count];

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

                for (var i = 0; i < count; i++)
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

        private bool TryProject(Vector3 worldPos, Matrix4x4 viewProj, out Vector2 uv)
        {
            uv = default;

            var clip = Vector4.Transform(new Vector4(worldPos, 1.0f), viewProj);

            if (clip.W <= 0.0001f)
                return false;

            var invW = 1.0f / clip.W;

            var ndcX = clip.X * invW;
            var ndcY = clip.Y * invW;

            if (ndcX < -1.0f || ndcX > 1.0f ||
                ndcY < -1.0f || ndcY > 1.0f)
                return false;

            uv = new Vector2(
                ndcX * 0.5f + 0.5f,
                ndcY * 0.5f + 0.5f);

            if (FlipProjectedY)
                uv.Y = 1.0f - uv.Y;

            return true;
        }

        private bool TrySampleLuma(ImageView image, Vector2 uv, out float luma)
        {
            luma = 0.0f;

            if (!IsUvDefined(uv))
                return false;

            var v = FlipImageY ? 1.0f - uv.Y : uv.Y;

            var x = (int)MathF.Round(uv.X * (image.Width - 1));
            var y = (int)MathF.Round(v * (image.Height - 1));

            var sum = 0.0f;
            var count = 0;

            for (var py = -_patchRadius; py <= _patchRadius; py++)
            {
                var sy = y + py;

                if (sy < 0 || sy >= image.Height)
                    continue;

                for (var px = -_patchRadius; px <= _patchRadius; px++)
                {
                    var sx = x + px;

                    if (sx < 0 || sx >= image.Width)
                        continue;

                    var ptr = image.Data + sy * image.Stride + sx * _bytesPerPixel;

                    var r = ptr[0] / 255.0f;
                    var g = ptr[1] / 255.0f;
                    var b = ptr[2] / 255.0f;

                    var yLum = r * 0.2126f + g * 0.7152f + b * 0.0722f;

                    if (yLum < _minLuma || yLum > _maxLuma)
                        continue;

                    sum += yLum;
                    count++;
                }
            }

            if (count == 0)
                return false;

            luma = sum / count;
            return true;
        }

        private static bool BoundsIntersect(Bounds3 a, Bounds3 b, float padding)
        {
            return a.Min.X - padding <= b.Max.X && a.Max.X + padding >= b.Min.X &&
                   a.Min.Y - padding <= b.Max.Y && a.Max.Y + padding >= b.Min.Y &&
                   a.Min.Z - padding <= b.Max.Z && a.Max.Z + padding >= b.Min.Z;
        }

        private static bool IsUvDefined(Vector2 uv)
        {
            return uv.X >= 0.0f && uv.X <= 1.0f &&
                   uv.Y >= 0.0f && uv.Y <= 1.0f;
        }

        private static void NormalizeMedian(float[] values)
        {
            var sorted = values.ToArray();

            Array.Sort(sorted);

            var median = sorted[sorted.Length / 2];

            for (var i = 0; i < values.Length; i++)
                values[i] -= median;
        }
    }
}