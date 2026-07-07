namespace XrEngine
{
    public static class SkinVertexShader
    {
        public static void UpdateShaderModel(ShaderUpdateBuilder bld, bool useUniform = false)
        {

            if (useUniform)
                bld.SetUniform("uHasSkin", ctx => ctx.Model is ISkinnedMesh ? 1 : 0);
            else
            {
                if (bld.Context.Model is not ISkinnedMesh mesh)
                    return;
                
                bld.AddFeature("HAS_SKIN");
            }

            bld.LoadBufferArray(ctx =>
            {
                if (bld.Context.Model is not ISkinnedMesh mesh)
                    return null;

                if (mesh.SkinVersion == ctx.CurrentBuffer!.Version)
                    return null;

                ctx.CurrentBuffer!.Version = mesh.SkinVersion;

                return mesh.Skin;

            }, BufferSlots.Skin, BufferStore.Model, BufferUsage.SSbo);


            bld.LoadBufferArray(ctx =>
            {
                if (bld.Context.Model is not ISkinnedMesh mesh)
                    return null;

                if (mesh.SkinMatricesVersion == ctx.CurrentBuffer!.Version)
                    return null;

                ctx.CurrentBuffer!.Version = mesh.SkinMatricesVersion;

                return mesh.SkinMatrices;

            }, BufferSlots.SkinMatrices, BufferStore.Model, BufferUsage.SSbo);
        }
    }
}
