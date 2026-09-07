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
                bld.LoadCameraBuffer();
            }
        }
    }

}
