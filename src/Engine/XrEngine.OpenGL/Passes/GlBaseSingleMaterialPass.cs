﻿using System.Diagnostics;

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
            var curStage = ctx.Stage;

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

            var updateContext = _renderer.UpdateContext;

            updateContext.UseInstanceDraw = false;

            bool firstUpdate = true;

            foreach (var shader in layer.Content.Contents)
            {
                var shaderContent = shader.Value;

                foreach (var material in shaderContent.Contents)
                {
                    var matContent = material.Value;

                    if (matContent.IsHidden || !material.Key.WriteDepth)
                        continue;

                    updateContext.Stage = UpdateShaderStage.Material;

                    var drawMaterial = matContent.ProgramInstance!.Material;

                    var upRes = UpdateProgram(updateContext, drawMaterial);

                    if (upRes == UpdateProgramResult.Skip)
                        continue;

                    if (firstUpdate || upRes == UpdateProgramResult.Changed)
                    {
                        _programInstance.UpdateUniforms(updateContext, upRes == UpdateProgramResult.Changed);
                        _programInstance.UpdateBuffers(updateContext);
                        firstUpdate = false;
                    }

                    foreach (var vertex in matContent.Contents)
                    {
                        var verContent = vertex.Value;
                        if (verContent.IsHidden)
                            continue;

                        var vHandler = verContent.VertexHandler!;

                        updateContext.ActiveComponents = verContent.ActiveComponents;

                        vHandler.Bind();

                        updateContext.Stage = UpdateShaderStage.Model;

                        if (_useInstanceDraw && verContent.Draw != null && verContent.Contents.Any(CanDraw))
                            verContent.Draw();
                        else
                        {
                            foreach (var draw in verContent.Contents)
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

            var updateContext = _renderer.UpdateContext;

            updateContext.UseInstanceDraw = false;

            foreach (var shader in layer.Content.ShaderContentsSorted!)
            {
                IEnumerable<VertexContent> vertices = shader.Value.ContentsSorted!;

                if (SortByCameraDistance)
                    vertices = vertices.OrderBy(a => a.AvgDistance);

                foreach (var vertex in vertices)
                {
                    var vHandler = vertex.VertexHandler!;

                    if (vHandler.NeedUpdate)
                        vHandler.Update();

                    updateContext.ActiveComponents = vertex.ActiveComponents;

                    vHandler.Bind();

                    foreach (var draw in vertex.Contents)
                    {
                        if (!CanDraw(draw))
                            continue;

                        updateContext.Model = draw.Object;

                        var drawMaterial = draw.ProgramInstance!.Material;

                        var upRes = UpdateProgram(updateContext, drawMaterial);

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
