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

        public override void Reload()
        {
            _shader?.NotifyChanged(ObjectChangeType.Material);
            base.Reload();
        }

        public virtual bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return false;
        }

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            var stage = bld.Context.Stage;

            if (stage == UpdateShaderStage.Any || stage == UpdateShaderStage.Model)
                UpdateShaderModel(bld);

            if (stage == UpdateShaderStage.Any || stage == UpdateShaderStage.Material)
                UpdateShaderMaterial(bld);
        }


        protected virtual void UpdateShaderModel(ShaderUpdateBuilder bld)
        {

        }

        protected virtual void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {

        }
    }
}
