
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


        protected override void RenderContentWork(GlobalContent content)
        {
            var updateContext = _renderer.UpdateContext; 

            foreach (var shader in content.ShaderContents.OrderBy(a => a.Key.Priority))
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

                        bool updateGlobals = false;

                        if (_renderer.State.ActiveProgram != progInst.Program!.Handle)
                        {
                            progInst.Program!.Use();
                            _renderer.State.ActiveProgram = progInst.Program!.Handle;
                            updateGlobals = true;
                        }

                        progInst.UpdateUniforms(updateContext, updateGlobals);

                        _renderer.ConfigureCaps(draw.ProgramInstance!.Material!);

                        if (!WriteDepth)
                        {
                            _renderer.GL.DepthMask(false);
                            _renderer.State.WriteDepth = false;
                        }

                        draw.Draw!();
                    }

                    vHandler.Unbind();
                }
            }
        }

        public bool WriteDepth { get; set; }    
    }
}
