namespace OpenXr.Engine
{
    public class ShaderMaterial : Material, IShaderHandler
    {
   
        protected Shader? _shader;

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

        public virtual void UpdateShader(UpdateShaderContext ctx, IUniformProvider up, IFeatureList fl)
        {

        }
    }
}
