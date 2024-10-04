using Silk.NET.OpenGL;
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

            if (_renderer.Options.SortByCameraDistance)
                layer.ComputeDistance(updateContext.Camera!);

            foreach (var shader in layer.Content.ShaderContents)
            {
                IEnumerable<VertexContent> vertices = shader.Value.Contents.Values;

                if (_renderer.Options.SortByCameraDistance)
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
