using System.Diagnostics.CodeAnalysis;

namespace XrEngine
{

    public class ShaderMaterial : Material, IShaderHandler
    {
        [AllowNull]
        protected Shader _shader;
        protected long _lastLightVersion = -1;

        protected internal readonly ChangeTracker _tracker = new();

        protected ShaderMaterial()
        {
            WriteDepth = true;
            UseDepth = true;
            WriteColor = true;
            Morph = MorphMode.AllTargets;
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
            if (UseMorph)
                return MorphVertexShader.NeedUpdateShader(ctx);
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
            SkinVertexShader.UpdateShader(bld, true);
        }

        protected virtual void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("USE_SKIN", ctx => UseSkin, Skin == SkinMode.Dynamic);

            if (UseMorph)
                MorphVertexShader.UpdateShader(bld);
        }

        public Func<string, string?>? Resolver { get; set; }
    }
}
