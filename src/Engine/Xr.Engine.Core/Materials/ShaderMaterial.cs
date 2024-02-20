namespace OpenXr.Engine
{
    public class ShaderMaterial : Material
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

        public virtual void ExtractFeatures(IFeatureList features)
        {

        }

        public virtual void UpdateUniforms(IUniformProvider obj)
        {

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


    }
}
