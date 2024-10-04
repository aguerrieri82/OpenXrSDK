#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

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

            if (_renderer.Options.UseDepthPass)
            {
                _renderer.State.SetWriteColor(true);
                _renderer.GL.Clear(ClearBufferMask.ColorBufferBit);
                _renderer.GL.DepthFunc(DepthFunction.Lequal);
            }
            else
                _renderer.Clear(_renderer.UpdateContext.Camera!.BackgroundColor);

            return true;
        }

        protected override void EndRender()
        {
            // _renderer.State.SetActiveProgram(0);
            // _renderer.RenderTarget!.End(false);
        }

        protected override void RenderLayer(GlLayer layer)
        {

            _renderer.GL.PushDebugGroup(DebugSource.DebugSourceApplication, 0, unchecked((uint)-1), $"Begin layer {layer.Type}");

            var updateContext = _renderer.UpdateContext;

            var useDepthPass = _renderer.Options.UseDepthPass;  

            foreach (var shader in layer.Content.ShaderContents.OrderBy(a => a.Key.Priority))
            {
                var progGlobal = shader.Value!.ProgramGlobal;

                updateContext.Shader = shader.Key;

                progGlobal!.UpdateProgram(updateContext, _renderer.RenderTarget as IShaderHandler);

                foreach (var vertex in shader.Value.Contents)
                {
                    if (useDepthPass && vertex.Value.Contents.All(a=> a.IsHidden || (a.Query != null && a.Query.GetResult() == 0)))
                        continue;

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

                        if (!useDepthPass && draw.Object is TriangleMesh mesh && _renderer.Options.FrustumCulling)
                        {
                            if (!mesh.WorldBounds.IntersectFrustum(updateContext.FrustumPlanes!))
                                continue;
                        }

                        var progInst = draw.ProgramInstance!;

                        if (!progInst.Material!.IsEnabled)
                            continue;

                        updateContext.Model = draw.Object;

                        progInst.UpdateProgram(updateContext);

                        bool programChanged = updateContext.ProgramInstanceId != progInst.Program!.Handle;

                        updateContext.ProgramInstanceId = progInst.Program!.Handle;

                        _renderer.State.SetActiveProgram(progInst.Program!.Handle);

                        progInst.UpdateUniforms(updateContext, programChanged);

                        _renderer.ConfigureCaps(draw.ProgramInstance!.Material!);

                        _renderer.State.SetWriteDepth(WriteDepth);

                        draw.Draw!();
                    }

                    vHandler.Unbind();
                }
            }

            _renderer.GL.PopDebugGroup();
        }

        public bool WriteDepth { get; set; }
    }
}
