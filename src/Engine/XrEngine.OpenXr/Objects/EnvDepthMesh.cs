using Common.Interop;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.OpenXr
{
    public class EnvDepthMesh : TriangleMesh
    {
        private IMemoryBuffer<byte>[]? _buffers;

        public EnvDepthMesh(Size2I gridSize)
        {
            Geometry = new Grid3D(gridSize);

            Materials.Add(new EnvDepthMaterial());

            Flags |= EngineObjectFlags.NoFrustumCulling;
        }


        public unsafe TriangleMesh? Freeze(Matrix4x4 colorViewProj, int eye = 0)
        {
            var mat = (EnvDepthMaterial)Materials[0];

            if (mat.LastTexture == null)
                return null;

            _buffers ??= [
                MemoryBuffer.Create<byte>(16),
                MemoryBuffer.Create<byte>(16)];

            OpenGLRender.Current!.ReadTexture(mat.LastTexture, TextureFormat.GrayInt16, 0, 0, _buffers);

            var size = ((Grid3D)Geometry!).Size;

            using var pTex = _buffers[eye].MemoryLock();

            var geoEye = CreateDepthColorGrid((ushort*)pTex.Data,
                (int)mat.LastTexture.Width, (int)mat.LastTexture.Height,
                (int)size.Width, (int)size.Height,
                mat.DepthCamera.Eyes![eye].ViewProjInv,
                colorViewProj
            );

            return new TriangleMesh(geoEye);
        }


        protected unsafe Geometry3D CreateDepthColorGrid(
            ushort* depth,
            int depthWidth,
            int depthHeight,
            int gridWidth,
            int gridHeight,
            Matrix4x4 depthViewProjInv,
            Matrix4x4 colorViewProj)
        {
            var vertices = new VertexData[gridWidth * gridHeight];
            var valid = new bool[gridWidth * gridHeight];
            var indices = new List<uint>((gridWidth - 1) * (gridHeight - 1) * 6);

            var invGridW = 1.0f / (gridWidth - 1);
            var invGridH = 1.0f / (gridHeight - 1);

            var maxDepthX = depthWidth - 1;
            var maxDepthY = depthHeight - 1;

            static bool KeepTriangle(uint i0, uint i1, uint i2, VertexData[] vertices, bool[] valid)
            {
                const float maxWorldEdge = 0.10f;

                if (!valid[i0] || !valid[i1] || !valid[i2])
                    return false;

                var p0 = vertices[i0].Pos;
                var p1 = vertices[i1].Pos;
                var p2 = vertices[i2].Pos;

                if (Vector3.Distance(p0, p1) > maxWorldEdge)
                    return false;

                if (Vector3.Distance(p1, p2) > maxWorldEdge)
                    return false;

                if (Vector3.Distance(p2, p0) > maxWorldEdge)
                    return false;

                return true;
            }

            for (var y = 0; y < gridHeight; y++)
            {
                var uvY = y * invGridH;
                var dy = (int)MathF.Round(uvY * maxDepthY);

                for (var x = 0; x < gridWidth; x++)
                {
                    var uvX = x * invGridW;
                    var dx = (int)MathF.Round(uvX * maxDepthX);

                    var index = y * gridWidth + x;
                    var rawD = depth[dy * depthWidth + dx];

                    if (rawD == 0 || rawD == ushort.MaxValue)
                    {
                        valid[index] = false;
                        continue;
                    }

                    var d = rawD / (float)ushort.MaxValue;

                    var clip = new Vector4(
                        uvX * 2.0f - 1.0f,
                        uvY * 2.0f - 1.0f,
                        d * 2.0f - 1.0f,
                        1.0f
                    );

                    var world4 = Vector4.Transform(clip, depthViewProjInv);

                    if (world4.W == 0)
                    {
                        valid[index] = false;
                        continue;
                    }

                    var invW = 1.0f / world4.W;

                    var world = new Vector3(
                        world4.X * invW,
                        world4.Y * invW,
                        world4.Z * invW
                    );

                    var colorClip = Vector4.Transform(new Vector4(world, 1.0f), colorViewProj);

                    if (colorClip.W == 0)
                    {
                        valid[index] = false;
                        continue;
                    }

                    var invColorW = 1.0f / colorClip.W;

                    var colorUv = new Vector2(
                        colorClip.X * invColorW * 0.5f + 0.5f,
                        colorClip.Y * invColorW * 0.5f + 0.5f
                    );

                    colorUv.Y = 1 - colorUv.Y;

                    vertices[index] = new VertexData
                    {
                        Pos = world,
                        UV = colorUv
                    };

                    valid[index] = true;
                }
            }

            for (var y = 0; y < gridHeight - 1; y++)
            {
                var row0 = y * gridWidth;
                var row1 = row0 + gridWidth;

                for (var x = 0; x < gridWidth - 1; x++)
                {
                    var i0 = (uint)(row0 + x);
                    var i1 = i0 + 1;
                    var i2 = (uint)(row1 + x);
                    var i3 = i2 + 1;

                    if (KeepTriangle(i0, i1, i2, vertices, valid))
                    {
                        indices.Add(i0);
                        indices.Add(i1);
                        indices.Add(i2);
                    }

                    if (KeepTriangle(i1, i3, i2, vertices, valid))
                    {
                        indices.Add(i1);
                        indices.Add(i3);
                        indices.Add(i2);
                    }
                }
            }

            var result = new Geometry3D
            {
                Vertices = vertices,
                Indices = indices.ToArray(),
                ActiveComponents = VertexComponent.Position | VertexComponent.UV0 | VertexComponent.Normal
            };

            result.ComputeNormals();

            return result;
        }
    }
}