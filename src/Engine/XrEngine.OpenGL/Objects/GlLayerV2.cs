using System.Runtime.InteropServices;

#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrMath;

namespace XrEngine.OpenGL
{

    public enum InstanceBufferMode
    {
        Auto,
        UpdateAlways,
        UpdateIncremental,
        UpdateAllWhenChanged
    }

    public class GlLayerV2 : IDisposable, IGlLayer
    {
        protected readonly OpenGLRender _render;
        protected readonly RenderContentV2 _content;
        protected readonly Scene3D _scene;
        protected readonly ILayer3D? _sceneLayer;
        protected readonly GlLayerType _type;
        protected long _lastUpdateVersion;
        protected long _lastFrame;
        protected Camera? _lastCamera;
        protected int _lastDrawId;
        protected bool _isContentDirty;

        public GlLayerV2(OpenGLRender render, Scene3D scene, GlLayerType type, ILayer3D? sceneLayer = null)
        {
            _render = render;
            _content = new RenderContentV2();
            _scene = scene;
            _lastUpdateVersion = -1;
            _sceneLayer = sceneLayer;
            _type = type;
            if (sceneLayer != null)
                sceneLayer.Changed += OnSceneLayerChanged;
        }

        private void OnSceneLayerChanged(ILayer3D layer, Layer3DChange change)
        {
            if (change.Type == Layer3DChangeType.Added)
                AddContent((Object3D)change.Item);

            else if (change.Type == Layer3DChangeType.Removed)
                RemoveContent((Object3D)change.Item);

            _lastUpdateVersion = _sceneLayer != null ? _sceneLayer.Version : _scene.Version;
        }

        protected virtual ShaderMaterial ReplaceMaterial(ShaderMaterial material)
        {
            return material;
        }

        public void Update()
        {
            if (NeedUpdate)
                Rebuild();
        }

        public void Rebuild()
        {
            Log.Info(this, "Building content '{0}' ({1})...", _scene.Name ?? "", _sceneLayer?.Name ?? "Main");

            _content.Contents.Clear();
            _content.LayerVersion = Version;

            _lastDrawId = 0;

            IEnumerable<Object3D> objects = _sceneLayer != null ?
                _sceneLayer.Content.OfType<Object3D>() :
                _scene.Descendants();

            foreach (Object3D obj3D in objects)
                AddContent(obj3D);

            _lastUpdateVersion = _sceneLayer != null ? _sceneLayer.Version : _scene.Version;

            Log.Debug(this, "Content Build");
        }

        protected void RemoveContent(Object3D obj3d)
        {
            if (!obj3d.Feature<IVertexSource>(out IVertexSource? vrtSrc))
                return;

            List<Action> clean = new List<Action>();

            foreach (KeyValuePair<Shader, ShaderContentV2> shader in _content.Contents)
            {
                foreach (KeyValuePair<Material, MaterialContentV2> material in shader.Value.Contents)
                {
                    foreach (KeyValuePair<EngineObject, VertexContentV2> vertex in material.Value.Contents)
                    {
                        for (int i = vertex.Value.Contents.Count - 1; i >= 0; i--)
                        {
                            DrawContent draw = vertex.Value.Contents[i];

                            if (draw.Object == obj3d)
                                vertex.Value.Contents.RemoveAt(i);
                        }

                        if (vertex.Value.Contents.Count == 0)
                            clean.Add(() => material.Value.Contents.Remove(vertex.Key));
                    }

                    if (material.Value.Contents.Count == 0)
                        clean.Add(() => shader.Value.Contents.Remove(material.Key));
                }

                if (shader.Value.Contents.Count == 0)
                    clean.Add(() => _content.Contents.Remove(shader.Key));
            }

            foreach (Action action in clean)
                action();

            _isContentDirty = true;
        }

