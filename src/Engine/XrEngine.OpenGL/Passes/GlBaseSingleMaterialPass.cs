using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public abstract class GlBaseSingleMaterialPass : GlBaseRenderPass
    {
        protected GlProgramInstance? _progInstBase;
        protected GlProgramInstance? _progInstMulti;
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
            _progInstBase = GetProgramInstance(CreateMaterial());

            if (_useInstanceDraw)
                _progInstMulti = GetProgramInstance(CreateMaterial());
        }

        protected virtual void Draw(DrawContent draw)
        {
            draw.Draw!();
        }

        protected override bool BeginRender(GlUpdateContext ctx)
        {
            Debug.Assert(_progInstBase != null);

            ctx.Stage = UpdateShaderStage.Shader;

            UseProgram(_progInstBase, false);

            return true;
        }

        protected void UpdateMaterial(GlProgramInstance instance, UpdateShaderContext ctx)
        {
            var curStage = ctx.Stage;

            ctx.Stage = UpdateShaderStage.Material;

            instance.UpdateUniforms(ctx, false);
            instance.UpdateBuffers(ctx);

            ctx.Stage = curStage;
        }

        protected virtual bool CanDraw(Material drawMaterial)
        {
            if (!drawMaterial.WriteDepth)
                return false;
            return true;
        }

        protected virtual UpdateProgramResult UpdateProgram(GlProgramInstance instance, UpdateShaderContext ctx, Material drawMaterial)
        {
            if (!CanDraw(drawMaterial))
                return UpdateProgramResult.Skip;

            if (instance.UpdateProgram(ctx))
                return UpdateProgramResult.Changed;

            return UpdateProgramResult.Unchanged;
        }

        protected virtual UpdateProgramResult UpdateProgram(GlProgramInstance instance, UpdateShaderContext ctx, Object3D model)
        {
            return UpdateProgramResult.Unchanged;
        }

        public override void RenderLayer(GlLayer layer)
        {

            var updateContext = _renderer.UpdateContext;

            updateContext.UseInstanceDraw = false;

            var firstUpdate = true;

            foreach (var shader in layer.Content.Contents)
            {
                var shaderContent = shader.Value;

                foreach (var material in shaderContent.Contents)
                {
                    var matContent = material.Value;

                    if (matContent.IsHidden)
                        continue;
                    /*
                    var curInst = matContent.UseInstanceDraw && _useInstanceDraw ? 
                        _progInstMulti : _progInstBase;
                    */

                    var curInst = _progInstBase;

                    Debug.Assert(curInst != null);

                    updateContext.UseInstanceDraw = matContent.UseInstanceDraw;

                    updateContext.Stage = UpdateShaderStage.Material;

                    var drawMaterial = matContent.ProgramInstance!.Material;

                    var upRes = UpdateProgram(curInst, updateContext, drawMaterial);

                    if (upRes == UpdateProgramResult.Skip)
                        continue;

                    _renderer.ConfigureCaps(curInst.Material);

                    if (firstUpdate || upRes == UpdateProgramResult.Changed)
                    {
                        curInst.UpdateUniforms(updateContext, upRes == UpdateProgramResult.Changed);
                        curInst.UpdateBuffers(updateContext);
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
                        {
                            verContent.Draw();
                        }
                        else
                        {
                            foreach (var draw in verContent.Contents)
                            {
                                if (!CanDraw(draw))
                                    continue;

                                updateContext.Model = draw.Object;

                                upRes = UpdateProgram(curInst, updateContext, draw.Object!);

                                if (upRes == UpdateProgramResult.Skip)
                                    continue;

                                curInst.UpdateModel(updateContext);

                                Draw(draw);
                            }
                        }

                        vHandler.Unbind();
                    }

                }
            }
            _renderer.State.BindVertexArray(0);
        }

        protected virtual bool CanDraw(DrawContent draw)
        {
            return !draw.IsHidden;
        }

        protected override void EndRender(GlUpdateContext ctx)
        {
            _renderer.State.SetActiveProgram(0);
            ctx.ProgramInstanceId = 0;
        }

        public override void Dispose()
        {
            _progInstBase?.Dispose();
            _progInstBase = null;

            _progInstMulti?.Dispose();
            _progInstMulti = null;
            base.Dispose();
        }

        protected bool SortByCameraDistance { get; set; }

    }
}
