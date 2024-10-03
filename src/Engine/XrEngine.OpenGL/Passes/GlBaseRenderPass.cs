namespace XrEngine.OpenGL
{
    public abstract class GlBaseRenderPass : IGlRenderPass
    {
        protected readonly OpenGLRender _renderer;
        protected bool _isInit;

        public GlBaseRenderPass(OpenGLRender renderer)
        {
            _renderer = renderer;
            IsEnabled = true;
        }

        protected virtual void Initialize()
        {
        }

        protected virtual IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type != GlLayerType.CastShadow);
        }

        public virtual void Render()
        {
            if (!IsEnabled)
                return;

            if (!_isInit)
            {
                Initialize();
                _isInit = true;
            }

            if (!BeginRender())
                return;

            foreach (var layer in SelectLayers())
                RenderLayer(layer);

            EndRender();
        }

        protected virtual bool BeginRender()
        {
            return true;
        }

        protected virtual void EndRender()
        {

        }

        protected virtual IGlRenderTarget? GetRenderTarget()
        {
            return _renderer.RenderTarget;
        }

        protected GlProgramInstance CreateProgram(ShaderMaterial material)
        {
            var global = material.Shader!.GetGlResource(gl => new GlProgramGlobal(_renderer.GL, material.Shader!));
            return new GlProgramInstance(_renderer.GL, material, global);
        }

        protected void UseProgram(GlProgramInstance instance, bool updateUniforms)
        {
            var updateContext = _renderer.UpdateContext;

            updateContext.Shader = instance.Material.Shader;

            instance.Global!.UpdateProgram(updateContext, GetRenderTarget() as IShaderHandler);

            instance.UpdateProgram(updateContext);

            bool programChanged = updateContext.ProgramInstanceId != instance.Program!.Handle;

            updateContext.ProgramInstanceId = instance.Program!.Handle;

            instance.Program.Use();

            if (programChanged)
                instance.Global.UpdateUniforms(updateContext, instance.Program);

            _renderer.ConfigureCaps(instance.Material);

            if (updateUniforms)
                instance.UpdateUniforms(updateContext, false);
        }

        protected abstract void RenderLayer(GlLayer layer);

        public bool IsEnabled { get; set; }
    }
}