        protected void AddContent(Object3D obj3d)
        {
            if (!obj3d.Feature<IVertexSource>(out IVertexSource? vrtSrc))
                return;

            foreach (ShaderMaterial realMaterial in vrtSrc.Materials.OfType<ShaderMaterial>())
            {
                ShaderMaterial material = ReplaceMaterial(realMaterial);

                if (material.Shader == null)
                    continue;

                if (!_content.Contents.TryGetValue(material.Shader, out ShaderContentV2? shaderContent))
                {
                    shaderContent = new ShaderContentV2
                    {
                        ProgramGlobal = material.Shader.GetGlResource(gl => new GlProgramGlobal(_render.GL, material.Shader!))
                    };

                    _content.Contents[material.Shader] = shaderContent;
                }

                if (!shaderContent.Contents.TryGetValue(material, out MaterialContentV2? materialContent))
                {
                    GlProgramInstance instance = new GlProgramInstance(_render.GL, material, shaderContent.ProgramGlobal!, obj3d);

                    ConfigureProgramInstance(instance);

                    materialContent = new MaterialContentV2
                    {
                        ProgramInstance = instance
                    };

                    shaderContent.Contents[material] = materialContent;
                }

                GlVertexSourceHandle vertexHandler = vrtSrc.Object.GetGlResource(a => GlVertexSourceHandle.Create(_render.GL, vrtSrc));

                if (!materialContent.Contents.TryGetValue(vrtSrc.Object, out VertexContentV2? vertexContent))
                {
                    vertexContent = new VertexContentV2
                    {
                        VertexHandler = vertexHandler,
                        ActiveComponents = VertexComponent.None,
                    };

                    foreach (GlVertexAttribute attr in vertexHandler.Layout!.Attributes!)
                        vertexContent.ActiveComponents |= attr.Component;

                    materialContent.Contents[vrtSrc.Object] = vertexContent;
                }

                vertexContent.ContentVersion++;

                Action draw;

                if (material is ITessellationMaterial tes && tes.TessellationMode != TessellationMode.None)
                {
                    int size = vrtSrc.Primitive == DrawPrimitive.Quad ? 4 : 3;
                    //TODO: disable instance draw
                    draw = () =>
                    {
                        _render.GL.PatchParameter(PatchParameterName.Vertices, size);
                        _render.State.SetWireframe(tes.DebugTessellation);
                        _render.State.SetLineWidth(0.5f);
                        vertexContent!.VertexHandler!.Draw(DrawPrimitive.Patch);
                    };
                }
                else
                {
                    DrawPrimitive? primitive = material.Shader.ForcePrimitive;
                    draw = () => vertexContent!.VertexHandler!.Draw(primitive);
                }


                vertexContent.Contents.Add(new DrawContent
                {
                    Draw = draw,
                    DrawId = _lastDrawId++,
                    Object = obj3d,
                    ProgramInstance = materialContent.ProgramInstance
                });
            }

            _isContentDirty = true;
        }

        protected virtual void ConfigureProgramInstance(GlProgramInstance instance)
        {

        }

        public void Prepare(RenderContext ctx)
        {
            Camera curCamera = _render.UpdateContext.PassCamera!;

            if (ctx.Frame == _lastFrame && curCamera == _lastCamera)
                return;

            if (_isContentDirty)
                SortMaterials();

            ComputeVisibility();

            UpdateVertexHandlers();

            _lastFrame = ctx.Frame;
            _lastCamera = curCamera;

            _isContentDirty = false;
        }

        protected void SortMaterials()
        {
            foreach (ShaderContentV2 shaderCont in _content.Contents.Values)
                shaderCont.SortedContent = shaderCont.Contents.OrderBy(a => a.Key.Priority).ToArray();
        }

        protected unsafe void UpdateVertexHandlers()
        {

            foreach (KeyValuePair<Shader, ShaderContentV2> shaderEntry in _content.Contents)
            {
                Shader shader = shaderEntry.Key;

                IInstanceShader? instanceShader = shader as IInstanceShader;

                foreach (KeyValuePair<Material, MaterialContentV2> matEntry in shaderEntry.Value.Contents)
                {
                    Dictionary<EngineObject, VertexContentV2>.ValueCollection verContentList = matEntry.Value.Contents.Values;

                    matEntry.Value.UseInstanceDraw = _render.Options.UseInstanceDraw && instanceShader != null &&
                                                     verContentList.Any(a => a.Contents.Count > 1);

                    foreach (VertexContentV2 verContent in matEntry.Value.Contents.Values)
                    {
                        GlVertexSourceHandle vHandler = verContent.VertexHandler!;

                        if (vHandler.NeedUpdate)
                            vHandler.Update();

                        if (matEntry.Value.UseInstanceDraw)
                            UpdateInstanceDraws(instanceShader!, verContent, matEntry.Key);
                        else
                            verContent.Draw = null;
                    }

                }
            }
        }


