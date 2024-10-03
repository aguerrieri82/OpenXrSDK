using XrMath;

namespace XrEngine.OpenGL
{
    public abstract class GlBaseSingleMaterialPass : GlBaseRenderPass
    {
        protected GlProgramInstance? _programInstance;

        public GlBaseSingleMaterialPass(OpenGLRender renderer)
            : base(renderer)
        {
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

        protected override bool BeginRender()
        {
            UseProgram(_programInstance!, false);
            return true;
        }


        protected override void RenderLayer(GlLayer layer)
        {

            var updateContext = _renderer.UpdateContext;

            foreach (var shader in layer.Content.ShaderContents)
            {
                foreach (var vertex in shader.Value.Contents)
                {
                    var vHandler = vertex.Value.VertexHandler!;

                    if (vHandler.NeedUpdate)
                        vHandler.Update();

                    updateContext.ActiveComponents = vertex.Value.ActiveComponents;

                    vHandler.Bind();

                    foreach (var draw in vertex.Value.Contents)
                    {
                        if (draw.Object is TriangleMesh mesh && _renderer.Options.FrustumCulling)
                        {
                            draw.IsHidden = !mesh.WorldBounds.IntersectFrustum(updateContext.FrustumPlanes!);
                            if (draw.IsHidden)
                                continue;
                        }
                        else
                            draw.IsHidden = false;

                        var material = draw.ProgramInstance!.Material;

                        if (!material.WriteDepth)
                            continue;

                        updateContext.Model = draw.Object;

                        _programInstance!.UpdateUniforms(updateContext, false);

                        Draw(draw);

                    }

                    vHandler.Unbind();
                }
            }
        }

        protected override void EndRender()
        {
            _renderer.State.SetActiveProgram(0);
        }
    }
}
