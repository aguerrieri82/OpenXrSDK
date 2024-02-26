
using SkiaSharp;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static Xr.Engine.Filament.FilamentLib;

namespace Xr.Engine.Filament
{
    public class FilamentOptions
    {
        public IntPtr GlCtx;
        public IntPtr HWnd;
        public string? MaterialCachePath;
    }

    public class FilamentRender : IRenderEngine
    {
        protected class Content
        {
            public Scene? Scene;
            public int Version;
            public Dictionary<EngineObject, uint>? Objects;
        }

        protected Rect2I _view;
        protected IntPtr _app;
        protected uint _viewId;
        protected Dictionary<IntPtr, int> _renderTargets = [];
        protected int _activeRenderTarget;
        protected Content? _content;



        public FilamentRender(FilamentOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.MaterialCachePath))
                Directory.CreateDirectory(options.MaterialCachePath);
     


            var initInfo = new InitializeOptions()
            {
                Driver = FlBackend.OpenGL,
                WindowHandle = options.HWnd,
                Context = options.GlCtx,
                MaterialCachePath = options.MaterialCachePath ?? string.Empty
            };

            var viewOpt = new ViewOptions
            {
                renderQuality= new RenderQuality
                {
                    HdrColorBuffer= FlQualityLevel.HIGH
                },
                antiAliasing = FlAntiAliasing.NONE,
                postProcessingEnabled = false,
                shadowingEnabled = false,
                blendMode = FlBlendMode.OPAQUE,
                sampleCount = 1,
                stencilBufferEnabled = false,
                frustumCullingEnabled = false,
                screenSpaceRefractionEnabled = false,
            };

            _app = Initialize(ref initInfo);
            _viewId = AddView(_app, ref viewOpt);
            _activeRenderTarget = -1;
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
            _activeRenderTarget = -1;
        }

        public void SetRenderTarget(uint width, uint height, IntPtr imageId)
        {
            if (_renderTargets.TryGetValue(imageId, out var rtIndex))
            {
                var options = new RenderTargetOptions()
                {
                    Width = width,
                    Height = height,
                    SampleCount = 1,
                    TextureId = imageId
                };

                rtIndex = AddRenderTarget(_app, ref options);
                _renderTargets[imageId] = rtIndex;
            }

            _activeRenderTarget = rtIndex;
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
                if (obj is DirectionalLight dir)
                {
                    GetOrCreate(dir, id =>
                    {
                        var info = new LightInfo
                        {
                            Type = FlLightType.Directional,
                            Direction = dir.Forward,
                            Intensity = dir.Intensity,  
                            Color = dir.Color,  
                            CastShadows= true,
                        };
                        AddLight(_app, id, ref info);
                    });
                }
                else if (obj is Group group)
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
                                MultiBounceAO = true,
                                SpecularAntiAliasing = true,
                                ScreenSpaceReflection = true,
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

        public unsafe void Render(Scene scene, Camera camera, Rect2I view)
        {
            if (_content == null || _content.Scene != scene || _content.Version != scene.Version)
                BuildContent(scene);

            
            var render = stackalloc RenderTarget[1];

            render[0].Camera = new CameraInfo
            {
                Far = camera.Far,
                Near = camera.Near,
                Projection = camera.Projection,
                Transform = camera.WorldMatrix
            };

            render[0].RenderTargetId = _activeRenderTarget;
            render[0].ViewId = _viewId;
            render[0].Viewport = view;

            foreach (var mesh in _content!.Objects!.Where(a=> a.Key is TriangleMesh))
                SetObjTransform(_app, mesh.Value, ((Object3D)mesh.Key).WorldMatrix);

            FilamentLib.Render(_app, render, 1);

            _view = view;

        }


        public Rect2I View => _view;
    }
}
