#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

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
            GetRenderTarget()!.Begin(camera);

            if (_renderer.Options.UseDepthPass)
            {
                _renderer.State.SetWriteColor(true);
                _gl.Clear(ClearBufferMask.ColorBufferBit);
                _gl.DepthFunc(DepthFunction.Lequal);
            }
            else
                _renderer.Clear(_renderer.UpdateContext.PassCamera!.BackgroundColor);

            return true;
        }

        protected override IEnumerable<GlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => (a.Type & GlLayerType.Color) == GlLayerType.Color ||
                                               (a.SceneLayer is DetachedLayer det));
        }

        protected override void EndRender()
        {
            // _renderer.State.SetActiveProgram(0);
            // _renderer.RenderTarget!.End(false);
        }

        protected virtual bool CanDraw(DrawContent draw)
        {
            if (draw.IsHidden)
                return false;

            if (draw.Query != null)
            {
                var passed = draw.Query.GetResult();
                if (passed == 0)
                    return false;
            }

            return true;
        }

        protected virtual void Draw(DrawContent draw)
        {
            draw.Draw!();

#if DEBUG
            var name = draw.Object!.Name;
            if (name != null)
                _gl.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, (uint)name.Length, name);
#endif

        }

        protected virtual bool UpdateProgram(UpdateShaderContext updateContext, GlProgramInstance progInst)
        {
            return progInst.UpdateProgram(updateContext);
        }

        public override void RenderLayer(GlLayer layer)
        {
            if (layer.SceneLayer != null && !layer.SceneLayer.IsVisible)
                return;

            _renderer.PushGroup($"Layer {layer.Name ?? layer.Type.ToString()}");

            var updateContext = _renderer.UpdateContext;

            var useDepthPass = _renderer.Options.UseDepthPass;

            var useOcclusion = _renderer.Options.UseOcclusionQuery;

            foreach (var shader in layer.Content.ShaderContentsSorted)
            {
                var progGlobal = shader.Value!.ProgramGlobal;

                updateContext.Shader = shader.Key;

                progGlobal!.UpdateProgram(updateContext, GetRenderTarget() as IShaderHandler);

                IEnumerable<VertexContent> vertices = shader.Value.ContentsSorted;

                if (_renderer.Options.SortByCameraDistance)
                    vertices = vertices.OrderBy(a => a.RenderPriority).ThenBy(a => a.AvgDistance);

                foreach (var vertex in vertices)
                {
                    if (vertex.IsHidden)
                        continue;

                    if (useOcclusion && vertex.Contents.All(a => a.Query != null && a.Query.GetResult() == 0))
                        continue;

                    var vHandler = vertex.VertexHandler!;

                    updateContext.ActiveComponents = vertex.ActiveComponents;

                    vHandler.Bind();

                    foreach (var draw in vertex.Contents)
                    {
                        if (!CanDraw(draw))
                            continue;

                        var progInst = draw.ProgramInstance!;

                        updateContext.Model = draw.Object;

                        UpdateProgram(updateContext, progInst);

                        var programChanged = updateContext.ProgramInstanceId != progInst.Program!.Handle;

                        updateContext.ProgramInstanceId = progInst.Program!.Handle;

                        progInst.Program.Use();

                        progInst.UpdateUniforms(updateContext, programChanged);

                        progInst.UpdateBuffers(updateContext);

                        _renderer.ConfigureCaps(draw.ProgramInstance!.Material!);

                        if (!WriteDepth)
                            _renderer.State.SetWriteDepth(false);

                        Draw(draw);
                    }

                    vHandler.Unbind();
                }
            }

            _renderer.PopGroup();
        }

        public bool WriteDepth { get; set; }
    }
}
