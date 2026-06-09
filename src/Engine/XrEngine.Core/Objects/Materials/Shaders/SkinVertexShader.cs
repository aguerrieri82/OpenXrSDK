using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine
{
    public static class SkinVertexShader
    {
        public static void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            if (bld.Context.Model is not ISkinnedMesh mesh)
                return;

            bld.AddFeature("HAS_SKIN");

            bld.LoadBufferArray(ctx =>
            {
                if (mesh.SkinVersion == ctx.CurrentBuffer!.Version)
                    return null;

                ctx.CurrentBuffer!.Version = mesh.SkinVersion;

                return mesh.Skin;

            }, 18, BufferStore.Model, false);


            bld.LoadBufferArray(ctx =>
            {
                if (mesh.SkinMatricesVersion == ctx.CurrentBuffer!.Version)
                    return null;

                ctx.CurrentBuffer!.Version = mesh.SkinMatricesVersion;

                return mesh.SkinMatrices;

            }, 19, BufferStore.Model, false);
        }
    }
}
