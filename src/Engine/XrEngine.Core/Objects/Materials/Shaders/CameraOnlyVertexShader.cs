using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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
                bld.LoadBuffer((ctx) =>
                {
                    Debug.Assert(ctx.PassCamera != null);

                    var result = new CameraUniforms
                    {
                        ViewProj = ctx.PassCamera.ViewProjection,
                        Position = ctx.PassCamera.WorldPosition,
                        NearPlane = ctx.PassCamera.Near,
                        FarPlane = ctx.PassCamera.Far,
                    };

                    return (CameraUniforms?)result;

                }, 0, BufferStore.Shader);
            }
        }
    }

}
