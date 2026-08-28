using System.Diagnostics;

namespace XrEngine
{
    public static class SkinVertexShader
    {
        public static void UpdateShader(ShaderUpdateBuilder bld, bool isDynamic = false)
        {
            if (!isDynamic && (bld.Context.Material == null || !bld.Context.Material!.UseSkin))
                return;

            bld.LoadBufferArray(ctx =>
            {
                var mesh = bld.Context.Model?.Feature<ISkinnedMesh>();

                Debug.Assert(mesh != null);

                if (mesh.SkinMatricesVersion == ctx.CurrentBuffer!.Version)
                    return null;

                ctx.CurrentBuffer!.Version = mesh.SkinMatricesVersion;

                return mesh.SkinMatrices;

            }, BufferSlots.SkinMatrices, BufferStore.Model, BufferUsage.Uniforms, ctx => ctx.Material!.UseSkin);
        }
    }
}
