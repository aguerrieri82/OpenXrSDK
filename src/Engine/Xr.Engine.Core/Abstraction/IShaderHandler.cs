namespace Xr.Engine
{


    public interface IShaderHandler
    {
        void UpdateShader(ShaderUpdateBuilder bld);

        bool NeedUpdateShader(UpdateShaderContext ctx, ShaderUpdate lastUpdate);
    }
}
