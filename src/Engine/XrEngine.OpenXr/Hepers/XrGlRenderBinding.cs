#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using OpenXr.Framework.Angle;
using Silk.NET.OpenXR;
using XrMath;
using XrEngine.OpenGL;


namespace XrEngine.OpenXr
{
    public unsafe class XrGlRenderBinding
    {
        private readonly XrApp _xrApp;
        private readonly EngineApp _app;
        private readonly bool _useAngle;
        private OpenGLRender _renderer;

        private readonly XrProjDepthMode _depthMode;
        private readonly GlRenderTargetPool? _targetPool;

        private AngleVulkanContext? _vulkanCtx;
        private GlDepthExportPass? _depthExportPass;
        private GlDepthCopyPass? _depthCopyPass;


        public XrGlRenderBinding(XrApp xrApp, EngineApp app, XrProjDepthMode depthMode, bool useAngle)
        {
            _xrApp = xrApp;
            _app = app;
            _useAngle = useAngle;
            _depthMode = depthMode;

            _renderer = GetRenderer();

            _targetPool = new GlRenderTargetPool(_renderer.GL, xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView)
            {
                Name = "Main"
            };

            _xrApp.SessionChanged += OnSessionChanged;

            CreateProjectionLayer();
        }

        protected virtual void OnSessionChanged(object? sender, EventArgs e)
        {
            if (_xrApp.State == XrAppState.Stopped)
                _targetPool?.Clear();
        }

        protected XrProjectionLayer CreateProjectionLayer()
        {
            XrProjectionLayer projLayer;

            if (_renderer.Options.MotionVectorMode == MotionVectorMode.Pass)
            {
                var motionVectorPass = _renderer.EnsurePass(() => new GlMotionVectorPass(
                    _renderer, _xrApp,
                    _xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView));

                var provider = new GlMotionVectorProviderPass(_app, _renderer, motionVectorPass);

                projLayer = _xrApp.Layers.AddProjectionSpaceWarp(RenderView, provider);
            }

            else if (_renderer.Options.MotionVectorMode == MotionVectorMode.Shared)
            {
                var provider = new GlMotionVectorProviderShared(_app, _renderer);

                projLayer = _xrApp.Layers.AddProjectionSpaceWarp(RenderView, provider);
            }
            
            else
                projLayer = _xrApp.Layers.AddProjection(RenderView, _xrApp.RenderOptions.UseProjectionDepth);

            projLayer.UseIntermediate = _renderer.Options.NeedPostProcess;

            return projLayer;
        }

        protected OpenGLRender GetRenderer()
        {
            if (_app.HasRenderer)
                return (OpenGLRender)_app.Renderer;

            var driver = _xrApp.Plugin<IXrGraphicDriver>();

            if (driver is not IApiProvider apiProvider)
                throw new NotSupportedException();

            var gl = apiProvider.GetApi<GL>() ??
                throw new NotSupportedException();

            var renderer = new OpenGLRender(gl);
            _app.Renderer = renderer;

            return renderer;
        }

        private (uint Color, uint Depth) GetImages(ref RenderViewsInfo info, int swapIndex)
        {
            if (!_useAngle)
            {
                return (
                    ((SwapchainImageOpenGLKHR*)info.ColorImages[swapIndex])->Image,
                    info.DepthImages == null ? 0 : ((SwapchainImageOpenGLKHR*)info.DepthImages[swapIndex])->Image);
            }

            _vulkanCtx ??= Context.Require<AngleVulkanContext>();

            var colorImg = _vulkanCtx.AttachVulkanImage(info.ColorImages[swapIndex], info.Color[0]);

            if (info.DepthImages == null || info.Depth == null)
                return (colorImg.Texture, 0);

            var depthImg = _vulkanCtx.AttachVulkanImage(info.DepthImages[swapIndex], info.Depth[0]);

            return (colorImg.Texture, depthImg.Texture);
        }

        protected IGlRenderTargetFB SetupRenderTarget(ref RenderViewsInfo info, PerspectiveCamera camera, int swapIndex)
        {
            var images = GetImages(ref info, swapIndex);

            var renderTarget = CreateRenderTarget(images.Color, images.Depth);

            _renderer.SetRenderTarget(renderTarget);

            if (XrDevice.IsMetaQuest)
            {
                info.RenderedSize = info.Layer.GetRecommendedResolution(info.DisplayTime);

                if (info.RenderedSize != null)
                    renderTarget.RenderSize = new Size2I((uint)info.RenderedSize.Value.Width, (uint)info.RenderedSize.Value.Height);
                else
                    renderTarget.RenderSize = new Size2I((uint)info.Color[0].Size.Width, (uint)info.Color[0].Size.Height);
            }

            return renderTarget;
        }

