namespace Xr.Engine
{
    public class ShaderMaterial : Material, IShaderHandler
    {

        protected Shader? _shader;
        protected long _lastLightVersion = -1;

        public ShaderMaterial()
        {
            WriteDepth = true;
            UseDepth = true;
            WriteColor = true;
        }

        public ShaderMaterial(Shader shader)
            : this()
        {
            _shader = shader;
        }

        public Shader? Shader
        {
            get => _shader;
            set
            {
                if (value == _shader)
                    return;
                _shader = value;
                NotifyChanged();
            }
        }

        public virtual bool NeedUpdateShader(UpdateShaderContext ctx, ShaderUpdate lastUpdate)
        {
            return lastUpdate.MaterialVersion != Version;
        }

        public virtual void UpdateShader(ShaderUpdateBuilder bld)
        {

        }
    }
}
