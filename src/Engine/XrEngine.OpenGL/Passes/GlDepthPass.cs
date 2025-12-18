using XrMath;

using System.Numerics;


#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;

#endif


namespace XrEngine.OpenGL
{

    public class GlDepthPass : GlBaseSingleMaterialPass, IDepthCullProvider
    {
        readonly GlComputeProgram _depthPyramid;
        readonly GlComputeProgram _depthCull;
        GlTexture? _depthTexture;
        readonly GlBuffer<DepthObjectData> _depthData;
        private long _lastContentVersion;

        public GlDepthPass(OpenGLRender renderer)
            : base(renderer)
        {
            UseOcclusionQuery = true;
            UseDepthCull = false;
            OnlyLargeOccluder = true;

            _useInstanceDraw = true;

            _depthPyramid = new GlComputeProgram(renderer.GL, "image/depth_pyramid.comp", str => Embedded.GetString<Material>(str));
            _depthPyramid.Build();

            _depthCull = new GlComputeProgram(renderer.GL, "depth_cull.comp", str => Embedded.GetString<Material>(str));
            _depthCull.Build();

            _depthData = new GlBuffer<DepthObjectData>(renderer.GL, BufferTargetARB.ShaderStorageBuffer);

            _lastContentVersion = -1;
        }

        protected override bool BeginRender(Camera camera)
        {
            _renderer.RenderTarget!.Begin(camera);
            _renderer.State.SetWriteDepth(true);

            _gl.Clear(ClearBufferMask.DepthBufferBit);
            _gl.DepthFunc(DepthFunction.Less);

            return base.BeginRender(camera);
        }

        protected override void EndRender()
        {
            if (UseDepthCull && _renderer.UpdateContext.PassCamera!.ActiveEye == 0)
            {
                UpdateDepthPyramid();
                UpdateVisibility();
            }
        }

        protected override bool CanDraw(DrawContent draw)
        {
            if (OnlyLargeOccluder && !draw.Object!.Is(EngineObjectFlags.LargeOccluder))
                return false;
            return base.CanDraw(draw);
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return new ColorMaterial
            {
                WriteColor = false,
                UseDepth = true,
                WriteDepth = true
            };
        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.Opaque).Take(1);
        }

        protected override void Draw(DrawContent draw)
        {
            if (UseOcclusionQuery)
            {
                draw.Query ??= draw.Object!.GetOrCreateProp(OpenGLRender.Props.GlQuery, () => new GlQuery(_gl));
                draw.Query!.Begin(QueryTarget.AnySamplesPassed);
                draw.Draw!();
                draw.Query.End();
            }
            else
                draw.Draw!();
        }


        protected bool UpdateDepthPyramid()
        {
            var provider = (IGlFrameBufferProvider)_renderer.RenderTarget!;

            var curDepth = provider.FrameBuffer.Depth!;
            if (curDepth == null)
                return false;

            _depthTexture ??= new GlTexture(_gl)
            {
                IsMutable = true,
                MaxLevel = 20,
                MagFilter = TextureMagFilter.Linear,
                MinFilter = TextureMinFilter.Linear,
                WrapS = TextureWrapMode.ClampToBorder,
                WrapT = TextureWrapMode.ClampToBorder,
                BorderColor = Color.White
            };



            if (_depthTexture.Width != curDepth.Width || _depthTexture.Height != curDepth.Height)
            {
                _depthTexture.Update(0, new TextureData
                {
                    Width = curDepth.Width,
                    Height = curDepth.Height,
                    Format = TextureFormat.GrayFloat32,
                    MipLevel = 0,
                });
            }

            GlImageProc.Instance.CopyDepth(provider.FrameBuffer, _depthTexture);

            var w = _depthTexture.Width;
            var h = _depthTexture.Height;
            var level = 1;

            _depthPyramid.Use();

            while (!(w == 1 && h == 1))
            {
                _gl.BindImageTexture(0, _depthTexture, level - 1, false, 0, BufferAccessARB.ReadOnly, _depthTexture.InternalFormat);
                _gl.BindImageTexture(1, _depthTexture, level, false, 0, BufferAccessARB.WriteOnly, _depthTexture.InternalFormat);

                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);

                var groupsX = (w + 7) / 8;
                var groupsY = (h + 7) / 8;

                _gl.DispatchCompute(groupsX, groupsY, 1);
                _gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                if (groupsX == 1 && groupsY == 1)
                    break;

                level++;
            }
            _depthTexture.MaxLevel = (uint)level - 1;

            _renderer.State.SetActiveProgram(0);

            return true;
        }

        protected unsafe void UpdateVisibility()
        {
            var contVersion = SelectLayers().OfType<GlLayerV2>().Sum(a => a.Version);

            if (contVersion != _lastContentVersion)
            {
                var draws = SelectLayers().OfType<GlLayerV2>()
                   .SelectMany(a => a.Content.Contents.Values)
                   .SelectMany(a => a.Contents.Values)
                   .SelectMany(a => a.Contents.Values)
                   .SelectMany(a => a.Contents);

                var count = draws.Count();
                if (count != _depthData.ArrayLength)
                    _depthData.Allocate((uint)(sizeof(DepthObjectData) * count));

                var pData = _depthData.Map(MapBufferAccessMask.WriteBit | MapBufferAccessMask.InvalidateBufferBit);

                var i = 0;
                foreach (var draw in draws)
                {
                    var bounds = draw.Object!.WorldBounds;
                    pData[i].BoundsMin = bounds.Min;
                    pData[i].BoundsMax = bounds.Max;
                    pData[i].IsVisible = true;
                    pData[i].IsCulled = false;
                    pData[i].Extent = Vector2.One;

                    if (draw.Id != i)
                    {
                        draw.Id = i;
                        draw.InstanceVersion = -1;
                    }

                    i++;
                }

                _depthData.Unmap();

                _lastContentVersion = contVersion;
            }


            _depthCull.Use();

            _gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, _depthData);

            var camera = _renderer.UpdateContext.PassCamera!;
            var planes = new Plane[6];
            camera.FrustumPlanes(planes);

            _renderer.State.LoadTexture(_depthTexture!, 0);
            _depthCull.SetUniform("viewProj", camera.ViewProjection);
            _depthCull.SetUniform("screenSize", new Vector2(camera.ViewSize.Width, camera.ViewSize.Height));
            _depthCull.SetUniform("maxMip", (int)_depthTexture!.MaxLevel);
            _depthCull.SetUniform("planes", planes);

            var groupsX = (_depthData.ArrayLength + 63) / 64;

            _gl.DispatchCompute(groupsX, 1, 1);

            _gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);

            /*
            pData = _depthData.Map(MapBufferAccessMask.ReadBit);

            i = -1;

            foreach (var draw in draws)
            {
                i++;
                if (draw.Object is LineMesh)
                    continue;

                draw.IsClipped = !pData[i].IsVisible || pData[i].IsCulled;
                draw.DepthData = pData[i];
            }

            _depthData.Unmap();
            */

            _renderer.State.SetActiveProgram(0);
        }


        bool IDepthCullProvider.IsActive => UseDepthCull;

        public bool OnlyLargeOccluder { get; set; }

        public bool UseOcclusionQuery { get; set; }

        public bool UseDepthCull { get; set; }

        public IBuffer<DepthObjectData> DepthCullBuffer => _depthData;
    }
}
