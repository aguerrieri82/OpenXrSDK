using OpenXr.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Glft
{
    public static partial class DracoDecoder
    {
        enum PontDataType
        {
            None = 0,
            Position = 0x1,
            Normal = 0x2,
            UV = 0x4,
            Color = 0x8
        };

        public unsafe struct VertexData
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 UV;
        }

        unsafe struct MeshDataNative
        {
            public uint* Indices;
            public ulong IndicesSize;
            public VertexData* Vertices;
            public ulong VerticesSize;
            public PontDataType Types;
            public IntPtr Mesh;
        }

        public struct MeshData
        {
            public uint[] Indices;

            public VertexData[] Vertices;

        }

        [LibraryImport("draco-native")]
        private static unsafe partial int DecodeBuffer(byte* buffer, ulong bufferSize, MeshDataNative* data);

        [LibraryImport("draco-native")]
        private static unsafe partial void ReadMesh(MeshDataNative* data);

        public unsafe static MeshData DecodeBuffer(byte[] buffer)
        {
            var data = new MeshDataNative();
            fixed (byte* pBuf = buffer)
            {
                DecodeBuffer(pBuf, (ulong)buffer.Length, &data);

                var vertices = new VertexData[data.VerticesSize];
                var indices = new uint[data.IndicesSize];
                fixed (VertexData* pVer = vertices)
                fixed (uint* pIdx = indices)
                {
                    data.Indices = pIdx;
                    data.Vertices = pVer;
                    ReadMesh(&data);

                    return new MeshData
                    {
                        Indices = indices,
                        Vertices = vertices,
                    };
                }
            }

        }
    }
}
