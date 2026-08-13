using System.Diagnostics;

namespace XrEngine
{
    public static class SkinVertexShader
    {
        public static void UpdateShaderModel(ShaderUpdateBuilder bld, bool useUniform = false)
        {
            if (bld.Context.Material == null || !bld.Context.Material!.HasSkin)
                return;

            var mesh = bld.Context.Model?.Feature<ISkinnedMesh>();

            if (useUniform)
                bld.SetUniform("uHasSkin", ctx => mesh != null ? 1 : 0);

            bld.LoadBufferArray(ctx =>
            {
                if (mesh == null)
                    return null;

                if (mesh.SkinMatricesVersion == ctx.CurrentBuffer!.Version)
                    return null;

                ctx.CurrentBuffer!.Version = mesh.SkinMatricesVersion;

                return mesh.SkinMatrices;

            }, BufferSlots.SkinMatrices, BufferStore.Model, BufferUsage.Uniforms);
        }
    }
}
