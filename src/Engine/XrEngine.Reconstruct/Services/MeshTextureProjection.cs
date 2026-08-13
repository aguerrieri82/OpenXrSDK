using Common.Interop;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XrEngine.Reconstruct
{
    public class ColorProjectionFrame
    {
        public ColorProjectionFrame(
            int imageIndex,
            Vector3 cameraPosition,
            Matrix4x4 viewProj,
            IMemoryBuffer<byte>? depthMap = null,
            int depthWidth = 0,
            int depthHeight = 0)
        {
            ImageIndex = imageIndex;
            CameraPosition = cameraPosition;
            ViewProj = viewProj;
            DepthMap = depthMap;
            DepthWidth = depthWidth;
            DepthHeight = depthHeight;
        }

        public int ImageIndex;

        public Vector3 CameraPosition;

        public Matrix4x4 ViewProj;

        public IMemoryBuffer<byte>? DepthMap;

        public Texture2D? DepthTexture;

        public int DepthWidth;

        public int DepthHeight;
    }

    public sealed class MeshTextureProjectionParams
    {
        public MeshTextureProjectionParams()
        {
            UvBorder = 0.01f;
            PreferCameraDistance = true;
            DepthBias = 200;
            MinVisibleSamples = 1; // Max 3
        }

        /// <summary>
        /// Margin excluded near the source image UV border.
        ///
        /// Projection samples close to 0/1 UV are fragile: small calibration/projection errors can sample
        /// outside the camera image or hit clamped edge pixels. This margin rejects those border samples.
        ///
        /// Suggested:
        /// 0.005-0.01 normally;
        /// increase if projected colors smear at image borders;
        /// decrease only if too much useful coverage is being rejected.
        /// </summary>
        public float UvBorder { get; set; }

        /// <summary>
        /// When multiple capture frames can color the same surface, prefer the frame whose camera is closer
        /// to the 3D point.
        ///
        /// Closer frames usually contain more texture detail and less projection blur.
        /// Disable only when testing pure visibility/projection behavior or when distance preference creates
        /// unstable frame switching.
        /// </summary>
        public bool PreferCameraDistance { get; set; }

        /// <summary>
        /// Visibility tolerance used by the legacy non-unwrap projection path when comparing against the
        /// 16-bit depth map.
        ///
        /// The comparison is:
        ///
        ///     projectedDepth &lt;= depthMapValue + DepthBias
        ///
        /// so the sample is accepted if the projected mesh point is in front of the stored depth, or only
        /// slightly behind it within this tolerance.
        ///
        /// Units are raw ushort depth units, not meters and not millimeters. The practical value depends on
        /// how the depth buffer was encoded. In the current pipeline, 200 works as a good tolerance: large
        /// enough to absorb projection/reconstruction mismatch, small enough to reject clearly hidden surfaces.
        ///
        /// Suggested:
        /// 100 = stricter, may reject valid samples;
        /// 200 = current working default;
        /// 300+ = more permissive, may leak colors from hidden/back surfaces.
        /// </summary>
        public int DepthBias { get; set; }

        /// <summary>
        /// Minimum number of visibility confirmations required before a projected color choice is accepted.
        ///
        /// Higher values reject unstable samples, but they can also create holes when only one frame sees a
        /// region clearly.
        ///
        /// Suggested:
        /// 1 for maximum coverage;
        /// 2 for stricter projection if enough overlapping captures exist;
        /// 3 is the practical upper limit and can be too aggressive.
        /// </summary>
        public int MinVisibleSamples { get; set; }
    }

    public sealed unsafe class MeshTextureProjection
    {
        private const float OneThird = 0.33333334f;

        private static readonly Vector4 ProjectionScale = new(0.5f, -0.5f, 0.5f, 0.0f);

        private static readonly Vector4 ProjectionOffset = new(0.5f, 0.5f, 0.5f, 0.0f);

        private MemoryLock<byte>[]? _depthLocks;

        private float _uvBorder;

        private float _uvMax;

        private bool _preferCameraDistance;

        private ushort _depthBias;

        private int _minVisibleSamples;
        private int _visibleDepth;
        private int _hiddenDepth;

        #region Private Structs

        private struct Candidate
        {
            public int ImageIndex;
            public Vector2 UvA;
            public Vector2 UvB;
            public Vector2 UvC;
            public float Score;
        }

        #endregion

        public MeshTextureProjection()
        {
            SetParams(new MeshTextureProjectionParams());
        }

        public MeshTextureProjection(MeshTextureProjectionParams parameters)
        {
            SetParams(parameters);
        }

        public void SetParams(MeshTextureProjectionParams parameters)
        {
            _uvBorder = parameters.UvBorder;
            _uvMax = 1.0f - _uvBorder;
            _preferCameraDistance = parameters.PreferCameraDistance;
            _depthBias = (ushort)parameters.DepthBias;
            _minVisibleSamples = Math.Clamp(parameters.MinVisibleSamples, 1, 4);
        }

        public void Project(Geometry3D<VertexData> geometry, IReadOnlyList<ColorProjectionFrame> frames)
        {
            var sourceVertices = geometry.Vertices;

            if (sourceVertices == null || sourceVertices.Length < 3 || frames.Count == 0)
                return;

            var sourceIndices = geometry.Indices;
            var frameCount = frames.Count;

            ColorProjectionFrame[]? copiedFrames = null;
            ReadOnlySpan<ColorProjectionFrame> frameSpan;

            if (frames is ColorProjectionFrame[] frameArray)
                frameSpan = frameArray;
            else if (frames is List<ColorProjectionFrame> frameList)
                frameSpan = CollectionsMarshal.AsSpan(frameList);
            else
            {
                copiedFrames = new ColorProjectionFrame[frameCount];

                for (var i = 0; i < frameCount; i++)
                    copiedFrames[i] = frames[i];

                frameSpan = copiedFrames;
            }

            _depthLocks = new MemoryLock<byte>[frameCount];

            try
            {
                LockDepthMaps(frameSpan);

                var maxTriangleCount = sourceIndices != null && sourceIndices.Length >= 3
                    ? sourceIndices.Length / 3
                    : sourceVertices.Length / 3;

                var targetVertices = new VertexData[maxTriangleCount * 3];
                var targetIndices = new uint[maxTriangleCount * 3];

                var vertexCount = 0;
                var indexCount = 0;

                fixed (VertexData* sourceVertexPtr = sourceVertices)
                fixed (VertexData* targetVertexPtr = targetVertices)
                fixed (uint* targetIndexPtr = targetIndices)
                {
                    if (sourceIndices != null && sourceIndices.Length >= 3)
                    {
                        fixed (uint* sourceIndexPtr = sourceIndices)
                        {
                            for (var i = 0; i + 2 < sourceIndices.Length; i += 3)
                            {
                                EmitProjectedTriangle(
                                    sourceVertexPtr[(int)sourceIndexPtr[i + 0]],
                                    sourceVertexPtr[(int)sourceIndexPtr[i + 1]],
                                    sourceVertexPtr[(int)sourceIndexPtr[i + 2]],
                                    frameSpan,
                                    targetVertexPtr,
                                    targetIndexPtr,
                                    ref vertexCount,
                                    ref indexCount);
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i + 2 < sourceVertices.Length; i += 3)
                        {
                            EmitProjectedTriangle(
                                sourceVertexPtr[i + 0],
                                sourceVertexPtr[i + 1],
                                sourceVertexPtr[i + 2],
                                frameSpan,
                                targetVertexPtr,
                                targetIndexPtr,
                                ref vertexCount,
                                ref indexCount);
                        }
                    }
                }

                if (vertexCount != targetVertices.Length)
                    Array.Resize(ref targetVertices, vertexCount);

                if (indexCount != targetIndices.Length)
                    Array.Resize(ref targetIndices, indexCount);

                geometry.Vertices = targetVertices;
                geometry.Indices = targetIndices;

                geometry.ActiveComponents |=
                    VertexComponent.UV0 |
                    VertexComponent.UV1 |
                    VertexComponent.Tangent;
            }
            finally
            {
                UnlockDepthMaps();
            }
        }

        private void LockDepthMaps(ReadOnlySpan<ColorProjectionFrame> frames)
        {
            for (var i = 0; i < frames.Length; i++)
            {
                ref readonly var frame = ref frames[i];

                if (frame.DepthMap == null || frame.DepthWidth <= 0 || frame.DepthHeight <= 0)
                    continue;

                _depthLocks![i] = frame.DepthMap.MemoryLock();
            }
        }

        private void UnlockDepthMaps()
        {
            if (_depthLocks == null)
                return;

            for (var i = 0; i < _depthLocks.Length; i++)
            {
                if (_depthLocks[i].Data != null)
                    _depthLocks[i].Dispose();
            }

            _depthLocks = null;
        }

        private void EmitProjectedTriangle(
            VertexData a,
            VertexData b,
            VertexData c,
            ReadOnlySpan<ColorProjectionFrame> frames,
            VertexData* vertices,
            uint* indices,
            ref int vertexCount,
            ref int indexCount)
        {
            var best0 = new Candidate
            {
                ImageIndex = -1,
                Score = float.MaxValue
            };

            var best1 = new Candidate
            {
                ImageIndex = -1,
                Score = float.MaxValue
            };

            var center = (a.Pos + b.Pos + c.Pos) * OneThird;

            for (var i = 0; i < frames.Length; i++)
            {
                ref readonly var frame = ref frames[i];

                if (!TryProject(a.Pos, in frame, out var uvA, out var depthA) ||
                    !TryProject(b.Pos, in frame, out var uvB, out var depthB) ||
                    !TryProject(c.Pos, in frame, out var uvC, out var depthC))
                {
                    continue;
                }

                var frameDepth = (ushort*)_depthLocks![i].Data;

                if (frameDepth != null)
                {
                    if (!TryProject(center, in frame, out var uvCenter, out var depthCenter))
                        continue;

                    var visibleSamples = 0;
                    var remainingSamples = 4;

                    if (IsVisibleInDepthMap(uvCenter, depthCenter, frameDepth, frame.DepthWidth, frame.DepthHeight))
                    {
                        visibleSamples++;
                        _visibleDepth++;
                    }
                    else
                        _hiddenDepth++;

                    remainingSamples--;

                    if (visibleSamples + remainingSamples < _minVisibleSamples)
                        continue;

                    if (IsVisibleInDepthMap(uvA, depthA, frameDepth, frame.DepthWidth, frame.DepthHeight))
                        visibleSamples++;

                    remainingSamples--;

                    if (visibleSamples + remainingSamples < _minVisibleSamples)
                        continue;

                    if (IsVisibleInDepthMap(uvB, depthB, frameDepth, frame.DepthWidth, frame.DepthHeight))
                        visibleSamples++;

                    remainingSamples--;

                    if (visibleSamples + remainingSamples < _minVisibleSamples)
                        continue;

                    if (IsVisibleInDepthMap(uvC, depthC, frameDepth, frame.DepthWidth, frame.DepthHeight))
                        visibleSamples++;

                    if (visibleSamples < _minVisibleSamples)
                        continue;
                }

                var score = 0.0f;

                if (_preferCameraDistance)
                    score += Vector3.DistanceSquared(center, frame.CameraPosition);

                var uvCenter2 = (uvA + uvB + uvC) * OneThird - new Vector2(0.5f);

                score += Vector2.Dot(uvCenter2, uvCenter2) * 0.05f;

                var candidate = new Candidate
                {
                    ImageIndex = frame.ImageIndex,
                    UvA = uvA,
                    UvB = uvB,
                    UvC = uvC,
                    Score = score
                };

                if (score < best0.Score)
                {
                    best1 = best0;
                    best0 = candidate;
                }
                else if (score < best1.Score)
                {
                    best1 = candidate;
                }
            }

            if (best0.ImageIndex < 0)
                return;

            ApplyProjection(ref a, best0.ImageIndex, best0.UvA, best1.ImageIndex, best1.UvA);
            ApplyProjection(ref b, best0.ImageIndex, best0.UvB, best1.ImageIndex, best1.UvB);
            ApplyProjection(ref c, best0.ImageIndex, best0.UvC, best1.ImageIndex, best1.UvC);

            var start = (uint)vertexCount;

            vertices[vertexCount + 0] = a;
            vertices[vertexCount + 1] = b;
            vertices[vertexCount + 2] = c;

            indices[indexCount + 0] = start + 0;
            indices[indexCount + 1] = start + 1;
            indices[indexCount + 2] = start + 2;

            vertexCount += 3;
            indexCount += 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsVisibleInDepthMap(
            Vector2 uv,
            ushort depth,
            ushort* depthData,
            int width,
            int height)
        {
            var x = (int)(uv.X * width);
            var y = height - 1 - (int)(uv.Y * height);

            if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                return false;

            var mapDepth = depthData[(y * width) + x];

            return depth <= mapDepth + _depthBias;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyProjection(
            ref VertexData vertex,
            int image0,
            Vector2 uv0,
            int image1,
            Vector2 uv1)
        {
            vertex.UV = uv0;
            vertex.UV1 = image1 >= 0 ? uv1 : uv0;

            vertex.Tangent = new Vector4(
                image0,
                image1,
                0.0f,
                0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryProject(
            Vector3 position,
            in ColorProjectionFrame frame,
            out Vector2 uv,
            out ushort depth)
        {
            var clip = Vector4.Transform(new Vector4(position, 1.0f), frame.ViewProj);

            if (clip.W <= 0.0f)
            {
                uv = default;
                depth = 0;
                return false;
            }

            var p = Vector4.Divide(clip, clip.W);

            p = Vector4.Multiply(p, ProjectionScale) + ProjectionOffset;

            if (p.X < _uvBorder || p.X > _uvMax ||
                p.Y < _uvBorder || p.Y > _uvMax ||
                p.Z < 0.0f || p.Z > 1.0f)
            {
                uv = default;
                depth = 0;
                return false;
            }

            uv = new Vector2(p.X, p.Y);
            depth = (ushort)(p.Z * 65535.0f + 0.5f);

            return true;
        }
    }
}