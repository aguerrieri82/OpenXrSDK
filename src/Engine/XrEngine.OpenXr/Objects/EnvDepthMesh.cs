using Common.Interop;
using System.Numerics;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.OpenXr
{
    public class EnvDepthMesh : TriangleMesh
    {
        private IMemoryBuffer<byte>[]? _buffers;
        private DepthGeometryGenerator? _generator;

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

            _generator ??= new DepthGeometryGenerator((int)size.Width, (int)size.Height);

            var geoEye = _generator.CreateGeometry((ushort*)pTex.Data,
                (int)mat.LastTexture.Width, (int)mat.LastTexture.Height,
                mat.DepthCamera.Eyes![eye].ViewProjInv,
                colorViewProj
            );

            return new TriangleMesh(geoEye);
        }

        public EnvDepthMaterial Material => (EnvDepthMaterial)Materials[0];
    }
}