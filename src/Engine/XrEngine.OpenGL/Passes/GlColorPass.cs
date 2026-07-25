#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using XrEngine.Helpers;

namespace XrEngine.OpenGL
{
    public class GlColorPass : GlBaseRenderPass
    {
        int _frame = 0;
        readonly ShaderMaterial _dummyMaterial;

#if GLES
        readonly Silk.NET.OpenGLES.Extensions.EXT.ExtPrimitiveBoundingBox _bounds;
#endif

        public GlColorPass(OpenGLRender renderer)
            : base(renderer)
        {
            WriteDepth = true;
#if GLES
            _bounds = new Silk.NET.OpenGLES.Extensions.EXT.ExtPrimitiveBoundingBox(renderer.GL.Context);
#endif

            _dummyMaterial = new PbrMaterial
            {
                ColorMap = TextureFactory.CreateChecker(),
                Metalness = 0,
                Roughness = 0.5f
            };
        }

        protected override bool BeginRender(GlUpdateContext ctx)
        {
            GetRenderTarget()!.Begin(ctx.PassCamera!);

            if (_renderer.Options.UseDepthPass)
            {
                _renderer.State.SetWriteColor(true);
                _gl.Clear(ClearBufferMask.ColorBufferBit);
                _gl.DepthFunc(DepthFunction.Lequal);
            }
            else
                _renderer.Clear(ctx.PassCamera!.BackgroundColor);

            _frame++;

            return true;
        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => (a.Type & GlLayerType.Color) == GlLayerType.Color ||
                                               (a.Type & GlLayerType.Static) == GlLayerType.Static ||
                                               (a.SceneLayer is DetachedLayer det && det.Usage != DetachedLayerUsage.Outline));
        }

        protected override void EndRender(GlUpdateContext ctx)
        {
            _renderer.State.SetActiveProgram(0);
            //_renderer.RenderTarget!.End(false);
        }

        protected virtual bool CanDraw(DrawContent draw)
        {

            if (draw.IsHidden || draw.IsClipped)
                return false;

            if (draw.Query != null)
            {
                var passed = draw.Query.GetResult();
                if (passed == 0)
                    return false;
            }

            return true;
        }

