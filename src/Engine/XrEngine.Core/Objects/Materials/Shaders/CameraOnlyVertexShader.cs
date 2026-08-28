using System.Diagnostics;

namespace XrEngine
{

    public class CameraOnlyVertexShader : Shader, IShaderHandler
    {
        public bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return false;
        }

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            var stage = bld.Context.Stage;

            if (stage == UpdateShaderStage.Any || stage == UpdateShaderStage.Shader)
            {
                bld.LoadBuffer<CameraUniforms>((ctx, ref update) =>
                {
                    Debug.Assert(ctx.PassCamera != null);

                    update.Value = new CameraUniforms
                    {
                        ViewProj = ctx.PassCamera.ViewProjection,
                        Position = ctx.PassCamera.WorldPosition,
                        NearPlane = ctx.PassCamera.Near,
                        FarPlane = ctx.PassCamera.Far,
                    };

                    return true;

                }, UniformsSlots.Camera, BufferStore.Shader);
            }
        }
    }

}
