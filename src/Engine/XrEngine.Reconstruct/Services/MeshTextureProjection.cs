using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XrEngine.Reconstruct
{
    public readonly struct ColorProjectionFrame
    {
        public ColorProjectionFrame(int imageIndex, Vector3 cameraPosition, Matrix4x4 viewProj)
        {
            ImageIndex = imageIndex;
            CameraPosition = cameraPosition;
            ViewProj = viewProj;
        }

        public readonly int ImageIndex;

        public readonly Vector3 CameraPosition;

        public readonly Matrix4x4 ViewProj;
    }

    public sealed class MeshTextureProjection
    {
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
            UvBorder = 0.01f;
            PreferCameraDistance = true;
        }

        public unsafe void Project(Geometry3D geometry, IReadOnlyList<ColorProjectionFrame> frames)
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

            var maxTriangleCount = sourceIndices != null && sourceIndices.Length >= 3
                ? sourceIndices.Length / 3
                : sourceVertices.Length / 3;

            var targetVertices = new VertexData[maxTriangleCount * 3];
            var targetIndices = new uint[maxTriangleCount * 3];

            var vertexCount = 0;
            var indexCount = 0;

            var uvBorder = UvBorder;
            var uvMax = 1.0f - uvBorder;
            var preferCameraDistance = PreferCameraDistance;

            fixed (ColorProjectionFrame* framePtr = frameSpan)
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
                                framePtr,
                                frameCount,
                                targetVertexPtr,
                                targetIndexPtr,
                                ref vertexCount,
                                ref indexCount,
                                uvBorder,
                                uvMax,
                                preferCameraDistance);
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
                            framePtr,
                            frameCount,
                            targetVertexPtr,
                            targetIndexPtr,
                            ref vertexCount,
                            ref indexCount,
                            uvBorder,
                            uvMax,
                            preferCameraDistance);
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

        private static unsafe void EmitProjectedTriangle(
            VertexData a,
            VertexData b,
            VertexData c,
            ColorProjectionFrame* frames,
            int frameCount,
            VertexData* vertices,
            uint* indices,
            ref int vertexCount,
            ref int indexCount,
            float uvBorder,
            float uvMax,
            bool preferCameraDistance)
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

            var center = (a.Pos + b.Pos + c.Pos) * 0.33333334f;

            for (var i = 0; i < frameCount; i++)
            {
                ref readonly var frame = ref frames[i];

                if (!TryProject(a.Pos, in frame, uvBorder, uvMax, out var uvA) ||
                    !TryProject(b.Pos, in frame, uvBorder, uvMax, out var uvB) ||
                    !TryProject(c.Pos, in frame, uvBorder, uvMax, out var uvC))
                    continue;

                var score = 0.0f;

                // Prefer closer capture camera, then slightly prefer projections near texture center.
                if (preferCameraDistance)
                {
                    var dx = center.X - frame.CameraPosition.X;
                    var dy = center.Y - frame.CameraPosition.Y;
                    var dz = center.Z - frame.CameraPosition.Z;

                    score += dx * dx + dy * dy + dz * dz;
                }

                var uvCenterX = (uvA.X + uvB.X + uvC.X) * 0.33333334f - 0.5f;
                var uvCenterY = (uvA.Y + uvB.Y + uvC.Y) * 0.33333334f - 0.5f;

                score += (uvCenterX * uvCenterX + uvCenterY * uvCenterY) * 0.05f;

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
        private static bool TryProject(
            Vector3 position,
            in ColorProjectionFrame frame,
            float uvBorder,
            float uvMax,
            out Vector2 uv)
        {
            var m = frame.ViewProj;

            var clipX = position.X * m.M11 + position.Y * m.M21 + position.Z * m.M31 + m.M41;
            var clipY = position.X * m.M12 + position.Y * m.M22 + position.Z * m.M32 + m.M42;
            var clipW = position.X * m.M14 + position.Y * m.M24 + position.Z * m.M34 + m.M44;

            if (clipW <= 0.0f)
            {
                uv = default;
                return false;
            }

            var invW = 1.0f / clipW;

            var x = clipX * invW * 0.5f + 0.5f;
            var y = 0.5f - clipY * invW * 0.5f;

            uv = new Vector2(x, y);

            return x >= uvBorder && x <= uvMax &&
                   y >= uvBorder && y <= uvMax;
        }

        public float UvBorder { get; set; }

        public bool PreferCameraDistance { get; set; }
    }
}