        protected unsafe void UpdateInstanceDraws(IInstanceShader instanceShader, VertexContentV2 verContent, Material material)
        {
            GlVertexSourceHandle vHandler = verContent.VertexHandler!;

            InstanceBufferMode mode = InstanceBufferMode.Auto;

            int changedCount = 0;

            int elSize = Marshal.SizeOf(instanceShader.InstanceBufferType);

            if (verContent.InstanceBuffer == null || verContent.InstanceBuffer.Version != verContent.ContentVersion)
            {
                //TODO: store in somewhere safe, is unique for material+geometry
                verContent.InstanceBuffer ??= GlBuffer.Create(_render.GL, BufferTargetARB.ShaderStorageBuffer, instanceShader.InstanceBufferType);

                verContent.InstanceBuffer.Allocate((uint)(elSize * verContent.Contents.Count));
                verContent.InstanceBuffer.Version = verContent.ContentVersion;

                mode = InstanceBufferMode.UpdateAlways;
            }

            if (mode != InstanceBufferMode.UpdateAlways)
            {
                for (int i = 0; i < verContent.Contents.Count; i++)
                {
                    DrawContent draw = verContent.Contents[i]!;
                    if (instanceShader.NeedUpdate(draw.Object!, draw.InstanceVersion))
                    {
                        draw.InstanceChanged = true;
                        changedCount++;
                        if (mode == InstanceBufferMode.UpdateAllWhenChanged)
                            break;
                    }
                }
                if (changedCount == 0)
                    return;
            }

            if (mode == InstanceBufferMode.Auto)
            {
                float ratio = (float)changedCount / verContent.Contents.Count;
                if (ratio < 0.3 && changedCount < 5)
                    mode = InstanceBufferMode.UpdateIncremental;
                else
                    mode = InstanceBufferMode.UpdateAlways;
            }

            if (mode == InstanceBufferMode.UpdateAlways || mode == InstanceBufferMode.UpdateAllWhenChanged)
            {
                byte* data = verContent.InstanceBuffer!.Lock(BufferAccessMode.Replace);

                for (int i = 0; i < verContent.Contents.Count; i++)
                {
                    DrawContent draw = verContent.Contents[i];
                    draw.InstanceVersion = instanceShader.Update(data, draw.Object!, draw.Id);
                    data += elSize;
                }

                verContent.InstanceBuffer.Unlock();
            }
            else
            {

                verContent.InstanceBuffer!.BeginUpdate();
                byte* buffer = stackalloc byte[elSize];

                for (int i = 0; i < verContent.Contents.Count; i++)
                {
                    DrawContent draw = verContent.Contents[i];

                    if (!draw.InstanceChanged)
                        continue;

                    draw.InstanceVersion = instanceShader.Update(buffer, draw.Object!, draw.Id);
                    verContent.InstanceBuffer!.UpdateRange(new ReadOnlySpan<byte>(buffer, elSize), i);
                }

                verContent.InstanceBuffer!.EndUpdate();
            }

            if (verContent.Draw == null)
            {
                if (material is ITessellationMaterial tes && tes.TessellationMode != TessellationMode.None)
                {
                    int size = vHandler.Source.Primitive == DrawPrimitive.Quad ? 4 : 3;

                    verContent.Draw = () =>
                    {
                        _render.GL.PatchParameter(PatchParameterName.Vertices, size);
                        _render.State.SetWireframe(tes.DebugTessellation);
                        _render.State.SetLineWidth(0.5f);

                        _render.GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 4, ((GlObject)verContent.InstanceBuffer).Handle);
                        vHandler.DrawInstances(verContent.Contents.Count, DrawPrimitive.Patch);
                    };
                }
                else
                {
                    verContent.Draw = () =>
                    {
                        _render.GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 4, ((GlObject)verContent.InstanceBuffer).Handle);

                        vHandler.DrawInstances(verContent.Contents.Count);
                    };
                }
            }


        }

        protected int ComputeVisibility()
        {
            GlUpdateContext updateContext = _render.UpdateContext;

            int totHidden = 0;
            int totDraw = 0;

            foreach (MaterialContentV2? material in _content.Contents.SelectMany(a => a.Value.Contents.Values))
            {
                bool allMatHidden = true;

                foreach (VertexContentV2 vertex in material.Contents.Values)
                {
                    bool allVertexHidden = true;

                    foreach (DrawContent draw in vertex.Contents)
                    {
                        totDraw++;

                        GlProgramInstance progInst = material.ProgramInstance!;

                        draw.IsHidden = !progInst.Material!.IsEnabled || !draw.Object!.IsVisible;

                        if (!draw.IsHidden && _render.Options.FrustumCulling && draw.Object is TriangleMesh mesh && (mesh.Flags & EngineObjectFlags.NoFrustumCulling) == 0)
                        {
                            draw.IsHidden = !mesh.WorldBounds.IntersectFrustum(updateContext.FrustumPlanes!);
                            if (draw.IsHidden)
                                totHidden++;
                        }

                        if (!draw.IsHidden)
                        {
                            allVertexHidden = false;
                            allMatHidden = false;
                        }
                    }
                    vertex.IsHidden = allVertexHidden;
                }
                material.IsHidden = allMatHidden;
            }

            return totHidden;
        }

        public void Dispose()
        {
            if (_sceneLayer != null)
                _sceneLayer.Changed -= OnSceneLayerChanged;
            _content?.Contents.Clear();
            GC.SuppressFinalize(this);
        }

        public string? Name => _sceneLayer?.Name;

        public bool NeedUpdate => _lastUpdateVersion != Version;

        public GlLayerType Type => _type;

        public RenderContentV2 Content => _content;

        public ILayer3D? SceneLayer => _sceneLayer;

        public Scene3D Scene => _scene;

        public bool IsEmpty => _content.Contents.Count == 0;

        public long Version => _sceneLayer != null ? _sceneLayer.Version : _scene.Version;
    }
}