        protected void Draw(DrawContent draw)
        {
            draw.Draw!();

#if DEBUG
            var name = draw.Object!.Name;
            if (name != null)
                _gl.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, (uint)name.Length, name);
#endif

        }

        protected virtual bool UpdateProgram(UpdateShaderContext updateContext, GlProgramInstance progInst, bool forceSync = false)
        {
            return progInst.UpdateProgram(updateContext, forceSync);
        }

        /*
        protected void SetBounds(Camera camera, Object3D obj)
        {

#if GLES
            var bounds = obj.WorldBounds;

            var min = Vector4.Transform(new Vector4(bounds.Min, 1.0f), camera.ViewProjection);
            var max = Vector4.Transform(new Vector4(bounds.Max, 1.0f), camera.ViewProjection);

            _bounds.PrimitiveBoundingBox(min.X, min.Y, min.Z, min.W, max.X, max.Y, max.Z, max.W);
#endif

        }
        */

        protected virtual void ConfigureCaps(ShaderMaterial material)
        {
            _renderer.ConfigureCaps(material);

            if (!WriteDepth)
                _renderer.State.SetWriteDepth(false);
        }

        public override void RenderLayer(GlLayer layer)
        {
            GlUtils.EnsureRenderThread();

            if (layer.SceneLayer != null && !layer.SceneLayer.IsVisible)
                return;
#if GL_WRAPPER
            
            var timer = Stopwatch.StartNew();

            bool isRecording = false;

            var wrapper = _gl as OpenGLWrapper.GlSwitchWrapper;

            if (wrapper != null && layer.IsStatic)
            {
                if (layer.RenderActions.Count == 0 && _frame > 1000)
                {
                    _renderer.State.Reset();
                    isRecording = true;
                    wrapper.BeginRecord();
                }
                else if (layer.RenderActions.Count > 0)
                {
                    layer.Execute(wrapper.Enqueue.Instance);
                    _renderer.State.Reset();

                    if (layer.IsStatic && _frame % 100 == 0)
                    {
                        timer.Stop();
                        Log.Warn(this, "STATIC TIME: {0}", timer.Elapsed.TotalMilliseconds);
                    }
                    return;
                }
            }
#endif

            _renderer.PushGroup($"Layer {layer.Name ?? layer.Type.ToString()}");

            var updateContext = _renderer.UpdateContext;

            var useDepthPass = _renderer.Options.UseDepthPass;

            var useOcclusion = _renderer.Options.UseOcclusionQuery;

            uint globalProgChangesCount = 0;

            foreach (var shader in layer.Content.SortedContent!)
            {
                var progGlobal = shader.Value!.ProgramGlobal;

                updateContext.Shader = shader.Key;
                updateContext.Stage = UpdateShaderStage.Shader;

                progGlobal!.UpdateProgram(updateContext, GetRenderTarget()?.ShaderHandler);

                foreach (var material in shader.Value.SortedContent!)
                {
                    var matContent = material.Value;

                    if (material.Value.IsHidden)
                        continue;

                    updateContext.UseInstanceDraw = matContent.UseInstanceDraw;

                    var progInst = matContent.ProgramInstance!;

                    updateContext.Stage = UpdateShaderStage.Material;

                    updateContext.ActiveComponents = matContent.ActiveComponents;

                    var progChanged = UpdateProgram(updateContext, progInst);

                    if (!progInst.IsReady)
                    {
                        progInst = GetProgramInstance(_dummyMaterial);
                        updateContext.Stage = UpdateShaderStage.Shader;
                        progInst.Global.UpdateProgram(updateContext, GetRenderTarget()?.ShaderHandler);
                        updateContext.Stage = UpdateShaderStage.Material;
                        progChanged = UpdateProgram(updateContext, progInst);
                    }

                    var programChanged = updateContext.ProgramInstanceId != progInst.Program!.Handle;

                    updateContext.ProgramInstanceId = progInst.Program!.Handle;

                    progInst.Program.Use();

                    progInst.UpdateBuffers(updateContext);

                    progInst.UpdateUniforms(updateContext, programChanged);

                    ConfigureCaps(progInst.Material!);

                    if (progChanged)
                    {
                        globalProgChangesCount++;
                        layer.Invalidate(shader.Value);
                    }

                    foreach (var vertex in matContent.Contents)
                    {
                        var vertexContent = vertex.Value;
                        if (vertexContent.IsHidden)
                            continue;

                        if (vertexContent.Contents.All(a => a.IsClipped))
                            continue;

                        var vHandler = vertexContent.VertexHandler!;

                        vHandler.Bind();

                        updateContext.Stage = UpdateShaderStage.Model;

                        if (vertexContent.Draw != null)
                        {
#if GL_VALIDATE_PROG
                            progInst.Program.Validate();
#endif
                            vertexContent.Draw();
                        }
                        else
                        {
                            foreach (var draw in vertexContent.Contents)
                            {
                                if (!CanDraw(draw))
                                    continue;

                                updateContext.Model = draw.Object;

                                progInst.UpdateModel(updateContext);

                                //SetBounds(updateContext.PassCamera!, draw.Object!);

#if GL_VALIDATE_PROG
                                progInst.Program.Validate();
#endif

                                Draw(draw);
                            }
                        }

                        //vHandler.Unbind();
                    }
                }

                _renderer.State.SetActiveProgram(0);
            }

            _renderer.State.BindVertexArray(0);


            _renderer.PopGroup();

            if (globalProgChangesCount > 0)
                Log.Debug(this, "Changes: {0}", globalProgChangesCount);

#if GL_WRAPPER
            if (wrapper != null && isRecording)
                layer.RenderActions.AddRange(wrapper.EndRecord());

            if (layer.IsStatic && _frame % 100 == 0)
            {
                timer.Stop();
                Log.Warn(this, "STATIC TIME: {0}", timer.Elapsed.TotalMilliseconds);
            }
#endif
        }



        public bool WriteDepth { get; set; }
    }
}