        protected IGlRenderTargetFB CreateRenderTarget(uint colorTex, uint depthTex)
        {
            var sampleCount = _xrApp.RenderOptions.SampleCount;
            var isMultiView = _xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView;

            var isHandleDepth = depthTex != 0 && sampleCount > 1;

            if (isHandleDepth)
            {
                if (_depthMode == XrProjDepthMode.DepthPass)
                {
                    _depthExportPass ??= _renderer.EnsurePass(() => new GlDepthExportPass(_renderer, isMultiView));

                    _depthExportPass.Configure(depthTex);
                }
                
                else if (_depthMode == XrProjDepthMode.DepthCopy)
                {
                    _depthCopyPass ??= _renderer.EnsurePass(() => new GlDepthCopyPass(_renderer, isMultiView, imageMode: false));

                    _depthCopyPass.Configure(depthTex);

                    _renderer.UpdateContext.UseCopyDepth = true;
                }

                else if (_depthMode == XrProjDepthMode.DepthCopyImage)
                {
                    _depthCopyPass ??= _renderer.EnsurePass(() => new GlDepthCopyPass(_renderer, isMultiView, imageMode: true));

                    _renderer.UpdateContext.CopyDepthImage = (Texture2D?)_depthCopyPass
                        .Configure(depthTex)?
                        .ToEngineTexture();
                }

                depthTex = 0;
            }

            var renderTarget = _targetPool!.GetRenderTarget(colorTex, depthTex, sampleCount);

            if (_depthMode == XrProjDepthMode.DepthCopy && isHandleDepth)
            {
                renderTarget.FrameBuffer.GetOrCreateEffect(FramebufferAttachment.ColorAttachment1, TextureFormat.Gray16);

                renderTarget.FrameBuffer.BindDraw(DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1);

                _renderer.State.SetWriteColor(true);
                _renderer.State.EnableFeature(EnableCap.Blend, false);

                _renderer.GL.ClearBuffer(BufferKind.Color, 1, [0f]);
            }

            return renderTarget;
        }

        protected void UpdateCamera(ref RenderViewsInfo info, PerspectiveCamera camera)
        {
            camera.Eyes ??= new CameraEye[2];
            camera.IsStereo = true;
            camera.IsMultiView = info.Mode == XrRenderMode.MultiView;
            camera.Transform.Version++;

            var eyes = camera.Eyes;
            var referenceFrame = XrApp.Current!.ReferenceFrame.ToMatrix();

            for (var i = 0; i < info.ProjViews.Length; i++)
            {
                XrCameraTransform transform;

                if (info.Layer.UseSimmetricFov)
                    transform = XrCameraTransform.FromView(info.ProjViews[i].Pose.ToPose3(), info.SharedFov, camera.Near, camera.Far);
                else
                    transform = XrCameraTransform.FromView(info.ProjViews[i], camera.Near, camera.Far);

                eyes[i].World = transform.World * referenceFrame;
                eyes[i].Projection = transform.Projection;
                eyes[i].View = eyes[i].World.Invert();
                eyes[i].ViewProj = eyes[i].View * eyes[i].Projection;
                eyes[i].ViewProjInv = eyes[i].ViewProj.Invert();

                var depth = (CompositionLayerDepthInfoKHR*)StructChain.FindNextStruct(
                    ref info.ProjViews[i], StructureType.CompositionLayerDepthInfoKhr);

                if (depth != null)
                {
                    depth->NearZ = camera.Near;
                    depth->FarZ = camera.Far;
                }
            }
        }

        protected void UpdateClipRegion(ref RenderViewsInfo info, IGlRenderTargetFB renderTarget, int viewIndex)
        {
            if (info.Layer.UseSimmetricFov)
            {
                if (renderTarget.ClipRegions == null || renderTarget.ClipRegions.Length != 2)
                    renderTarget.ClipRegions = new Rect2I[2];

                var w = renderTarget.RenderSize.Width;
                var h = renderTarget.RenderSize.Height;

                var cropW = (uint)MathF.Round(w / info.CropScale.X);
                var x = viewIndex == 0 ? 0 : w - cropW;

                renderTarget.ClipRegions[viewIndex] = new Rect2I((int)x, 0, cropW, h);
            }
            else
                renderTarget.ClipRegions = null;
        }

        protected void RenderView(ref RenderViewsInfo info)
        {
            var camera = (PerspectiveCamera)_app.ActiveScene!.ActiveCamera!;

            UpdateCamera(ref info, camera);

            var eyes = camera.Eyes!;

            if (info.Mode == XrRenderMode.SingleEye)
            {
                _app.BeginFrame();

                for (var i = 0; i < info.ColorImages.Length; i++)
                {
                    var renderTarget = SetupRenderTarget(ref info, camera, i);

                    camera.Projection = eyes[i].Projection;
                    camera.WorldMatrix = eyes[i].World;
                    camera.ActiveEye = i;

                    UpdateClipRegion(ref info, renderTarget, i);

                    _app.RenderScene();
                }

                _app.EndFrame();
            }
            else if (info.Mode == XrRenderMode.MultiView)
            {
                var renderTarget = SetupRenderTarget(ref info, camera, 0);

                camera.Projection = eyes[0].Projection;
                camera.WorldMatrix = eyes[0].World.InterpolateWorldMatrix(eyes[1].World, 0.5f);
                camera.ActiveEye = -1;

                UpdateClipRegion(ref info, renderTarget, 0);
                UpdateClipRegion(ref info, renderTarget, 1);

                _app.RenderFrame();
            }
        }

        public OpenGLRender Renderer => _renderer;  
    }
}
