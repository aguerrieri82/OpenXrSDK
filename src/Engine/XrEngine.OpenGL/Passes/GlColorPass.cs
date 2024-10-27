#if GLES
using Silk.NET.OpenGLES;
#else
using Microsoft.VisualBasic;
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

        protected override bool BeginRender(Camera camera)
        {
            _renderer.RenderTarget!.Begin(camera, _renderer.UpdateContext.ViewSize);
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

        public override void RenderLayer(GlLayer layer)
        {
            _renderer.GL.PushDebugGroup(DebugSource.DebugSourceApplication, 0, unchecked((uint)-1), $"Begin layer {layer.Type}");

            var updateContext = _renderer.UpdateContext;

            var useDepthPass = _renderer.Options.UseDepthPass;

            foreach (var shader in layer.Content.ShaderContents.OrderBy(a => a.Key.Priority))
            {
                var progGlobal = shader.Value!.ProgramGlobal;

                updateContext.Shader = shader.Key;

                progGlobal!.UpdateProgram(updateContext, _renderer.RenderTarget as IShaderHandler);

                IEnumerable<VertexContent> vertices = shader.Value.Contents.Values;

                if (_renderer.Options.SortByCameraDistance)
                    vertices = vertices.OrderBy(a => a.RenderPriority).ThenBy(a => a.AvgDistance);
                else
                    vertices = vertices.OrderBy(a => a.RenderPriority);

                foreach (var vertex in vertices)
                {
                    if (vertex.IsHidden)
                        continue;

                    if (useDepthPass && vertex.Contents.All(a => a.Query != null && a.Query.GetResult() == 0))
                        continue;

                    var vHandler = vertex.VertexHandler!;

                    updateContext.ActiveComponents = vertex.ActiveComponents;

                    vHandler.Bind();

                    foreach (var draw in vertex.Contents)
                    {
                        if (draw.IsHidden)
                            continue;

                        if (draw.Query != null)
                        {
                            var passed = draw.Query.GetResult();
                            if (passed == 0)
                                continue;
                        }

                        var progInst = draw.ProgramInstance!;

                        updateContext.Model = draw.Object;

                        progInst.UpdateProgram(updateContext);

                        bool programChanged = updateContext.ProgramInstanceId != progInst.Program!.Handle;

                        updateContext.ProgramInstanceId = progInst.Program!.Handle;

                        _renderer.State.SetActiveProgram(progInst.Program!.Handle);

                        progInst.UpdateUniforms(updateContext, programChanged);

                        progInst.UpdateBuffers(updateContext);

                        _renderer.ConfigureCaps(draw.ProgramInstance!.Material!);

                        if (!WriteDepth)
                            _renderer.State.SetWriteDepth(false);

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
