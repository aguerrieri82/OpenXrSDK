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
        Silk.NET.OpenGLES.Extensions.EXT.ExtPrimitiveBoundingBox _bounds;
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
                                               (a.SceneLayer is DetachedLayer det));
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

        protected virtual bool UpdateProgram(UpdateShaderContext updateContext, GlProgramInstance progInst)
        {
            return progInst.UpdateProgram(updateContext);
        }

        protected void SetBounds(Camera camera, Object3D obj)
        {

#if GLES
            var bounds = obj.WorldBounds;

            var min = Vector4.Transform(new Vector4(bounds.Min, 1.0f), camera.ViewProjection);
            var max = Vector4.Transform(new Vector4(bounds.Max, 1.0f), camera.ViewProjection);
            
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

            var updateContext = _renderer.UpdateContext;

            var useDepthPass = _renderer.Options.UseDepthPass;

            var useOcclusion = _renderer.Options.UseOcclusionQuery;


            foreach (var shader in layer.Content.Contents)
            {
                var progGlobal = shader.Value!.ProgramGlobal;

                updateContext.Shader = shader.Key;
                updateContext.Stage = UpdateShaderStage.Shader;

                progGlobal!.UpdateProgram(updateContext, GetRenderTarget() as IShaderHandler);


                foreach (var material in shader.Value.Contents!
                                        .OrderBy(a=> a.Key.Priority)
                                        .ThenBy(a=> a.Value.ProgramInstance?.Program?.Handle ?? 0))
                {
                    var matContent = material.Value;

                    if (material.Value.IsHidden)
                        continue;

                    updateContext.UseInstanceDraw = matContent.UseInstanceDraw;

                    var progInst = matContent.ProgramInstance!;
                    
                    updateContext.Stage = UpdateShaderStage.Material;

                    UpdateProgram(updateContext, progInst);

                    var programChanged = updateContext.ProgramInstanceId != progInst.Program!.Handle;

                    updateContext.ProgramInstanceId = progInst.Program!.Handle;

                    progInst.Program.Use();

                    progInst.UpdateBuffers(updateContext);

                    progInst.UpdateUniforms(updateContext, programChanged);

                    ConfigureCaps(progInst.Material!);

                    foreach (var vertex in matContent.Contents)
                    {
                        var vertexContent = vertex.Value;
                        if (vertexContent.IsHidden)
                            continue;

                        if (vertexContent.Contents.All(a => a.IsClipped))
                            continue;

                        var vHandler = vertexContent.VertexHandler!;

                        updateContext.ActiveComponents = vertexContent.ActiveComponents;

                        vHandler.Bind();

                        updateContext.Stage = UpdateShaderStage.Model;

                        if (vertexContent.Draw != null)
                        {
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

            var updateContext = _renderer.UpdateContext;

            var useDepthPass = _renderer.Options.UseDepthPass;

            var useOcclusion = _renderer.Options.UseOcclusionQuery;

            updateContext.UseInstanceDraw = false;

            updateContext.Stage = UpdateShaderStage.Any; 

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
