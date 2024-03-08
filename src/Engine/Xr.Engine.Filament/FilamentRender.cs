using System.Runtime.InteropServices;
using Xr.Math;
using static Xr.Engine.Filament.FilamentLib;

namespace Xr.Engine.Filament
{
    public class FilamentOptions
    {
        public IntPtr Context;
        public IntPtr WindowHandle;
        public FlBackend Driver;
        public string? MaterialCachePath;
        public bool EnableStereo;
        public bool OneViewPerTarget;
        public uint SampleCount;
    }

    public class FilamentRender : IRenderEngine
    {
        protected class Content
        {
            public Scene? Scene;

            public long Version;

            public Dictionary<EngineObject, uint>? Objects;
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
        protected bool _oneViewPerTarget;
        protected uint _renderTargetDepth;
        protected uint _sampleCount;

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
                OneViewPerTarget = options.OneViewPerTarget
            };

            _renderTargetDepth = 1;
            _sampleCount = options.SampleCount <= 0 ? 1 : _sampleCount;
            /*
            if (options.EnableStereo && options.Driver == FlBackend.OpenGL)
                _renderTargetDepth = 2;
            */

            _oneViewPerTarget = options.OneViewPerTarget;

            _driver = options.Driver;

            _app = Initialize(ref initInfo);

