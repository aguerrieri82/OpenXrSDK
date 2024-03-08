using System.Runtime.InteropServices;

namespace XrEngine.Gltf
{
    public static partial class DracoDecoder
    {
        public enum AttributeType : byte
        {
            None = 0,
            Position = 1,
            Normal = 2,
            UV = 3,
            Color = 4,
            Other = 5
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct MeshData
        {
            public uint IndicesSize;
            public uint VerticesSize;
            public uint AttributeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public AttributeType[] Attributes;
            public IntPtr Mesh;
        }

        [DllImport("draco-native")]
        private static unsafe extern int DecodeBuffer(byte* buffer, ulong bufferSize, ref MeshData data);

        [DllImport("draco-native")]
        private static unsafe extern void ReadAttribute(IntPtr mesh, int index, void* buffer, int itemSize, int itemCount);

        [DllImport("draco-native")]
        private static unsafe extern void ReadIndices(IntPtr mesh, uint* buffer, int itemCount);

        [DllImport("draco-native")]
        public static unsafe extern void DisposeMesh(IntPtr mesh);


        public unsafe static uint[] ReadIndices(MeshData data)
        {
            var buffer = new uint[data.IndicesSize];
            fixed (uint* pBuf = buffer)
                ReadIndices((IntPtr)data.Mesh, pBuf, (int)data.IndicesSize);
            return buffer;
        }

        public unsafe static T[] ReadAttribute<T>(MeshData data, int index) where T : unmanaged
        {
            var buffer = new T[data.VerticesSize];
            fixed (T* pBuf = buffer)
                ReadAttribute((IntPtr)data.Mesh, index, pBuf, sizeof(T), (int)data.VerticesSize);
            return buffer;
        }

        public unsafe static MeshData DecodeBuffer(byte[] buffer)
        {
            return DecodeBuffer(buffer, 0, buffer.Length);
        }

        public unsafe static MeshData DecodeBuffer(byte[] buffer, int offset, int size)
        {
            var data = new MeshData();
            fixed (byte* pBuf = buffer)
            {
                DecodeBuffer(pBuf + offset, (ulong)size, ref data);
                return data;
            }
        }
    }
}
