using System.Runtime.InteropServices;
using XrEngine.Objects;
using XrMath;
using static XrEngine.Filament.FilamentLib;

namespace XrEngine.Filament
{
    public class FilamentOptions
    {
        public FilamentOptions()
        {
            PostProcessing = false;
            AntiAliasing = FlAntiAliasing.NONE;
            ShadowingEnabled = true;
            ShadowType = FlShadowType.PCF;
            HdrColorBuffer = FlQualityLevel.MEDIUM;
            SampleCount = 1;
            UseSrgb = false;
        }

        public IntPtr Context;
        public IntPtr WindowHandle;
        public FlBackend Driver;
        public string? MaterialCachePath;
        public bool EnableStereo;
        public bool OneViewPerTarget;
        public uint SampleCount;
        public bool PostProcessing;
        public bool UseSrgb;
        public FlAntiAliasing AntiAliasing;
        public bool ShadowingEnabled;
        public FlShadowType ShadowType;
        public FlQualityLevel HdrColorBuffer;
    }

    public class FilamentRender : IRenderEngine
    {
        protected class Content
        {
            public Scene3D? Scene;

            public long Version;

            public Dictionary<EngineObject, Guid>? Objects;
        }

        protected class RenderTargetBind
        {
            public uint ViewId;

            public int RenderTargetId;
        }

        protected class ViewSizeRtBind
        {
            public uint ViewId;

            public Rect2I Viewport;

            public int RenderTargetId;
        }

        protected Rect2I _viewport;
        protected IntPtr _app;
        protected Dictionary<IntPtr, RenderTargetBind> _renderTargets = [];
        protected List<ViewSizeRtBind> _views = [];
        protected RenderTargetBind? _activeRenderTarget;
        protected Content? _content;
        protected FlBackend _driver;
        protected uint _renderTargetDepth;
        protected QueueDispatcher _dispatcher = new();

        protected FilamentOptions _options;

        public FilamentRender(FilamentOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.MaterialCachePath))
                Directory.CreateDirectory(options.MaterialCachePath);

            var initInfo = new InitializeOptions()
            {
                Driver = options.Driver,
                WindowHandle = options.WindowHandle,
                Context = options.Context,
                MaterialCachePath = options.MaterialCachePath ?? string.Empty,
                EnableStereo = options.EnableStereo,
                OneViewPerTarget = options.OneViewPerTarget,
                UseSrgb = options.UseSrgb
            };

            _options = options;
            _renderTargetDepth = 1;

            /*
            if (options.EnableStereo && options.Driver == FlBackend.OpenGL)
                _renderTargetDepth = 2;
            */

            _driver = options.Driver;

            _app = Initialize(ref initInfo);

            if (options.WindowHandle != IntPtr.Zero)
            {
                var mainViewId = CreateView(0, 0, -1);

                _renderTargets[0] = new RenderTargetBind { ViewId = mainViewId, RenderTargetId = -1 };

                SetDefaultRenderTarget();
            }