            if (options.WindowHandle != IntPtr.Zero)
            {
                var mainViewId = CreateView(0, 0, -1);

                _renderTargets[0] = new RenderTargetBind { ViewId = mainViewId, RenderTargetId = -1 };

                SetDefaultRenderTarget();
            }
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
                    HdrColorBuffer = FlQualityLevel.MEDIUM
                },
                AntiAliasing = FlAntiAliasing.NONE,
                PostProcessingEnabled = false,
                ShadowingEnabled = false,
                ShadowType = FlShadowType.PCF,
                BlendMode = FlBlendMode.OPAQUE,
                SampleCount = _sampleCount,
                StencilBufferEnabled = false,
                FrustumCullingEnabled = false,
                ScreenSpaceRefractionEnabled = false,
                Viewport = new Rect2I() { Width = width, Height = height },
                RenderTargetId = renderTargetId
            };

            return AddView(_app, ref viewOpt);
        }

        public void SetRenderTarget(uint width, uint height, IntPtr imageId, FlTextureInternalFormat format)
        {
            if (!_renderTargets.TryGetValue(imageId, out var rtBind))
            {
                var options = new RenderTargetOptions()
                {
                    Width = width,
                    Height = height,
                    SampleCount = 1,
                    TextureId = imageId,
                    Format = format,
                    Depth = _renderTargetDepth
                };

                rtBind = new RenderTargetBind
                {
                    RenderTargetId = AddRenderTarget(_app, ref options)
                };

                var viewBind = _oneViewPerTarget ?
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
                _renderTargets[imageId] = rtBind;
            }

            _activeRenderTarget = rtBind;
        }

        public void ReleaseContext(bool release)
        {
            FilamentLib.ReleaseContext(_app, release);
            Thread.Sleep(100);
        }

        protected uint GetOrCreate<T>(T obj, Action<uint> factory) where T : EngineObject
        {

            if (!_content!.Objects!.ContainsKey(obj))
            {
                factory(obj.Id);
                _content!.Objects[obj] = obj.Id;
            }
            return obj.Id;
        }

        internal void FreeTexture(TextureInfo info)
        {
            if (info.Data.Data != 0)
            {
                Marshal.FreeHGlobal(info.Data.Data);
                info.Data.Data = 0;
            }
        }

        internal TextureInfo ToTextureInfo(Texture2D? texture)
        {
            var result = new TextureInfo();

            if (texture != null)
            {
                result.Width = texture.Width;
                result.Height = texture.Height;
                result.Levels = 100;

                switch (texture.Format)
                {
                    case TextureFormat.Rgba32:
                        result.InternalFormat = FlTextureInternalFormat.RGBA8;
                        break;
                    case TextureFormat.SRgba32:
                        result.InternalFormat = FlTextureInternalFormat.SRGB8_A8;
                        break;
                    default:
                        throw new NotSupportedException();
                }

                if (texture.Data != null)
                {
                    var mainData = texture.Data[0];

                    result.Data.Type = FlPixelType.UBYTE;
                    result.Data.DataSize = (uint)mainData.Data!.Length;

                    switch (mainData.Format)
                    {
                        case TextureFormat.Rgba32:
                        case TextureFormat.SRgba32:
                            result.Data.Format = FlPixelFormat.RGBA;
                            result.Data.Data = Marshal.AllocHGlobal(mainData.Data.Length);
                            Marshal.Copy(mainData.Data, 0, result.Data.Data, mainData.Data.Length);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            return result;
        }

        protected unsafe void BuildContent(Scene scene)
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
                if (obj is SunLight sun)
                {
                    GetOrCreate(sun, id =>
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
                            CastShadows = true,
                        };
                        AddLight(_app, id, ref info);
                    });
                }
                else if (obj is DirectionalLight dir)
                {
                    GetOrCreate(dir, id =>
                    {
                        var info = new LightInfo
                        {
                            Type = FlLightType.Directional,
                            Direction = dir.Direction,
                            Intensity = dir.Intensity,
                            Color = dir.Color,
                            CastShadows = true,
                        };
                        AddLight(_app, id, ref info);
                    });
                }
                else if (obj is Group3D group)
                {
                    GetOrCreate(group, groupId =>
                    {
                        AddGroup(_app, groupId);
                        if (group.Parent is not Scene)
                            SetObjParent(_app, groupId, group.Parent!.Id);
                    });
                }
                else if (obj is TriangleMesh mesh)
                {
                    if (mesh.Materials.Count == 0 || mesh.Geometry == null || mesh.Materials[0] is not PbrMaterial)
                        continue;

                    GetOrCreate(mesh, meshId =>
                    {
                        var geoId = GetOrCreate(mesh.Geometry!, geoId =>
                        {
                            var geo = mesh.Geometry;

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

                                    AddGeometry(_app, geoId, ref geoInfo);
                                }
                            }
                        });

                        var matId = GetOrCreate(mesh.Materials[0], matId =>
                        {
                            var mat = (PbrMaterial)mesh.Materials[0];

                            var matInfo = new MaterialInfo
                            {
                                NormalMap = ToTextureInfo(mat.NormalTexture),
                                Color = mat.MetallicRoughness?.BaseColorFactor ?? Color.White,
                                BaseColorMap = ToTextureInfo(mat.MetallicRoughness?.BaseColorTexture),
                                MetallicRoughnessMap = ToTextureInfo(mat.MetallicRoughness?.MetallicRoughnessTexture),
                                AoMap = ToTextureInfo(mat.OcclusionTexture),
                                MetallicFactor = mat.MetallicRoughness?.MetallicFactor ?? 1,
                                RoughnessFactor = mat.MetallicRoughness?.RoughnessFactor ?? 1,
                                NormalScale = mat.NormalScale,
                                AoStrength = mat.OcclusionStrength,
                                Blending = mat.Alpha switch
                                {
                                    PbrMaterial.AlphaMode.Opaque => FlBlendingMode.OPAQUE,
                                    PbrMaterial.AlphaMode.Blend => FlBlendingMode.TRANSPARENT,
                                    PbrMaterial.AlphaMode.Mask => FlBlendingMode.MASKED,
                                    _ => throw new NotSupportedException()
                                },
                                EmissiveFactor = mat.EmissiveFactor,
                                EmissiveMap = ToTextureInfo(mat.EmissiveTexture),
                                EmissiveStrength = 1,
                                MultiBounceAO = true,
                                SpecularAntiAliasing = true,
                                ScreenSpaceReflection = true,
                                AlphaCutoff = mat.AlphaCutoff,
                                DoubleSided = mat.DoubleSided,
                                SpecularAO = FlSpecularAO.Simple
                            };

                            AddMaterial(_app, mat.Id, ref matInfo);

                            //FreeTexture(matInfo.NormalMap);
                            //FreeTexture(matInfo.BaseColorMap);
                            //FreeTexture(matInfo.MetallicRoughnessMap);
                            //FreeTexture(matInfo.AoMap);
                        });

                        var meshInfo = new MeshInfo
                        {
                            CastShadows = true,
                            Culling = false,
                            Fog = false,
                            GeometryId = geoId,
                            MaterialId = matId,
                            ReceiveShadows = true
                        };

                        AddMesh(_app, meshId, ref meshInfo);

                        if (mesh.Parent is not Scene)
                            SetObjParent(_app, meshId, mesh.Parent!.Id);
                    });
                }
            }
        }


        public unsafe void Render(Scene scene, Camera camera, Rect2I viewport, bool flush)
        {
            if (_content == null || _content.Scene != scene || _content.Version != scene.Version)
                BuildContent(scene);

            var render = stackalloc RenderTarget[1];
            var persp = (PerspectiveCamera)camera;

            render[0].Camera = new CameraInfo
            {
                Far = camera.Far,
                Near = camera.Near,
                Projection = camera.Projection,
                Transform = camera.WorldMatrix
            };

            if (persp.Eyes != null)
            {
                render[0].Camera.IsStereo = true;

                render[0].Camera.Eye1 = new CameraEyesInfo
                {
                    RelTransform = persp.Eyes[0].Transform,
                    Projection = persp.Eyes[0].Projection
                };

                render[0].Camera.Eye2 = new CameraEyesInfo
                {
                    RelTransform = persp.Eyes[1].Transform,
                    Projection = persp.Eyes[1].Projection
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

            foreach (var mesh in _content!.Objects!.Where(a => a.Key is TriangleMesh))
                SetObjTransform(_app, mesh.Value, ((Object3D)mesh.Key).WorldMatrix);

            FilamentLib.Render(_app, render, 1, flush);

            _viewport = viewport;

        }

        public FlBackend Driver => _driver;

        public Rect2I View => _viewport;
    }
}
