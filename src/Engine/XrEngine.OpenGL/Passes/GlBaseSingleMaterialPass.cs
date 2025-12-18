using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public abstract class GlBaseSingleMaterialPass : GlBaseRenderPass
    {
        protected GlProgramInstance? _programInstance;
        protected bool _useInstanceDraw;

        protected enum UpdateProgramResult
        {
            Unchanged,
            Changed,
            Skip
        }

        public GlBaseSingleMaterialPass(OpenGLRender renderer)
            : base(renderer)
        {
            SortByCameraDistance = _renderer.Options.SortByCameraDistance;
        }

        protected abstract ShaderMaterial CreateMaterial();

        protected override void Initialize()
        {
            _programInstance = CreateProgram(CreateMaterial());
        }

        protected virtual void Draw(DrawContent draw)
        {
            draw.Draw!();
        }

        protected override bool BeginRender(Camera camera)
        {
            Debug.Assert(_programInstance != null);

            _renderer.UpdateContext.Stage = UpdateShaderStage.Shader;

            UseProgram(_programInstance, false);

            return true;
        }

        protected void UpdateMaterial(UpdateShaderContext ctx)
        {
            UpdateShaderStage curStage = ctx.Stage;

            ctx.Stage = UpdateShaderStage.Material;

            _programInstance!.UpdateUniforms(ctx, false);
            _programInstance!.UpdateBuffers(ctx);

            ctx.Stage = curStage;
        }

        protected virtual UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Material drawMaterial)
        {
            if (_programInstance!.UpdateProgram(updateContext))
                return UpdateProgramResult.Changed;

            return UpdateProgramResult.Unchanged;
        }


        protected virtual UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Object3D model)
        {
            return UpdateProgramResult.Unchanged;
        }


        public override void RenderLayer(GlLayerV2 layer)
        {
            Debug.Assert(_programInstance != null);

            GlUpdateContext updateContext = _renderer.UpdateContext;

            updateContext.UseInstanceDraw = false;

            bool firstUpdate = true;

            foreach (KeyValuePair<Shader, ShaderContentV2> shader in layer.Content.Contents)
            {
                ShaderContentV2 shaderContent = shader.Value;

                foreach (KeyValuePair<Material, MaterialContentV2> material in shaderContent.Contents)
                {
                    MaterialContentV2 matContent = material.Value;

                    if (matContent.IsHidden || !material.Key.WriteDepth)
                        continue;

                    updateContext.Stage = UpdateShaderStage.Material;

                    ShaderMaterial drawMaterial = matContent.ProgramInstance!.Material;

                    UpdateProgramResult upRes = UpdateProgram(updateContext, drawMaterial);

                    if (upRes == UpdateProgramResult.Skip)
                        continue;

                    if (firstUpdate || upRes == UpdateProgramResult.Changed)
                    {
                        _programInstance.UpdateUniforms(updateContext, upRes == UpdateProgramResult.Changed);
                        _programInstance.UpdateBuffers(updateContext);
                        firstUpdate = false;
                    }

                    foreach (KeyValuePair<EngineObject, VertexContentV2> vertex in matContent.Contents)
                    {
                        VertexContentV2 verContent = vertex.Value;
                        if (verContent.IsHidden)
                            continue;

                        GlVertexSourceHandle vHandler = verContent.VertexHandler!;

                        updateContext.ActiveComponents = verContent.ActiveComponents;

                        vHandler.Bind();

                        updateContext.Stage = UpdateShaderStage.Model;

                        if (_useInstanceDraw && verContent.Draw != null && verContent.Contents.Any(CanDraw))
                            verContent.Draw();
                        else
                        {
                            foreach (DrawContent draw in verContent.Contents)
                            {
                                if (!CanDraw(draw))
                                    continue;

                                updateContext.Model = draw.Object;

                                upRes = UpdateProgram(updateContext, draw.Object!);

                                if (upRes == UpdateProgramResult.Skip)
                                    continue;

                                _programInstance.UpdateModel(updateContext);

                                Draw(draw);
                            }
                        }

                        //vHandler.Unbind();
                    }


                }
            }
            _renderer.State.BindVertexArray(0);
        }


        public override void RenderLayer(GlLayer layer)
        {
            Debug.Assert(_programInstance != null);

            GlUpdateContext updateContext = _renderer.UpdateContext;

            updateContext.UseInstanceDraw = false;

            foreach (KeyValuePair<Shader, ShaderContent> shader in layer.Content.ShaderContentsSorted!)
            {
                IEnumerable<VertexContent> vertices = shader.Value.ContentsSorted!;

                if (SortByCameraDistance)
                    vertices = vertices.OrderBy(a => a.AvgDistance);

                foreach (VertexContent vertex in vertices)
                {
                    GlVertexSourceHandle vHandler = vertex.VertexHandler!;

                    if (vHandler.NeedUpdate)
                        vHandler.Update();

                    updateContext.ActiveComponents = vertex.ActiveComponents;

                    vHandler.Bind();

                    foreach (DrawContent draw in vertex.Contents)
                    {
                        if (!CanDraw(draw))
                            continue;

                        updateContext.Model = draw.Object;

                        ShaderMaterial drawMaterial = draw.ProgramInstance!.Material;

                        UpdateProgramResult upRes = UpdateProgram(updateContext, drawMaterial);

                        if (upRes == UpdateProgramResult.Skip)
                            continue;

                        UpdateProgram(updateContext, draw.Object!);

                        _programInstance.UpdateUniforms(updateContext, upRes == UpdateProgramResult.Changed);
                        _programInstance.UpdateBuffers(updateContext);

                        Draw(draw);

                    }

                    vHandler.Unbind();
                }
            }
        }

        protected virtual bool CanDraw(DrawContent draw)
        {
            return !draw.IsHidden;
        }

        protected override void EndRender()
        {
            _renderer.State.SetActiveProgram(0);
            _renderer.UpdateContext.ProgramInstanceId = 0;
        }

        public override void Dispose()
        {
            _programInstance?.Dispose();
            _programInstance = null;
            base.Dispose();
        }

        protected bool SortByCameraDistance { get; set; }

    }
}