            ReleaseContext(_app, ReleaseContextMode.NotRelease);
        }

        public GraphicContextInfo GetContext()
        {
            GetGraphicContext(_app, out var info);
            return info;
        }

        public void Dispose()
        {
        }

        public Texture2D? GetDepth()
        {
            throw new NotImplementedException();
        }

        public void SetDefaultRenderTarget()
        {
            _activeRenderTarget = _renderTargets[0];
        }

        protected uint CreateView(uint width, uint height, int renderTargetId)
        {
            var viewOpt = new ViewOptions
            {
                RenderQuality = new RenderQuality
                {
                    HdrColorBuffer = _options.HdrColorBuffer,
                },
                AntiAliasing = _options.AntiAliasing,
                PostProcessingEnabled = _options.PostProcessing,
                ShadowingEnabled = _options.ShadowingEnabled,
                ShadowType = _options.ShadowType,
                BlendMode = FlBlendMode.TRANSLUCENT,
                SampleCount = _options.SampleCount,
                StencilBufferEnabled = false,
                FrustumCullingEnabled = false,
                ScreenSpaceRefractionEnabled = false,
                Viewport = new Rect2I() { Width = width, Height = height },
                RenderTargetId = renderTargetId
            };

            return AddView(_app, ref viewOpt);
        }

        //TODO implement depth
        public void SetRenderTarget(uint width, uint height, nint colorImage, nint depthImage, FlTextureInternalFormat format)
        {
            if (!_renderTargets.TryGetValue(colorImage, out var rtBind))
            {
                var options = new RenderTargetOptions()
                {
                    Width = width,
                    Height = height,
                    SampleCount = 1,
                    TextureId = colorImage,
                    Format = format,
                    Depth = _renderTargetDepth
                };

                rtBind = new RenderTargetBind
                {
                    RenderTargetId = AddRenderTarget(_app, ref options)
                };

                var viewBind = _options.OneViewPerTarget ?
                    _views.FirstOrDefault(a => a.RenderTargetId == rtBind.RenderTargetId) :
                    _views.FirstOrDefault(a => a.Viewport.Width == width && a.Viewport.Height == height);

                if (viewBind == null)
                {
                    viewBind = new ViewSizeRtBind
                    {
                        ViewId = CreateView(width, height, rtBind.RenderTargetId),
                        Viewport = new Rect2I() { Width = width, Height = height },
                        RenderTargetId = rtBind.RenderTargetId,
                    };
                    _views.Add(viewBind);
                }
                rtBind.ViewId = viewBind.ViewId;
                _renderTargets[colorImage] = rtBind;
            }

            _activeRenderTarget = rtBind;
        }

        public void Suspend()
        {
            ReleaseContext(_app, ReleaseContextMode.ReleaseAndSuspend);
        }

        public void Resume()
        {
            ReleaseContext(_app, ReleaseContextMode.ReleaseOnExecute);
        }


        protected Guid GetOrCreate<T>(T obj, Action<Guid> factory) where T : EngineObject
        {
            obj.EnsureId();
            if (!_content!.Objects!.TryGetValue(obj, out var curId))
            {
                factory(obj.Id);
                _content!.Objects[obj] = obj.Id;
            }
            return obj.Id;
        }

        internal unsafe TextureInfo AllocateTexture(Texture2D? texture)
        {
            var result = new TextureInfo();

            if (texture != null)
            {
                texture.EnsureId();

                result.Width = texture.Width;
                result.Height = texture.Height;
                result.Levels = 100;
                result.TextureId = texture.Id;

                result.InternalFormat = texture.Format switch
                {
                    TextureFormat.Rgba32 => FlTextureInternalFormat.RGBA8,
                    TextureFormat.SRgba32 => FlTextureInternalFormat.SRGB8_A8,
                    TextureFormat.RgbFloat32 => FlTextureInternalFormat.RGB32F,
                    TextureFormat.RgbaFloat32 => FlTextureInternalFormat.RGBA32F,
                    TextureFormat.RgbFloat16 => FlTextureInternalFormat.RGB16F,
                    TextureFormat.Bgra32 => FlTextureInternalFormat.RGBA8,
                    _ => throw new NotSupportedException(),
                };
                if (texture.Data != null)
                {
                    var mainData = texture.Data[0];

                    result.Data.Format = mainData.Format switch
                    {
                        TextureFormat.Rgba32 or
                        TextureFormat.SRgba32 => FlPixelFormat.RGBA,

                        TextureFormat.RgbFloat32 => FlPixelFormat.RGB,
                        TextureFormat.RgbaFloat32 => FlPixelFormat.RGBA,

                        TextureFormat.Bgra32 => FlPixelFormat.RGBA,

                        _ => throw new NotSupportedException(),
                    };

                    result.Data.Type = mainData.Format switch
                    {
                        TextureFormat.RgbFloat32 or
                        TextureFormat.RgbFloat16 or
                        TextureFormat.RgbaFloat16 or
                        TextureFormat.RgbaFloat32
                            => FlPixelType.FLOAT,

                        TextureFormat.Rgba32 or
                        TextureFormat.Bgra32 or
                        TextureFormat.SRgba32
                            => FlPixelType.UBYTE,

                        _ => throw new NotSupportedException(),
                    };

                    result.Data.DataSize = mainData.Data!.Size;
                    result.Data.Data = MemoryManager.Allocate(result.Data.DataSize, this);
                    result.Data.AutoFree = true;

                    using var pSrc = mainData.Data.MemoryLock();
                    EngineNativeLib.CopyMemory(pSrc, result.Data.Data, mainData.Data.Size);
                }
            }

            return result;
        }

        protected void Create(Guid id, SunLight sun)
        {
            var info = new LightInfo
            {
                Type = FlLightType.Sun,
                Direction = sun.Direction,
                Intensity = sun.Intensity,
                Color = sun.Color,
                Sun = new FilamentLib.SunLight
                {
                    HaloSize = sun.HaloSize,
                    AngularRadius = sun.SunRadius,
                    HaloFalloff = sun.HaloFallOff,
                },
                CastShadows = sun.CastShadows,
            };
            AddLight(_app, id, ref info);
        }

        protected void Create(Guid id, DirectionalLight dir)
        {
            var info = new LightInfo
            {
                Type = FlLightType.Directional,
                Direction = dir.Direction,
                Intensity = dir.Intensity,
                Color = dir.Color,
                CastShadows = dir.CastShadows,
            };
            AddLight(_app, id, ref info);
        }

        protected void Create(Guid id, ImageLight img)
        {
            var info = new ImageLightInfo
            {
                Intensity = img.Intensity * 4,
                Rotation = img.RotationY,
                ShowSkybox = false,
                Texture = AllocateTexture(img.Panorama)
            };


            AddImageLight(_app, ref info);

            img.Changed += (s, e) =>
            {
                info = new ImageLightInfo
                {
                    Intensity = img.Intensity * 4,
                    Rotation = img.RotationY,
                    ShowSkybox = false,
                };
                UpdateImageLight(_app, ref info);
            };
        }

        protected void Create(Guid id, Group3D group)
        {
            AddGroup(_app, id);
            if (group.Parent is not Scene3D)
                SetObjParent(_app, id, group.Parent!.Id);
        }

        protected unsafe void Create(Guid id, Geometry3D geo)
        {
            var attributes = new List<VertexAttribute>();

            if ((geo.ActiveComponents & VertexComponent.Position) != 0)
                attributes.Add(new VertexAttribute
                {
                    Offset = 0,
                    Size = 12,
                    Type = VertexAttributeType.Position
                });


            if ((geo.ActiveComponents & VertexComponent.Normal) != 0)
                attributes.Add(new VertexAttribute
                {
                    Offset = 12,
                    Size = 12,
                    Type = VertexAttributeType.Normal
                });

            if ((geo.ActiveComponents & VertexComponent.UV0) != 0)
                attributes.Add(new VertexAttribute
                {
                    Offset = 24,
                    Size = 8,
                    Type = VertexAttributeType.UV0
                });


            if ((geo.ActiveComponents & VertexComponent.Tangent) != 0)
                attributes.Add(new VertexAttribute
                {
                    Offset = 32,
                    Size = 16,
                    Type = VertexAttributeType.Tangent
                });

            var attributesArray = attributes.ToArray();

            fixed (VertexAttribute* pAttr = attributesArray)
            {
                var layout = new VertexLayout
                {
                    SizeByte = (uint)Marshal.SizeOf<VertexData>(),
                    AttributeCount = (uint)attributes.Count,
                    Attributes = pAttr
                };

                fixed (uint* pIndex = geo.Indices)
                fixed (VertexData* pVert = geo.Vertices)
                {
                    var geoInfo = new GeometryInfo
                    {
                        layout = layout,
                        Bounds = geo.Bounds,
                        Vertices = (byte*)pVert,
                        VerticesCount = geo.Vertices!.Length,
                        Indices = pIndex,
                        IndicesCount = pIndex == null ? 0 : geo.Indices!.Length
                    };

                    AddGeometry(_app, id, ref geoInfo);
                }
            }
        }

        protected void Create(Guid id, Material mat)
        {

            MaterialInfo UpdateMatInfo()
            {
                if (mat is IPbrMaterial pbr)
                {
                    return new MaterialInfo
                    {
                        NormalMap = AllocateTexture(pbr.NormalMap),
                        Color = pbr.Color,
                        BaseColorMap = AllocateTexture(pbr.ColorMap),
                        MetallicRoughnessMap = AllocateTexture(pbr.MetallicRoughnessMap),
                        AoMap = AllocateTexture(pbr.OcclusionMap),
                        MetallicFactor = pbr.Metalness,
                        RoughnessFactor = pbr.Roughness,
                        NormalScale = pbr.NormalScale,
                        AoStrength = pbr.OcclusionStrength,
                        Blending = pbr.Alpha switch
                        {
                            AlphaMode.Opaque => FlBlendingMode.OPAQUE,
                            AlphaMode.Blend => FlBlendingMode.TRANSPARENT,
                            AlphaMode.BlendMain => FlBlendingMode.TRANSPARENT,
                            AlphaMode.Mask => FlBlendingMode.MASKED,
                            _ => throw new NotSupportedException()
                        },
                        //EmissiveFactor = pbr.EmissiveFactor,
                        //EmissiveMap = AllocateTexture(pbr.EmissiveTexture),
                        //EmissiveStrength = 1,
                        MultiBounceAO = true,
                        SpecularAntiAliasing = true,
                        ScreenSpaceReflection = true,
                        AlphaCutoff = pbr.AlphaCutoff,
                        DoubleSided = pbr.DoubleSided,
                        SpecularAO = FlSpecularAO.Simple,
                        Reflectance = 0.5f,
                        IsLit = true,
                        WriteDepth = pbr.WriteDepth,
                        WriteColor = pbr.WriteColor,
                        UseDepth = pbr.UseDepth,
                        IsShadowOnly = pbr.ReceiveShadows && pbr.Alpha == AlphaMode.Blend && pbr.Color.A < 1
                    };
                }

                if (mat is ColorMaterial color)
                {
                    return new MaterialInfo
                    {
                        Color = color.Color,
                        Blending = color.Alpha == AlphaMode.Opaque ? FlBlendingMode.OPAQUE : FlBlendingMode.TRANSPARENT,
                        DoubleSided = color.DoubleSided,
                        IsLit = false,
                        WriteDepth = color.WriteDepth,
                        WriteColor = color.WriteColor,
                        UseDepth = color.UseDepth,
                    };
                }

                if (mat is ShadowOnlyMaterial shadow)
                {
                    return new MaterialInfo
                    {
                        Color = shadow.ShadowColor,
                        Blending = FlBlendingMode.OPAQUE,
                        DoubleSided = shadow.DoubleSided,
                        IsLit = false,
                        WriteDepth = shadow.WriteDepth,
                        WriteColor = shadow.WriteColor,
                        UseDepth = shadow.UseDepth,
                        IsShadowOnly = true
                    };
                }

                if (mat is TextureMaterial tex)
                {
                    return new MaterialInfo
                    {
                        Blending = tex.Alpha switch
                        {
                            AlphaMode.Opaque => FlBlendingMode.OPAQUE,
                            AlphaMode.BlendMain => FlBlendingMode.TRANSPARENT,
                            AlphaMode.Blend => FlBlendingMode.TRANSPARENT,
                            AlphaMode.Mask => FlBlendingMode.MASKED,
                            _ => throw new NotSupportedException()
                        },
                        BaseColorMap = AllocateTexture(tex.Texture),
                        Color = Color.White,
                        DoubleSided = tex.DoubleSided,
                        IsLit = false,
                        WriteDepth = tex.WriteDepth,
                        WriteColor = tex.WriteColor,
                        UseDepth = tex.UseDepth,
                    };
                }

                throw new NotSupportedException();
            }

            var matInfo = UpdateMatInfo();
            AddMaterial(_app, mat.Id, ref matInfo);

            mat.Changed += (s, c) =>
            {
                matInfo = UpdateMatInfo();
                UpdateMaterial(_app, mat.Id, ref matInfo);
            };

            if (mat is TextureMaterial tex)
            {
                tex.Texture!.Changed += (s, c) =>
                {
                    var texInfo = AllocateTexture(tex.Texture);
                    if (!UpdateTexture(_app, tex.Texture.Id, ref texInfo.Data))
                    {
                        matInfo = UpdateMatInfo();
                        UpdateMaterial(_app, mat.Id, ref matInfo);
                    }
                };
            }
        }

        protected void Create(Guid id, TriangleMesh mesh)
        {
            var geoId = GetOrCreate(mesh.Geometry!, geoId => Create(geoId, mesh.Geometry!));

            var matId = GetOrCreate(mesh.Materials[0], matId => Create(matId, mesh.Materials[0]));

            var meshInfo = new MeshInfo
            {
                Culling = true,
                Fog = false,
                GeometryId = geoId,
                MaterialId = matId,
                ReceiveShadows = true,
            };

            if (mesh.Materials[0] is IShadowMaterial pbr)
                meshInfo.CastShadows = pbr.CastShadows;

            AddMesh(_app, id, ref meshInfo);

            if (mesh.Parent is not Scene3D)
                SetObjParent(_app, id, mesh.Parent!.Id);

            mesh.Changed += (s, c) =>
            {
                if (c.IsAny(ObjectChangeType.Render) && mesh.Materials.Count > 0)
                {
                    var matId = GetOrCreate(mesh.Materials[0], matId => Create(matId, mesh.Materials[0]));
                    SetMeshMaterial(_app, id, matId);
                }
            };
        }

        protected void Create(Guid id, Object3D obj)
        {
            if (obj is SunLight sun)
            {
                Create(id, sun);
            }
            else if (obj is DirectionalLight dir)
            {
                Create(id, dir);
            }
            else if (obj is ImageLight img && img.Panorama != null)
            {
                Create(id, img);
            }
            else if (obj is Group3D group)
            {
                Create(id, group);
            }
            else if (obj is Object3DInstance instance)
            {
                if (instance.Reference == null)
                    return;
                GetOrCreate(instance, id => Create(obj.Id, instance.Reference));
            }
            else if (obj is TriangleMesh mesh)
            {
                if (mesh.Materials.Count == 0 || mesh.Geometry == null || (
                    mesh.Materials[0] is not IPbrMaterial &&
                    mesh.Materials[0] is not ColorMaterial &&
                    mesh.Materials[0] is not ShadowOnlyMaterial &&
                    mesh.Materials[0] is not TextureMaterial))
                    return;

                Create(id, mesh);
            }
        }

        protected unsafe void BuildContent(Scene3D scene)
        {
            if (_content == null)
            {
                _content = new Content()
                {
                    Objects = []
                };
            }
            _content.Scene = scene;
            _content.Version = scene.Version;

            foreach (var obj in scene.Descendants())
            {
                if (!obj.IsVisible)
                    continue;
                GetOrCreate(obj, id => Create(id, obj));
            }
        }

        public unsafe void Render(RenderContext ctx, Rect2I viewport, bool flush)
        {
            var scene = ctx.Scene!;

            if (_content == null || _content.Scene != scene || _content.Version != scene.Version)
                BuildContent(scene);

            var render = stackalloc RenderTarget[1];
            var camera = (PerspectiveCamera)ctx.Camera!;

            render[0].Camera = new CameraInfo
            {
                Far = camera.Far,
                Near = camera.Near,
                Projection = camera.Projection,
                Transform = camera.WorldMatrix
            };

            if (camera.Eyes != null)
            {
                render[0].Camera.IsStereo = true;

                render[0].Camera.Eye1 = new CameraEyesInfo
                {
                    RelTransform = camera.Eyes[0].World,
                    Projection = camera.Eyes[0].Projection
                };

                render[0].Camera.Eye2 = new CameraEyesInfo
                {
                    RelTransform = camera.Eyes[1].World,
                    Projection = camera.Eyes[1].Projection
                };
            }
            else
            {
                render[0].Camera.Eye1.Projection = camera.Projection;
                render[0].Camera.Eye2.Projection = camera.Projection;
            }

            render[0].RenderTargetId = _activeRenderTarget!.RenderTargetId;
            render[0].ViewId = _activeRenderTarget.ViewId;
            render[0].Viewport = viewport;

            foreach (var mesh in _content!.Objects!.Where(a => a.Key is TriangleMesh || a.Key is Object3DInstance))
            {
                SetObjVisible(_app, mesh.Value, ((Object3D)mesh.Key).IsVisible);
                SetObjTransform(_app, mesh.Value, ((Object3D)mesh.Key).WorldMatrix);
            }

            FilamentLib.Render(_app, render, 1, flush);

            _viewport = viewport;

            _dispatcher.ProcessQueue();

        }

        public void SetRenderTarget(Texture2D? texture)
        {
            throw new NotSupportedException();
        }

        public Texture2D? GetShadowMap()
        {
            throw new NotSupportedException();
        }

        public IList<TextureData>? ReadTexture(Texture texture, TextureFormat format, uint startMipLevel = 0, uint? endMipLevel = null)
        {
            throw new NotSupportedException();
        }

        public FlBackend Driver => _driver;

        public Rect2I View => _viewport;

        public IDispatcher Dispatcher => _dispatcher;
    }
}
