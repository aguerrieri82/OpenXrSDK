using System;
using System.Collections.Generic;
using System.Numerics;

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

        public int ImageIndex { get; }

        public Vector3 CameraPosition { get; }

        public Matrix4x4 ViewProj { get; }
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

        public void Project(Geometry3D geometry, IReadOnlyList<ColorProjectionFrame> frames)
        {
            var sourceVertices = geometry.Vertices;

            if (sourceVertices == null || sourceVertices.Length < 3 || frames.Count == 0)
                return;

            var sourceIndices = geometry.Indices;

            var vertices = new List<VertexData>(sourceVertices.Length);
            var indices = new List<uint>(sourceIndices?.Length ?? sourceVertices.Length);

            if (sourceIndices != null && sourceIndices.Length >= 3)
            {
                for (var i = 0; i + 2 < sourceIndices.Length; i += 3)
                {
                    var ia = (int)sourceIndices[i + 0];
                    var ib = (int)sourceIndices[i + 1];
                    var ic = (int)sourceIndices[i + 2];

                    if ((uint)ia >= sourceVertices.Length ||
                        (uint)ib >= sourceVertices.Length ||
                        (uint)ic >= sourceVertices.Length)
                        continue;

                    EmitProjectedTriangle(
                        sourceVertices[ia],
                        sourceVertices[ib],
                        sourceVertices[ic],
                        frames,
                        vertices,
                        indices);
                }
            }
            else
            {
                for (var i = 0; i + 2 < sourceVertices.Length; i += 3)
                {
                    EmitProjectedTriangle(
                        sourceVertices[i + 0],
                        sourceVertices[i + 1],
                        sourceVertices[i + 2],
                        frames,
                        vertices,
                        indices);
                }
            }

            geometry.Vertices = vertices.ToArray();
            geometry.Indices = indices.ToArray();

            geometry.ActiveComponents |=
                VertexComponent.UV0 |
                VertexComponent.UV1 |
                VertexComponent.Tangent;
        }

        private void EmitProjectedTriangle(
            VertexData a,
            VertexData b,
            VertexData c,
            IReadOnlyList<ColorProjectionFrame> frames,
            List<VertexData> vertices,
            List<uint> indices)
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

            for (var i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];

                if (!TryProject(a.Pos, frame, out var uvA) ||
                    !TryProject(b.Pos, frame, out var uvB) ||
                    !TryProject(c.Pos, frame, out var uvC))
                    continue;

                var score = GetScore(a.Pos, b.Pos, c.Pos, uvA, uvB, uvC, frame);

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

            var start = (uint)vertices.Count;

            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);

            indices.Add(start + 0);
            indices.Add(start + 1);
            indices.Add(start + 2);
        }

        private static void ApplyProjection(
            ref VertexData vertex,
            int image0,
            Vector2 uv0,
            int image1,
            Vector2 uv1)
        {
            vertex.UV = uv0;

            if (image1 >= 0)
                vertex.UV1 = uv1;
            else
                vertex.UV1 = uv0;

            vertex.Tangent = new Vector4(
                image0,
                image1,
                0.0f,
                0.0f);
        }

        private bool TryProject(Vector3 position, ColorProjectionFrame frame, out Vector2 uv)
        {
            var clip = Vector4.Transform(new Vector4(position, 1.0f), frame.ViewProj);

            if (clip.W <= 0.0f)
            {
                uv = default;
                return false;
            }

            var invW = 1.0f / clip.W;

            uv = new Vector2(
                clip.X * invW * 0.5f + 0.5f,
                clip.Y * invW * 0.5f + 0.5f);

            uv.Y = 1.0f - uv.Y;

            return uv.X >= UvBorder && uv.X <= 1.0f - UvBorder &&
                   uv.Y >= UvBorder && uv.Y <= 1.0f - UvBorder;
        }

        private float GetScore(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector2 uvA,
            Vector2 uvB,
            Vector2 uvC,
            ColorProjectionFrame frame)
        {
            var score = 0.0f;

            if (PreferCameraDistance)
            {
                var center = (a + b + c) / 3.0f;
                score += Vector3.DistanceSquared(center, frame.CameraPosition);
            }

            var uvCenter = (uvA + uvB + uvC) / 3.0f;
            var centered = uvCenter - new Vector2(0.5f, 0.5f);

            score += centered.LengthSquared() * 0.05f;

            return score;
        }

        public float UvBorder { get; set; }

        public bool PreferCameraDistance { get; set; }
    }
}