using Silk.NET.OpenGL;
using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public abstract class GlBaseSingleMaterialPass : GlBaseRenderPass
    {
        protected GlProgramInstance? _programInstance;

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
            UseProgram(_programInstance, false);
            return true;
        }


        protected virtual UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Material drawMaterial)
        {
            if (_programInstance!.UpdateProgram(updateContext))
                return UpdateProgramResult.Changed;

            return UpdateProgramResult.Unchanged;
        }

        public override void RenderLayer(GlLayer layer)
        {
            Debug.Assert(_programInstance != null);

            var updateContext = _renderer.UpdateContext;

            foreach (var shader in layer.Content.ShaderContents)
            {
                IEnumerable<VertexContent> vertices = shader.Value.Contents.Values;

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
                        if (draw.IsHidden)
                            continue;

                        if (!CanDraw(draw))
                            continue;

                        updateContext.Model = draw.Object;

                        var drawMaterial = draw.ProgramInstance!.Material;

                        var upRes = UpdateProgram(updateContext, drawMaterial);
                        if (upRes == UpdateProgramResult.Skip)
                            continue;

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
            return true;
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
