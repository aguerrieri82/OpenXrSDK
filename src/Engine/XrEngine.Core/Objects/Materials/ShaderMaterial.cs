using System.Diagnostics.CodeAnalysis;

namespace XrEngine
{
    public class ShaderMaterial : Material, IShaderHandler
    {
        [AllowNull]
        protected Shader _shader;
        protected long _lastLightVersion = -1;

        protected bool _isSkinDynamic;


        protected ShaderMaterial()
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

        public Shader Shader
        {
            get => _shader ?? throw new NullReferenceException("Shader is not assigned");
            set
            {
                if (value == _shader)
                    return;
                _shader = value;
                NotifyChanged(ChangeType.Render);
            }
        }

        public override void Reload()
        {
            _shader?.NotifyChanged(ChangeType.Render);

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

            if (stage == UpdateShaderStage.Any || stage == UpdateShaderStage.Material || stage == UpdateShaderStage.Shader)
                UpdateShaderMaterial(bld);
        }

        protected virtual void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            SkinVertexShader.UpdateShaderModel(bld, true);
        }

        protected virtual void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("HAS_SKIN", ctx => HasSkin, _isSkinDynamic);
        }

        public bool HasSkin { get; set; }

        public Func<string, string?>? Resolver { get; set; }
    }
}
