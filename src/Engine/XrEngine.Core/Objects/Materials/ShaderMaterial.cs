namespace XrEngine
{
    public abstract class ShaderMaterial : Material, IShaderHandler
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
                NotifyChanged(ObjectChangeType.Render);
            }
        }

        public virtual bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return false;
        }

        public virtual void UpdateShader(ShaderUpdateBuilder bld)
        {

        }
    }
}
