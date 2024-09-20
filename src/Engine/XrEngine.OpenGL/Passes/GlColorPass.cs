
using XrMath;

namespace XrEngine.OpenGL
{
    public class GlColorPass : GlBaseRenderPass
    {
        public GlColorPass(OpenGLRender renderer)
            : base(renderer)    
        {
            WriteDepth = true;  
        }

        protected override bool BeginRender()
        {
            _renderer.RenderTarget!.Begin();
            _renderer.State.SetView(_renderer.RenderView);
            _renderer.Clear(_renderer.UpdateContext.Camera!.BackgroundColor);
            return true;
        }

        protected override void EndRender()
        {
            _renderer.State.SetActiveProgram(0);
            _renderer.RenderTarget!.End();
        }

        protected override void RenderLayer(GlLayer layer)
        {
            var updateContext = _renderer.UpdateContext; 

            foreach (var shader in layer.Content.ShaderContents.OrderBy(a => a.Key.Priority))
            {
                var progGlobal = shader.Value!.ProgramGlobal;

                updateContext.Shader = shader.Key;

                progGlobal!.UpdateProgram(updateContext, _renderer.RenderTarget as IShaderHandler);

                foreach (var vertex in shader.Value.Contents)
                {
                    var vHandler = vertex.Value.VertexHandler!;

                    if (vHandler.NeedUpdate)
                        vHandler.Update();

                    updateContext.ActiveComponents = vertex.Value.ActiveComponents;

                    vHandler.Bind();

                    foreach (var draw in vertex.Value.Contents)
                    {
                        if (draw.IsHidden)
                            continue;

                        if (draw.Query != null)
                        {
                            var passed = draw.Query.GetResult();
                            if (passed == 0)
                                continue;
                        }
                        else
                        {
                            if (draw.Object is TriangleMesh mesh && _renderer.Options.FrustumCulling)
                            {
                                if (!mesh.WorldBounds.IntersectFrustum(updateContext.FrustumPlanes!))
                                    continue;
                            }
                        }

                        var progInst = draw.ProgramInstance!;

                        if (!progInst.Material!.IsEnabled)
                            continue;

                        updateContext.Model = draw.Object;

                        progInst.UpdateProgram(updateContext);

                        updateContext.ProgramInstanceId = progInst.Program!.Handle;

                        bool updateGlobals = _renderer.State.SetActiveProgram(progInst.Program!.Handle);

                        progInst.UpdateUniforms(updateContext, updateGlobals);

                        _renderer.ConfigureCaps(draw.ProgramInstance!.Material!);

                        _renderer.State.SetWriteDepth(WriteDepth);

                        draw.Draw!();
                    }

                    vHandler.Unbind();
                }
            }
        }

        public bool WriteDepth { get; set; }    
    }
}
