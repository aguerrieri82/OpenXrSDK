#if GLES
using Silk.NET.OpenGLES;
using System.Numerics;
using XrMath;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public class GlColorPass : GlBaseRenderPass
    {

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

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => (a.Type & GlLayerType.Color) == GlLayerType.Color ||
                                               (a.SceneLayer is DetachedLayer det && det.Usage != DetachedLayerUsage.Outline));
        }

        protected override void EndRender()
        {
            // _renderer.State.SetActiveProgram(0);
            // _renderer.RenderTarget!.End(false);
        }

        protected virtual bool CanDraw(DrawContent draw)
        {
            if (draw.IsHidden || draw.IsClipped)
                return false;

            if (draw.Query != null)
            {
                uint passed = draw.Query.GetResult();
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

        protected virtual bool UpdateProgram(UpdateShaderContext updateContext, GlProgramInstance progInst)
        {
            return progInst.UpdateProgram(updateContext);
        }

        protected void SetBounds(Camera camera, Object3D obj)
        {

#if GLES
            Bounds3 bounds = obj.WorldBounds;

            Vector4 min = Vector4.Transform(new Vector4(bounds.Min, 1.0f), camera.ViewProjection);
            Vector4 max = Vector4.Transform(new Vector4(bounds.Max, 1.0f), camera.ViewProjection);

            _bounds.PrimitiveBoundingBox(min.X, min.Y, min.Z, min.W, max.X, max.Y, max.Z, max.W);
#endif

        }

        protected virtual void ConfigureCaps(ShaderMaterial material)
        {
            _renderer.ConfigureCaps(material);

            if (!WriteDepth)
                _renderer.State.SetWriteDepth(false);
        }

        public override void RenderLayer(GlLayerV2 layer)
        {
            if (layer.SceneLayer != null && !layer.SceneLayer.IsVisible)
                return;

            _renderer.PushGroup($"Layer {layer.Name ?? layer.Type.ToString()}");

            GlUpdateContext updateContext = _renderer.UpdateContext;

            bool useDepthPass = _renderer.Options.UseDepthPass;

            bool useOcclusion = _renderer.Options.UseOcclusionQuery;


            foreach (KeyValuePair<Shader, ShaderContentV2> shader in layer.Content.Contents)
            {
                GlProgramGlobal? progGlobal = shader.Value!.ProgramGlobal;

                updateContext.Shader = shader.Key;
                updateContext.Stage = UpdateShaderStage.Shader;

                progGlobal!.UpdateProgram(updateContext, GetRenderTarget()?.ShaderHandler);


                foreach (KeyValuePair<Material, MaterialContentV2> material in shader.Value.Contents!
                                        .OrderBy(a => a.Key.Priority)
                                        .ThenBy(a => a.Value.ProgramInstance?.Program?.Handle ?? 0))
                {
                    MaterialContentV2 matContent = material.Value;

                    if (material.Value.IsHidden)
                        continue;


                    updateContext.UseInstanceDraw = matContent.UseInstanceDraw;

                    GlProgramInstance progInst = matContent.ProgramInstance!;

                    updateContext.Stage = UpdateShaderStage.Material;

                    UpdateProgram(updateContext, progInst);

                    bool programChanged = updateContext.ProgramInstanceId != progInst.Program!.Handle;

                    updateContext.ProgramInstanceId = progInst.Program!.Handle;

                    progInst.Program.Use();

                    progInst.UpdateBuffers(updateContext);

                    progInst.UpdateUniforms(updateContext, programChanged);

                    ConfigureCaps(progInst.Material!);

                    foreach (KeyValuePair<EngineObject, VertexContentV2> vertex in matContent.Contents)
                    {
                        VertexContentV2 vertexContent = vertex.Value;
                        if (vertexContent.IsHidden)
                            continue;

                        if (vertexContent.Contents.All(a => a.IsClipped))
                            continue;

                        GlVertexSourceHandle vHandler = vertexContent.VertexHandler!;

                        updateContext.ActiveComponents = vertexContent.ActiveComponents;

                        vHandler.Bind();

                        updateContext.Stage = UpdateShaderStage.Model;

                        if (vertexContent.Draw != null)
                        {
                            vertexContent.Draw();
                        }
                        else
                        {
                            foreach (DrawContent draw in vertexContent.Contents)
                            {
                                if (!CanDraw(draw))
                                    continue;

                                updateContext.Model = draw.Object;

                                progInst.UpdateModel(updateContext);

                                SetBounds(updateContext.PassCamera!, draw.Object!);

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
        }


        public override void RenderLayer(GlLayer layer)
        {
            if (layer.SceneLayer != null && !layer.SceneLayer.IsVisible)
                return;

            _renderer.PushGroup($"Layer {layer.Name ?? layer.Type.ToString()}");

            GlUpdateContext updateContext = _renderer.UpdateContext;

            bool useDepthPass = _renderer.Options.UseDepthPass;

            bool useOcclusion = _renderer.Options.UseOcclusionQuery;

            updateContext.UseInstanceDraw = false;

            updateContext.Stage = UpdateShaderStage.Any;

            foreach (KeyValuePair<Shader, ShaderContent> shader in layer.Content.ShaderContentsSorted)
            {
                GlProgramGlobal? progGlobal = shader.Value!.ProgramGlobal;

                updateContext.Shader = shader.Key;

                progGlobal!.UpdateProgram(updateContext, GetRenderTarget()?.ShaderHandler);

                IEnumerable<VertexContent> vertices = shader.Value.ContentsSorted;

                if (_renderer.Options.SortByCameraDistance)
                    vertices = vertices.OrderBy(a => a.RenderPriority).ThenBy(a => a.AvgDistance);

                foreach (VertexContent vertex in vertices)
                {
                    if (vertex.IsHidden)
                        continue;

                    if (useOcclusion && vertex.Contents.All(a => a.Query != null && a.Query.GetResult() == 0))
                        continue;

                    GlVertexSourceHandle vHandler = vertex.VertexHandler!;

                    updateContext.ActiveComponents = vertex.ActiveComponents;

                    vHandler.Bind();

                    foreach (DrawContent draw in vertex.Contents)
                    {
                        if (!CanDraw(draw))
                            continue;

                        GlProgramInstance progInst = draw.ProgramInstance!;

                        updateContext.Model = draw.Object;

                        UpdateProgram(updateContext, progInst);

                        bool programChanged = updateContext.ProgramInstanceId != progInst.Program!.Handle;

                        updateContext.ProgramInstanceId = progInst.Program!.Handle;

                        progInst.Program.Use();

                        progInst.UpdateUniforms(updateContext, programChanged);

                        progInst.UpdateBuffers(updateContext);

                        ConfigureCaps(draw.ProgramInstance!.Material!);

                        SetBounds(updateContext.PassCamera!, draw.Object!);

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
