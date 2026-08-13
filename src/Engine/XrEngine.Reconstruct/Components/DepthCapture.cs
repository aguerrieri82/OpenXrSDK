#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;
using OpenXr.Framework;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using XrEngine.Devices;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrMath;
using TurboJpeg;

namespace XrEngine.Reconstruct
{
    public enum DepthSnapeshotMode
    {
        Read,
        Capture,
        Record
    }

    public class CaptureFrame : BaseComponent<TriangleMesh>, ISelectionHandler
    {
        public CaptureFrame()
        {
            Exposure = 0;
        }

        [Action]
        public void GoToPose()
        {
            Debug.Assert(_host?.Scene?.ActiveCamera != null);
            Debug.Assert(Meta != null);

            _host.Scene.ActiveCamera.View = Meta.CameraView;
            _host.Scene.ActiveCamera.Projection = Meta.CameraProj;
            _host.Scene.ActiveCamera.Far = 100;
        }

        public void OnSelected(Object3D obj, bool isSelected)
        {
            _host!.Materials[1].IsEnabled = isSelected && ShowWireFrame;
        }

        public Texture2D? Texture { get; set; }

        public bool ShowWireFrame { get; set; }

        [Range(-1, 1, 0.01f)]
        public float Exposure { get; set; }

        public DepthCapture.DepthFrameMeta? Meta { get; set; }

        public IMemoryBuffer<byte>? ColorData;
    }

    #region COMMENT

    /*
     * DEPTH SNAPSHOT RECONSTRUCTION PIPELINE
     * ======================================
     *
     * This behavior is the coordinator for turning a set of Quest depth/color snapshots into a usable
     * reconstructed scene mesh.
     *
     * The important mental model is:
     *
     *      many partial depth frames
     *          -> many partial meshes
     *          -> one fused mesh
     *          -> cleanup / simplification
     *          -> optional UV unwrap
     *          -> optional baked color atlas
     *
     *
     * FRAME DATA
     * ----------
     *
     * Each captured frame contains:
     *
     *   - a depth-derived mesh patch
     *   - the RGB camera image
     *   - camera view/projection metadata
     *   - depth view/projection metadata
     *
     * In Record mode, this is persisted as raw files on disk.
     * In Read mode, those files are loaded back into per-frame TriangleMesh objects.
     *
     * The per-frame meshes are useful for preview/debugging, but they are not the final mesh. They are
     * input samples for the reconstruction stage.
     *
     *
     * *
     * DEPTH PATCH EXTRACTION
     * ----------------------
     *
     * Before voxel fusion, each recorded depth frame is converted into a temporary depth patch mesh.
     * This is done by DepthGeometryGenerator.
     *
     * The generator does not try to reconstruct the whole scene. It only turns one depth image into one
     * local piece of observed surface:
     *
     *      raw depth map
     *          -> sampled depth grid
     *          -> unprojected world points
     *          -> depth-grid triangles
     *          -> per-frame mesh patch
     *
     * Each sampled depth value is unprojected through the depth camera matrix to recover a 3D world point.
     * The same world point is then projected into the RGB camera to compute the color UV for that vertex.
     *
     * So every generated vertex already contains:
     *
     *      position = world-space point from depth
     *      uv       = where that point lands in the RGB image
     *
     * The output patch is still a rough depth-grid mesh. It can contain missing regions, jagged borders,
     * discontinuity artifacts and duplicated surfaces relative to other frames. That is expected. The patch
     * is only input evidence for the later voxel reconstruction.
     *
     *
     * DEPTH PATCH FILTERS
     * -------------------
     *
     * The generator filters bad samples before they reach the voxel stage. This is important because voxel
     * fusion can absorb small noise, but it should not be fed obviously wrong triangles.
     *
     * Vertex UV filter:
     *
     *      Rejects vertices whose projected RGB UV is outside the camera image.
     *
     * This removes depth points that exist in the depth frame but cannot be colored by the paired RGB frame.
     * It also avoids later projection using clamped/invalid color pixels.
     *
     * Triangle validity filter:
     *
     *      A grid quad emits two triangles only when all three vertices are valid.
     *
     * This naturally cuts holes around missing depth samples or points outside the RGB image.
     *
     * World-area filter:
     *
     *      Rejects triangles that are almost zero-area in 3D.
     *
     * These are usually degenerate depth samples or numerical debris. Keeping them adds noise but no useful
     * surface information.
     *
     * UV-area filter:
     *
     *      Rejects triangles that collapse to almost zero area in RGB image space.
     *
     * Those triangles cannot be textured reliably because a large or thin 3D region would sample almost the
     * same RGB pixel.
     *
     * World/UV area-ratio filter:
     *
     *      Rejects triangles whose 3D area and RGB-projected area are inconsistent.
     *
     * The current implementation effectively tests:
     *
     *      worldArea / uvArea
     *
     * This catches the classic bad depth-grid triangles:
     *
     *      - long triangles stretched across depth discontinuities
     *      - grazing triangles that would smear color
     *      - triangles joining foreground/background surfaces
     *      - projection artifacts where a large 3D surface maps to a tiny color region
     *
     * These filters are not meant to make the patch perfect. They only remove the worst input triangles
     * before voxel fusion. Later stages still handle the real cleanup:
     *
     *      voxel fusion        -> merges overlapping frame evidence
     *      vertex collapse     -> removes near-duplicates / turns soup into topology
     *      UV unwrap/projection -> builds final texture space
     *
     * GeneratorParams controls this extraction stage. If the final reconstruction has flying curtains or
     * smeared color from depth edges, tune the patch filters first. If the final mesh is simply too dense
     * or too coarse, tune MeshParams / voxel size instead.
     *
     *
     * GEOMETRY RECONSTRUCTION
     * -----------------------
     *
     * The final mesh is produced by VoxelMeshReconstructor.
     *
     * Conceptually, every input depth mesh contributes surface evidence into a voxel/spatial structure.
     * The extractor then produces a single reconstructed mesh from that fused spatial representation.
     *
     * This is different from simply keeping all frame meshes:
     *
     *   - overlapping captures are merged
     *   - duplicate surfaces are reduced
     *   - small frame-to-frame inconsistencies are absorbed by the voxel resolution
     *   - the result becomes one persistent scene mesh
     *
     * MeshParams controls this stage. The most important parameter is voxel size:
     *
     *   - smaller voxel size: more detail, more vertices, more noise, slower
     *   - larger voxel size: smoother/coarser, fewer vertices, faster
     *
     * UseMeshCache can skip this expensive reconstruction step by loading reconstruct.obj.
     * Disable the cache when changing reconstruction parameters or when the source frames changed.
     *
     *
     * OPTIMIZATION / CLEANUP
     * ----------------------
     *
     * After extraction the mesh can still contain too many vertices and near-duplicates.
     *
     * The Optimize path first computes/welds indices and then collapses close vertices. This is important
     * because topology-based operations are meaningless while the mesh is still effectively triangle soup.
     *
     * OptimizeTollerance controls how aggressively nearby vertices are merged:
     *
     *   - too small: keeps noise and duplicate seams
     *   - too large: destroys detail, creates wrong joins, can damage UV unwrap later
     *
     * MeshOptimizer is applied later as a rendering/index-buffer optimization pass. It should not be
     * confused with geometric cleanup: it improves runtime mesh layout, not reconstruction quality.
     *
     *
     *
     * TWO TEXTURING STRATEGIES
     * ------------------------
     *
     * 1) UnwrapUv == false
     *
     *    Legacy projection mode.
     *    MeshTextureProjection assigns capture-frame/color choices directly to the geometry.
     *
     *    This path can also run FrameExposureSolver. That solver estimates per-frame exposure corrections
     *    so frames captured under different auto-exposure levels blend/look more consistent.
     *
     *    Output can be:
     *
     *      - a packed old-style atlas
     *      - or a texture-array material with per-frame exposure values
     *
     * 2) UnwrapUv == true
     *
     *    Preferred final mode.
     *    MeshUvUnwrapper creates a real atlas UV layout for the fused mesh.
     *    Capture colors are projected into that atlas using weighted accumulation, then resolved.
     *
     *    Output:
     *
     *      - one reconstructed mesh
     *      - one normal texture atlas
     *      - normal TextureMaterial path
     *      
     *      
     * UV UNWRAP
     * ---------
     *
     * The UV unwrap stage should group connected, reasonably planar surface regions into charts.
     *
     * The goal is not perfect artist-quality UVs. The goal is a stable atlas that:
     *
     *   - avoids triangle-per-island output
     *   - avoids UV overlaps
     *   - keeps connected surfaces together when reasonable
     *   - leaves enough padding for filtering/dilation
     *
     * UvUnwrapParams controls chart creation, chart merging, packing and padding.
     *
     * Important rule:
     *
     *      disconnected coplanar surfaces are dangerous to merge
     *
     * because projection to a chart plane can collapse unrelated surfaces onto overlapping UV space.
     * Prefer solving atlas waste with better packing, not by merging unrelated geometry into the same chart.
     *
     *
     * ATLAS COLOR PROJECTION
     * ----------------------
     *
     * The atlas bake renders the final reconstructed mesh in UV space.
     *
     * For each atlas fragment we still know the original 3D world position. That world position is
     * projected into every capture camera. If a capture sees that point, its color contributes to the
     * atlas texel.
     *
     * The atlas texture during projection is not final color. It is a weighted accumulator:
     *
     *      rgb = sum(captureColor * weight)
     *      a   = sum(weight)
     *
     * Resolve later converts this to:
     *
     *      finalColor = rgb / a
     *
     *
     * VISIBILITY
     * ----------
     *
     * Wrong-wall rejection is handled by depth occlusion, not by normals.
     *
     * For each capture, the final reconstructed mesh can be rendered from that capture pose to generate
     * a depth texture. During projection, the current 3D point is compared against that depth texture.
     *
     * A point is rejected only when it is behind the visible reconstructed surface:
     *
     *      pointDepth > sampledDepth + bias
     *
     * Do not require exact depth equality. The fused/optimized mesh will not exactly match the original
     * depth samples, and exact matching creates holes.
     *
     *
     * NORMALS AND FRONTNESS
     * ---------------------
     *
     * Reconstructed normals are not always sign-coherent. Some local patches can point backwards even
     * when the surface is valid.
     *
     * For atlas baking, normal sign should not decide visibility. Use two-sided frontness:
     *
     *      abs(dot(normal, toCaptureCamera))
     *
     * Frontness is a quality term:
     *
     *   - frontal views get higher weight
     *   - grazing views get lower weight or are discarded
     *
     * Depth decides whether the point is visible.
     * Frontness decides whether the sample is useful.
     *
     *
     * DISTANCE WEIGHT
     * ---------------
     *
     * Close captures usually contain sharper image detail.
     *
     * Distance weighting biases the bake toward closer captures:
     *
     *      weight *= pow(referenceDistance / cameraDistance, power)
     *
     * Use power 0 to disable it.
     * Use power 1-2 for mild preference.
     * Use power 3+ when close captures should dominate strongly.
     *
     *
     * RESOLVE AND DILATION
     * --------------------
     *
     * Resolve converts weighted accumulation into final color and keeps alpha as a coverage mask.
     *
     * Dilation then expands valid texels into neighboring invalid texels in atlas space.
     *
     * This fixes texture-filtering cracks:
     *
     *   - padding creates empty room between UV islands
     *   - dilation fills that room with edge color
     *
     * Without dilation, bilinear filtering and mipmaps can sample black/unwritten texels just outside an
     * island and produce visible cracks.
     *
     *
     * 
     * EXPOSURE SOLVE IN THE NON-UV PATH
     * ---------------------------------
     *
     * The old non-unwrap projection path can compensate per-frame camera exposure before building the
     * final material/atlas.
     *
     * This matters because the Quest camera auto-exposure is not locked: two captures of the same wall
     * can have different brightness even if the geometry projection is correct.
     *
     * In the UnwrapUv == false path:
     *
     *   1. MeshTextureProjection chooses which capture/image contributes to each mesh region.
     *   2. FrameExposureSolver estimates relative exposure offsets between frames.
     *   3. Those exposure values are used when building the old atlas or when rendering the texture array.
     *
     * This is not the same as ResolveColorAsync().
     *
     *   - FrameExposureSolver works on source-frame brightness consistency.
     *   - ResolveColorAsync() works on the weighted UV-atlas accumulator produced by the unwrap bake.
     *
     * The current UnwrapUv == true atlas bake does not yet use that exposure solve directly. If auto-exposure
     * differences become visible in the baked atlas, the same idea should be applied before or during
     * accumulation, by weighting/adjusting each capture frame color before it contributes to the atlas.
     *
     *
     * PARAMETER GROUPS
     * ----------------
     *
     * Geometry capture / depth patch extraction:
     *
     *      GridSize
     *      GeneratorParams
     *
     * Fused mesh reconstruction:
     *
     *      MeshParams
     *      UseMeshCache
     *
     * Geometric cleanup:
     *
     *      Optimize
     *      OptimizeTollerance
     *      ComputeIndices
     *
     * UV charting/packing:
     *
     *      UvUnwrapParams
     *
     * Atlas projection and resolve:
     *
     *      UvProjParams
     *
     * Legacy direct color projection / exposure compensation:
     *
     *      ProjParams
     *      ExposeParams
     *      SolveExposure
     *      
     *      
     * COMMON MODES
     * ------------
     *
     * Fast geometry debug:
     *
     *      UseMeshCache = false when changing reconstruction
     *      BuildAtlas = false
     *      UnwrapUv can be false
     *
     * UV/debug bake:
     *
     *      UseMeshCache = true
     *      UnwrapUv = true
     *      BuildAtlas = true
     *      smaller AtlasSize
     *
     * Final bake:
     *
     *      UseMeshCache = true after geometry is stable
     *      Optimize = true
     *      UnwrapUv = true
     *      BuildAtlas = true
     *      UseDepthOcclusion = true
     *      enough dilation passes to cover filtering/mipmap seams
     *
     *
     * FAILURE MAP
     * -----------
     *
     * Too many vertices:
     *      voxel size too small, Optimize disabled, collapse tolerance too low.
     *
     * Lost detail / melted geometry:
     *      voxel size too large or OptimizeTollerance too high.
     *
     * Holes in atlas:
     *      depth bias too strict, frontness too strict, bad signed-normal rejection.
     *
     * Wrong surface color:
     *      depth occlusion disabled, depth UV convention wrong, or depth bias too permissive.
     *
     * Dark/striped atlas before resolve:
     *      looking at weighted accumulation instead of resolved color.
     *
     * Black cracks on UV borders:
     *      outside-island texels are unwritten; resolve+dilation required.
     */

    #endregion

    public class DepthCapture : Behavior<Group3D>
    {
        #region STRUCTS 

        public class DepthFrame
        {
            public TriangleMesh? Mesh;

            public Texture2D? CameraTexture;

            public Matrix4x4 DepthView;

            public Matrix4x4 DepthProj;

            public Matrix4x4 CameraView;

            public Matrix4x4 CameraProj;

            public long DepthXrTime;

            public long CameraXrTime;

            public long FrameXrTime;
        }

        public class DepthFrameMeta
        {
            public int Frame;

            public int ColorWidth;
            public int ColorHeight;

            public int DepthWidth;
            public int DepthHeight;

            public int GridWidth;
            public int GridHeight;

            public Matrix4x4 DepthView;
            public Matrix4x4 DepthProj;

            public Matrix4x4 CameraView;
            public Matrix4x4 CameraProj;

            public long DepthXrTime;
            public long CameraXrTime;
            public long FrameXrTime;
        }

        public class UvAtlasProjectionParams
        {
            public UvAtlasProjectionParams()
            {
                AtlasSize = 8192;

                MinFrontness = 0.02f;
                DepthBiasMillimeters = 5.0f;

                DistanceRef = 1.0f;
                DistanceWeightPower = 3.0f;
                MinDistanceWeight = 0.0f;

                ResolveMinWeight = 0.00001f;
                DilationPasses = 1;
            }

            /// <summary>
            /// Size of the generated UV atlas texture.
            /// Suggested: 4096 for tests, 8192 for final room-scale bake.
            /// Higher values preserve more projected image detail but make projection/resolve/dilation slower.
            /// </summary>
            public int AtlasSize { get; set; }

            /// <summary>
            /// Minimum two-sided frontness accepted during UV atlas projection.
            /// The shader should use abs(dot(normal, toCamera)) because reconstructed normals can be flipped.
            /// Suggested: 0.01-0.03 for noisy reconstructed meshes.
            /// Higher values reject grazing views but can create holes.
            /// </summary>
            public float MinFrontness { get; set; }

            /// <summary>
            /// Depth occlusion tolerance in millimeters, used only when DepthCapture.UseDepthOcclusion is enabled.
            /// Suggested: 3-5 mm.
            /// Too low creates holes because the fused mesh does not exactly match the generated depth map.
            /// Too high can allow wrong surfaces to pass near depth discontinuities.
            /// </summary>
            public float DepthBiasMillimeters { get; set; }

            /// <summary>
            /// Reference distance in meters for distance weighting.
            /// Usually keep this at 1.0.
            /// </summary>
            public float DistanceRef { get; set; }

            /// <summary>
            /// Controls how strongly close captures dominate far captures.
            /// 0 disables distance weighting.
            /// 1-2 gives mild/strong close-frame preference.
            /// 3 is aggressive and useful when close frames are visibly sharper.
            /// </summary>
            public float DistanceWeightPower { get; set; }

            /// <summary>
            /// Lower clamp for distance weight.
            /// 0 lets far captures contribute almost nothing.
            /// 0.02-0.05 keeps far captures alive for weak fill.
            /// </summary>
            public float MinDistanceWeight { get; set; }

            /// <summary>
            /// Minimum accumulated projection weight required for a texel to be valid after resolve.
            /// Typical: 0.00001.
            /// Raise only if very weak/noisy texels survive the resolve.
            /// </summary>
            public float ResolveMinWeight { get; set; }

            /// <summary>
            /// Number of atlas-space dilation passes after resolve.
            /// This fills unwritten texels around UV islands to prevent black seams from bilinear/mip sampling.
            /// 0 disables dilation.
            /// 1-2 is usually enough for bilinear cracks.
            /// 8-12 if mipmaps expose seams.
            /// </summary>
            public int DilationPasses { get; set; }
        }

        #endregion

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            IncludeFields = true,
            WriteIndented = true
        };

        private int _frameIndex;
        private SplatMesh? _splatMesh;
        private CameraController? _capture;
        private EnvDepthMesh? _envDepth;
        private XrBoolInput? _captureBtn;
        private XrBoolInput? _deleteBtn;
        private IMemoryBuffer<byte>[]? _buffers;
        private string? _lastPath;
        private TriangleMesh? _recMesh;
        private Texture2D? _colorArrayTex;
        private GlTexture? _tempDepthTex;
        private Texture2D? _atlasTex;
        private WireframeMaterial? _wireMat;
        private PbrMaterial? _colorMat;
        private Material? _texMat;
        private readonly DepthSnapeshotMode _mode;
        private readonly string _sessionPath;
        private readonly List<DepthFrame> _frames = [];

        public DepthCapture(DepthSnapeshotMode mode)
        {
            if (mode == DepthSnapeshotMode.Record)
            {
                var root = Path.Combine(XrPlatform.Current!.SharedPath, "DepthSnapshots");
                _sessionPath = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
                Directory.CreateDirectory(_sessionPath);
            }
            else
            {
                _sessionPath = "";
            }

            _mode = mode;

            GridSize = 300;
            DepthMapSize = 300;
            UseMeshCache = true;
            BuildAtlas = true;
            ComputeIndices = true;
            UseDepthOcclusion = true;
            SolveExposure = true;
            Optimize = true;
            UnwrapUv = true;
            FillHoles = true;

            MeshParams = new();
            GeneratorParams = new();
            ExposeParams = new();
            ProjParams = new();
            UvUnwrapParams = new();
            UvProjParams = new();
            CollapseParams = new();
            HoleParams = new();

            CollapseParams.Distance = 0.04f;
            MeshParams.VoxelSize = 0.05f;

            HoleParams.MaxPasses = 1;
            HoleParams.EdgeMode = HoleFillMode.ThreeEdges;
        }

        protected override void Start(RenderContext ctx)
        {
            if (_mode == DepthSnapeshotMode.Read)
                return;

            Debug.Assert(_host?.Scene != null);

            if (!_host.Scene.TryComponent(out _capture))
            {
                _capture = new CameraController();
                _host.Scene.AddComponent(_capture);
            }

            _envDepth = _host.Scene.Descendants<EnvDepthMesh>().FirstOrDefault();
            _envDepth ??= _host.AddChild(new EnvDepthMesh(new Size2I(300, 300)));

            _ = _capture.StartCameraAsync(OculusCameras.Left);
        }

        private unsafe void SaveTextureRaw(Texture2D texture, TextureFormat format, int bytesPerPixel, string path)
        {
            _buffers ??= [MemoryBuffer.Create<byte>(16), MemoryBuffer.Create<byte>(16)];

            OpenGLRender.Current!.ReadTexture(texture, format, 0, 0, _buffers);

            var size = checked((int)(texture.Width * texture.Height * bytesPerPixel));

            using var pTex = _buffers[0].MemoryLock();
            using var file = File.Create(path);

            file.Write(new ReadOnlySpan<byte>(pTex.Data, size));
        }

        private Texture2D LoadColorTextureRaw(string path, int width, int height, string name)
        {
            var texture = CreateTexture((uint)width, (uint)height);

            texture.LoadData(new TextureData
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = UseSrgb ? TextureFormat.SRgba8 : TextureFormat.Rgba8,
                Content = MemoryBuffer.Create(File.ReadAllBytes(path))
            });

            return texture;
        }

        private void SaveFrame(DepthFrame frame, DepthFrameMeta meta)
        {
            var framePath = Path.Combine(_sessionPath, $"frame_{meta.Frame:000000}");
            Directory.CreateDirectory(framePath);

            File.WriteAllText(
                Path.Combine(framePath, "meta.json"),
                JsonSerializer.Serialize(meta, _jsonOptions));

            SaveTextureRaw(
                frame.CameraTexture!,
                TextureFormat.Rgba8,
                4,
                Path.Combine(framePath, "color_rgba.raw"));

            var mat = (EnvDepthMaterial)_envDepth!.Materials[0];

            SaveTextureRaw(
                mat.LastTexture!,
                TextureFormat.Gray16,
                2,
                Path.Combine(framePath, "depth_u16.raw"));
        }

        protected Material CreateMaterial(Texture2D texture)
        {
            if (_mode == DepthSnapeshotMode.Read)
            {
                return new GridMaterial
                {
                    Priority = -1000 + _frames.Count,
                    WriteDepth = true,
                    UseDepth = true,
                    Alpha = AlphaMode.Blend,
                    Texture = texture
                };
            }

            return new TextureMaterial(texture)
            {
                Priority = -1000 + _frames.Count,
                WriteDepth = true,
                UseDepth = false,
                Color = new Color(1, 1, 1, 1f),
                Alpha = AlphaMode.Blend
            };
        }

        protected Texture2D CreateTexture(uint width, uint height)
        {
            return new Texture2D
            {
                Width = width,
                Height = height,
                MipLevelCount = 5,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.LinearMipmapLinear,
                Format = TextureFormat.Rgba8
            };
        }

        private async Task ProjColorAsync(ColorProjectionFrame frame)
        {
            Debug.Assert(_colorArrayTex != null);
            Debug.Assert(_atlasTex != null);
            Debug.Assert(_recMesh != null);

            await EngineApp.RenderThread;

            var gl = OpenGLRender.Current?.GL ?? throw new InvalidOperationException();

            var glState = GlState.Current;

            var useDepth = UseDepthOcclusion && frame.DepthTexture != null;

            var prog = GlImageProc.LoadProgram(gl,
                "[XrEngine.Reconstruct]mesh_uv_proj.frag",
                "[XrEngine.Reconstruct]mesh_uv_proj.vert",
                useDepth ? ["USE_DEPTH"] : [], []);

            prog.SetUniform("uCaptureViewProj", frame.ViewProj);
            prog.SetUniform("uCaptureCameraPos", frame.CameraPosition);
            prog.SetUniform("uWorldMatrix", _recMesh.WorldMatrix);
            prog.SetUniform("uMinFrontness", UvProjParams.MinFrontness);
            prog.SetUniform("uColorIndex", frame.ImageIndex);

            prog.SetUniform("uDistanceRef", UvProjParams.DistanceRef);
            prog.SetUniform("uDistanceWeightPower", UvProjParams.DistanceWeightPower);
            prog.SetUniform("uMinDistanceWeight", UvProjParams.MinDistanceWeight);

            prog.LoadTexture(_colorArrayTex, 0);

            if (useDepth)
            {
                prog.SetUniform("uDepthBias", UvProjParams.DepthBiasMillimeters / 1000f);
                prog.LoadTexture(frame.DepthTexture!, 1);
            }

            var fb = GlImageProc.PrepareFrameBuffer(gl, _atlasTex.ToGlTexture());

            var vertexHandler = _recMesh.Geometry!.GetGlResource(a => GlVertexSourceHandle.Create(gl, _recMesh));

            if (vertexHandler.NeedUpdate)
                vertexHandler.Update();

            glState.SetWriteDepth(false);
            glState.SetUseDepth(false);
            glState.SetWriteColor(true);
            glState.SetAlphaMode(AlphaMode.Add);
            glState.SetDoubleSided(true);
            glState.SetView(new Rect2I(0, 0, _atlasTex.Width, _atlasTex.Height));
            glState.Commit();

            if (frame.ImageIndex == 0)
            {
                glState.SetClearColor(Color.Transparent);
                gl.Clear(ClearBufferMask.ColorBufferBit);
            }

            vertexHandler.Bind();
            vertexHandler.Draw();
        }

        private async Task ResolveColorAsync()
        {
            Debug.Assert(_atlasTex != null);

            await EngineApp.RenderThread;

            var gl = OpenGLRender.Current?.GL ?? throw new InvalidOperationException();

            var glState = GlState.Current;

            var atlasGlTex = _atlasTex.ToGlTexture();

            var tempGlTex = GlTempAllocator.StaticTexture(
                gl,
                _atlasTex.Width,
                _atlasTex.Height,
                1,
                TextureFormat.RgbaFloat16);

            glState.SetView(new Rect2I(0, 0, _atlasTex.Width, _atlasTex.Height));
            glState.SetWriteDepth(false);
            glState.SetUseDepth(false);
            glState.SetWriteColor(true);
            glState.SetDoubleSided(false);
            glState.SetAlphaMode(AlphaMode.Opaque);
            glState.Commit();

            var resolveProg = GlImageProc.LoadProgram(
                gl,
                "[XrEngine.Reconstruct]resolve.frag",
                [],
                []);

            // atlasTex: accumulated rgba(sum(color * weight), sum(weight))
            // tempTex : resolved rgba(color, coverage)
            GlImageProc.PrepareFrameBuffer(gl, tempGlTex);

            resolveProg.SetUniform("uMinWeight", UvProjParams.ResolveMinWeight);
            resolveProg.LoadTexture(_atlasTex, 0);

            GlImageProc.DrawQuad(gl);

            var dilateProg = GlImageProc.LoadProgram(
                gl,
                "[XrEngine.Reconstruct]dilate.frag",
                [],
                []);

            var sourceGlTex = tempGlTex;
            var targetGlTex = atlasGlTex;

            for (var i = 0; i < UvProjParams.DilationPasses; i++)
            {
                GlImageProc.PrepareFrameBuffer(gl, targetGlTex);

                dilateProg.LoadTexture(sourceGlTex.ToEngineTexture(), 0);

                GlImageProc.DrawQuad(gl);

                (sourceGlTex, targetGlTex) = (targetGlTex, sourceGlTex);
            }

            if (!ReferenceEquals(sourceGlTex, atlasGlTex))
                GlImageProc.CopyColor(sourceGlTex, atlasGlTex);

            //atlasGlTex.MinFilter = TextureMinFilter.LinearMipmapLinear;
            //atlasGlTex.MagFilter = TextureMagFilter.Linear;
            //atlasGlTex.Update();
        }

        private async Task<IMemoryBuffer<byte>> GenerateDepthAsync(Matrix4x4 cameraViewProj, Texture2D? depthTex)
        {
            Debug.Assert(_recMesh != null);

            await EngineApp.RenderThread;

            var gl = OpenGLRender.Current?.GL ?? throw new InvalidOperationException();

            var glState = GlState.Current;

            GlTexture glDepthTex;

            if (depthTex == null)
            {
                if (_tempDepthTex == null)
                {
                    _tempDepthTex = GlTempAllocator.StaticTexture(
                         gl,
                         (uint)DepthMapSize,
                         (uint)DepthMapSize,
                         1,
                         TextureFormat.Depth16);
                }

                glDepthTex = _tempDepthTex;
            }
            else
            {
                if (depthTex.Width == 0)
                {
                    depthTex.MinFilter = ScaleFilter.Nearest;
                    depthTex.MagFilter = ScaleFilter.Nearest;
                    depthTex.MipLevelCount = 0;
                    depthTex.Width = (uint)DepthMapSize;
                    depthTex.Height = (uint)DepthMapSize;
                    depthTex.Format = TextureFormat.Depth16;
                    depthTex.NotifyChanged();
                }

                glDepthTex = depthTex.ToGlTexture();
            }

            if (glDepthTex.MinFilter != TextureMinFilter.Nearest || glDepthTex.MagFilter != TextureMagFilter.Nearest)
            {
                glDepthTex.MinFilter = TextureMinFilter.Nearest;
                glDepthTex.MagFilter = TextureMagFilter.Nearest;
                glDepthTex.UpdateSampler();
            }

            glState.SetView(new Rect2I
            {
                Width = glDepthTex.Width,
                Height = glDepthTex.Height
            });

            var fb = GlImageProc.PrepareFrameBuffer(gl, null, (IGlRenderAttachment)glDepthTex);

            try
            {
                var prog = GlImageProc.LoadProgram(gl, "empty.frag", "basic.vert");

                prog.Use();
                prog.SetUniform("uViewProj", cameraViewProj);
                prog.SetUniform("uWorldMatrix", _recMesh.WorldMatrix);

                glState.SetWriteDepth(true);
                glState.SetUseDepth(true);
                glState.SetWriteColor(false);
                glState.SetClearColor(Color.Transparent);
                glState.SetAlphaMode(AlphaMode.Opaque);
                glState.Commit();

                gl.Clear(ClearBufferMask.DepthBufferBit);

                var vertexHandler = _recMesh.Geometry!.GetGlResource(a => GlVertexSourceHandle.Create(gl, _recMesh));

                if (vertexHandler.NeedUpdate)
                    vertexHandler.Update();

                vertexHandler.Bind();
                vertexHandler.Draw();
            }
            finally
            {
                fb.Unbind();
                glState.SetWriteColor(true);
            }

            var data = GlImageProc.Read(glDepthTex, TextureFormat.Gray16);

            Debug.Assert(data != null && data.Count == 1);

            return data[0].Content!;
        }

        [Action]
        public async Task GenerateMeshAsync()
        {
            Debug.Assert(_lastPath != null);
            Debug.Assert(_colorArrayTex != null);
            Debug.Assert(_host != null);

            var rec = new VoxelMeshReconstructor();
            rec.SetParams(MeshParams);

            var colorFrames = new List<ColorProjectionFrame>();
            var cacheName = Path.Combine(_lastPath, "reconstruct.obj");
            var skipReconstruct = File.Exists(cacheName) && UseMeshCache;

            if (skipReconstruct)
            {
                Log.Info(this, "Load geometry");
                _recMesh?.Dispose();
                _recMesh = AssetLoader.Instance.Load<TriangleMesh>(cacheName, new BasicLoaderOptions { UseCache = false });
                _recMesh.AddComponent<MeshDebugger>();
                Log.Debug(this, "Loaded");
            }
            else
            {
                if (_recMesh == null)
                {
                    _recMesh = new TriangleMesh(new SimpleGeometry3D());
                    _recMesh.AddComponent<MeshDebugger>();
                }
            }

            _recMesh.Name = "Reconstruction";

            Debug.Assert(_recMesh.Geometry != null);

            _recMesh.Geometry.ActiveComponents =
                VertexComponent.Normal |
                VertexComponent.Position |
                VertexComponent.UV0 |
                VertexComponent.UV1 |
                VertexComponent.Tangent;

            var colorData = new List<IMemoryBuffer<byte>>();

            var colorSize = new Size2I();

            foreach (var mesh in _host.Children.OfType<TriangleMesh>())
            {
                if (!mesh.TryComponent<CaptureFrame>(out var frame))
                    continue;

                var meta = frame.Meta!;
                var cameraViewProj = meta.CameraView * meta.CameraProj;

                colorSize.Width = (uint)meta.ColorWidth;
                colorSize.Height = (uint)meta.ColorHeight;

                if (!skipReconstruct)
                {
                    Log.Info(this, "Feed frame {0}", meta.Frame);
                    rec.FeedFrame((Geometry3D<VertexData>)mesh.Geometry!);
                }

                colorFrames.Add(new ColorProjectionFrame(
                    meta.Frame,
                    meta.CameraView.Invert().Translation,
                    cameraViewProj));

                colorData.Add(frame.ColorData!);
            }

            if (!skipReconstruct)
            {
                Log.Info(this, "Extracting mesh");

                rec.ExtractMesh((Geometry3D<VertexData>)_recMesh.Geometry);

                var objWriter = new ObjWriter();
                objWriter.Add(_recMesh);

                File.WriteAllText(cacheName, objWriter.Text());
            }

            Log.Warn(this, "Mesh extracted {0} - {1}", _recMesh.Geometry.VerticesArray!.Length, _recMesh.Geometry.Indices!.Length);

            if (Optimize)
            {
                Log.Warn(this, "Collapse vertices");

                var collapse = new MeshCollapse(CollapseParams);

                collapse.CollapseCloseVertices((Geometry3D<VertexData>)_recMesh.Geometry!);

                Log.Warn(this, "Simplified {0} - {1}", _recMesh.Geometry.VerticesArray!.Length, _recMesh.Geometry.Indices!.Length);
            }

            if (FillHoles)
            {
                Log.Info(this, "Filling holes");
                var filler = new MeshHoleFiller(HoleParams);
                var fillRes = filler.FindMissingTriangles((Geometry3D<VertexData>)_recMesh!.Geometry);
                Log.Warn(this, "{0} triangles found", fillRes.Count);
            }

            if (UseDepthOcclusion)
            {
                foreach (var frame in colorFrames)
                {
                    Log.Info(this, "Generate deph {0}", frame.ImageIndex);
                    /*
                    if (frame.ImageIndex == 0)
                        EngineNativeLib.RdcStartFrameCapture();
                    */

                    if (UnwrapUv)
                    {
                        if (frame.DepthTexture != null && frame.DepthTexture.Width != DepthMapSize)
                            frame.DepthTexture.Dispose();

                        frame.DepthTexture = new Texture2D();
                    }

                    frame.DepthMap = await GenerateDepthAsync(frame.ViewProj, frame.DepthTexture);
                    frame.DepthWidth = DepthMapSize;
                    frame.DepthHeight = DepthMapSize;

                    /*
                    if (frame.ImageIndex == 0)
                        EngineNativeLib.RdcEndFrameCapture(false);
                    */
                }
            }

            float[] exposures = [];

            _wireMat ??= new WireframeMaterial() { Color = Color.White, IsEnabled = false };
            _colorMat ??= new PbrMaterial() { Color = Color.White, Metalness = 0, IsEnabled = false };

            if (UnwrapUv)
            {
                Log.Info(this, "UV Unwrap");

                var uvUnwrap = new MeshUvUnwrapper();
                uvUnwrap.SetParameters(UvUnwrapParams);
                uvUnwrap.Unwrap((Geometry3D<VertexData>)_recMesh.Geometry);

                if (BuildAtlas)
                {
                    _atlasTex?.Dispose();

                    EngineNativeLib.RdcStartFrameCapture();

                    _atlasTex = Texture2D.FromData([new TextureData
                    {
                        Width = (uint)UvProjParams.AtlasSize,
                        Height = (uint)UvProjParams.AtlasSize,
                        Format = TextureFormat.RgbaFloat16,
                    }]);

                    foreach (var frame in colorFrames)
                    {
                        Log.Info(this, "Proj color {0}", frame.ImageIndex);

                        await ProjColorAsync(frame);
                    }

                    Log.Info(this, "Resolve color");

                    await ResolveColorAsync();

                    EngineNativeLib.RdcEndFrameCapture(false);

                    if (_texMat is not TextureMaterial)
                        _texMat = new TextureMaterial();

                    ((TextureMaterial)_texMat).Texture = _atlasTex;

                    //_colorMat.ColorMap = _atlasTex;

                    _recMesh.Materials.Add(_texMat);
                }
            }
            else
            {
                Log.Info(this, "Color projection");

                var proj = new MeshTextureProjection();
                proj.SetParams(ProjParams);
                proj.Project((Geometry3D<VertexData>)_recMesh.Geometry, colorFrames);

                if (SolveExposure)
                {
                    Log.Info(this, "Solving exposure");

                    var solver = new FrameExposureSolver();

                    solver.SetParams(ExposeParams);

                    exposures = solver.Compute(
                        (Geometry3D<VertexData>)_recMesh.Geometry,
                        colorData.ToArray(),
                        (int)colorSize.Width,
                        (int)colorSize.Height);
                }
            }

            if (!UnwrapUv)
            {
                if (BuildAtlas)
                {
                    Log.Info(this, "Bulding atlas");

                    var builder = new TextureAtlasLayoutBuilder();

                    builder.SetParams(new()
                    {
                        SourceTextureCount = _host.Children.Count
                    });

                    _atlasTex?.Dispose();
                    _atlasTex = await builder.GenerateAtlasTextureAsync([(Geometry3D<VertexData>)_recMesh.Geometry], _colorArrayTex, exposures);

                    if (_texMat is not TextureMaterial)
                        _texMat = new TextureMaterial();

                    ((TextureMaterial)_texMat).Texture = _atlasTex;

                    _recMesh.Materials.Add(_texMat);

                }
                else
                {
                    if (_texMat is not TextureMaterial)
                    {
                        _texMat = new MultiTextureMaterial
                        {

                            Exposure = exposures
                        };
                    }
                    ((MultiTextureMaterial)_texMat).Texture = _colorArrayTex;
                }
            }

            if (ComputeIndices)
            {
                Log.Info(this, "Compute Indices");
                _recMesh.Geometry.ComputeIndices();
            }
            else
                _recMesh.Geometry.NotifyChanged(ChangeType.Geometry);

            if (Optimize)
            {
                Log.Info(this, "Optmize");
                MeshOptimizer.Optimize((Geometry3D<VertexData>)_recMesh.Geometry!);
            }

            Log.Warn(this, "Done {0} - {1}", _recMesh.Geometry.VerticesArray!.Length, _recMesh.Geometry.Indices!.Length);

            await EngineApp.MainThread;

            _recMesh.Materials.Clear();

            if (_texMat != null)
                _recMesh.Materials.Add(_texMat);

            _recMesh.Materials.Add(_wireMat);
            _recMesh.Materials.Add(_colorMat);

            if (_recMesh.Parent == null)
                _host.Scene!.AddChild(_recMesh);

            _recMesh.NotifyChanged(ChangeType.Geometry | ChangeType.Render);
        }

        [Action]
        public void Rebuild()
        {
            if (_lastPath == null)
                return;

            Load(_lastPath, true);
        }

        public unsafe void Load(
            string path,
            bool clear = false,
            float cleanupMargin = -0.01f)
        {
            _lastPath = path;

            if (clear)
            {
                foreach (var frame in _frames)
                {
                    frame.CameraTexture?.Dispose();
                    frame.Mesh?.Dispose();
                }

                _frames.Clear();
                _host!.Clear();
            }

            var frameDirs = Directory.GetDirectories(path)
                .OrderBy(a => a)
                .ToArray();

            var splats = new List<SplatData>();
            var texArrayData = new List<TextureData>();

            var generator = new DepthGeometryGenerator(GridSize, GridSize);
            generator.SetParams(GeneratorParams);

            foreach (var frameDir in frameDirs)
            {
                var metaPath = Path.Combine(frameDir, "meta.json");
                var depthPath = Path.Combine(frameDir, "depth_u16.raw");
                var colorPath = Path.Combine(frameDir, "color_rgba.raw");

                if (!File.Exists(metaPath) ||
                    !File.Exists(depthPath) ||
                    !File.Exists(colorPath))
                {
                    continue;
                }

                var meta = JsonSerializer.Deserialize<DepthFrameMeta>(
                    File.ReadAllText(metaPath),
                    _jsonOptions)!;

                Log.Info(this, "Loading frame {0}", meta.Frame);

                var colorViewProj = meta.CameraView * meta.CameraProj;

                if (Clip && splats.Count > 0)
                    RemoveSplatsInsideFrame(splats, colorViewProj, cleanupMargin);

                var depthBytes = File.ReadAllBytes(depthPath);

                SimpleGeometry3D geometry;

                fixed (byte* pBytes = depthBytes)
                {
                    geometry = generator.CreateGeometry(
                        (ushort*)pBytes,
                        meta.DepthWidth,
                        meta.DepthHeight,
                        Matrix4x4.Invert(meta.DepthView * meta.DepthProj, out var inv)
                            ? inv
                            : Matrix4x4.Identity,
                        colorViewProj);
                }

                var mesh = new TriangleMesh(geometry)
                {
                    Name = $"Frame {meta.Frame}"
                };

                var colorTexture = LoadColorTextureRaw(
                    colorPath,
                    meta.ColorWidth,
                    meta.ColorHeight,
                    $"Tex Frame {meta.Frame}");

                var colorData = colorTexture.Data![0];

                texArrayData.Add(new TextureData
                {
                    Width = colorData.Width,
                    Height = colorData.Height,
                    Content = colorData.Content,
                    Layer = (uint)meta.Frame,
                    Format = colorData.Format
                });

                mesh.Materials.Add(CreateMaterial(colorTexture));

                mesh.Materials.Add(new WireframeMaterial
                {
                    Color = new Color(1, 1, 1, 1),
                    IsEnabled = false
                });

                mesh.AddComponent(new CaptureFrame
                {
                    Meta = meta,
                    Texture = colorTexture,
                    ColorData = colorData.Content
                });

                if (SplatMode)
                {
                    DepthGridSplatBuilder.CreateSplats(
                        splats,
                        geometry,
                        colorData.Content!,
                        (int)colorTexture.Width,
                        (int)colorTexture.Height);
                }

                _frames.Add(new DepthFrame
                {
                    Mesh = mesh,
                    CameraTexture = colorTexture,

                    CameraProj = meta.CameraProj,
                    CameraView = meta.CameraView,

                    DepthView = meta.DepthView,
                    DepthProj = meta.DepthProj,

                    CameraXrTime = meta.CameraXrTime,
                    DepthXrTime = meta.DepthXrTime,
                    FrameXrTime = meta.FrameXrTime
                });

                if (!SplatMode)
                    _host!.AddChild(mesh);
            }

            _colorArrayTex = new Texture2D
            {
                MinFilter = ScaleFilter.Linear,
                MagFilter = ScaleFilter.Linear,
                MipLevelCount = 0
            };

            _colorArrayTex.LoadData(texArrayData);

            _frameIndex = _frames.Count;

            if (SplatMode)
            {
                _splatMesh = new SplatMesh(splats.ToArray());
                _host!.AddChild(_splatMesh);
            }

            Log.Info(this, "Done!");
        }

        private void RemoveSplatsInsideFrame(List<SplatData> splats, Matrix4x4 colorViewProj, float cleanupMargin)
        {
            var write = 0;

            for (var read = 0; read < splats.Count; read++)
            {
                var splat = splats[read];
                var clip = Vector4.Transform(new Vector4(splat.Position, 1.0f), colorViewProj);
                var remove = false;

                if (clip.W > 0.00001f)
                {
                    var p = clip / clip.W;

                    remove =
                        p.X >= -1.0f - cleanupMargin && p.X <= 1.0f + cleanupMargin &&
                        p.Y >= -1.0f - cleanupMargin && p.Y <= 1.0f + cleanupMargin &&
                        p.Z >= -1.0f - cleanupMargin && p.Z <= 1.0f + cleanupMargin;
                }

                if (!remove)
                    splats[write++] = splat;
            }

            if (write < splats.Count)
                splats.RemoveRange(write, splats.Count - write);
        }

        public DepthFrame? CreateSnapeshot()
        {
            var camera = _capture!.GetCameraStatus(OculusCameras.Left);

            if (!camera.IsActive)
                return null;

            var cameraTime = camera.FrameTime;
            var cameraWorld = camera.Pose?.ToMatrix() ?? Matrix4x4.Identity;

            Matrix4x4.Invert(cameraWorld, out var cameraView);

            var cameraProj = camera.Proj!.Value;
            var cameraViewProj = cameraView * cameraProj;

            var frozenMesh = _envDepth!.Freeze(cameraViewProj);

            if (frozenMesh == null)
                return null;

            var mat = (EnvDepthMaterial)_envDepth.Materials[0];
            var frameTexture = CreateTexture(camera.Texture!.Width, camera.Texture.Height);

            GlImageProc.CopyColor(camera.Texture!.ToGlTexture(), frameTexture.ToGlTexture());

            frozenMesh.Materials.Add(CreateMaterial(frameTexture));

            var frame = new DepthFrame
            {
                CameraProj = cameraProj,
                CameraView = cameraView,
                CameraXrTime = cameraTime,

                Mesh = frozenMesh,
                CameraTexture = frameTexture,

                DepthView = mat.DepthCamera.Eyes![0].View,
                DepthProj = mat.DepthCamera.Eyes![0].Projection,
                DepthXrTime = mat.LastFrameTime,

                FrameXrTime = XrApp.Current!.FramePredictedDisplayTime
            };

            if (_mode == DepthSnapeshotMode.Record)
            {
                var size = ((Grid3D)_envDepth.Geometry!).Size;

                var meta = new DepthFrameMeta
                {
                    Frame = _frameIndex++,

                    ColorWidth = (int)camera.Texture.Width,
                    ColorHeight = (int)camera.Texture.Height,

                    DepthWidth = (int)mat.LastTexture!.Width,
                    DepthHeight = (int)mat.LastTexture.Height,

                    GridWidth = (int)size.Width,
                    GridHeight = (int)size.Height,

                    CameraProj = cameraProj,
                    CameraView = cameraView,

                    DepthView = mat.DepthCamera.Eyes![0].View,
                    DepthProj = mat.DepthCamera.Eyes![0].Projection,

                    CameraXrTime = cameraTime,
                    DepthXrTime = mat.LastFrameTime,
                    FrameXrTime = frame.FrameXrTime
                };

                SaveFrame(frame, meta);
            }

            return frame;
        }

        public void DeleteLast()
        {
            if (_frames.Count == 0)
                return;

            var lastFrame = _frames[^1];

            _frames.RemoveAt(_frames.Count - 1);

            lastFrame.CameraTexture!.Dispose();
            lastFrame.Mesh!.Dispose();
        }

        protected override void Update(RenderContext ctx)
        {
            if (_mode == DepthSnapeshotMode.Read)
            {
                foreach (var mesh in _host!.Children.OfType<TriangleMesh>())
                {
                    if (!mesh.TryComponent<CaptureFrame>(out var frame))
                        continue;

                    if (mesh.Materials[0] is GridMaterial mat)
                        mat.Exposure = frame.Exposure;
                }

                //_wireMat?.IsEnabled = ShowWireframe;
                //_colorMar?.IsEnabled = ShowWireframe;
                //_texMat?.IsEnabled = !ShowWireframe;

                return;
            }

            Debug.Assert(_captureBtn != null && _deleteBtn != null);

            if (_captureBtn.IsChanged && _captureBtn.Value)
            {
                var frame = CreateSnapeshot();

                if (frame != null)
                {
                    _frames.Add(frame);
                    _host!.AddChild(frame.Mesh!);
                }
            }

            if (_deleteBtn.IsChanged && _deleteBtn.Value)
                DeleteLast();
        }

        public void ConfigureInput(IXrBasicInteractionProfile input)
        {
            _captureBtn = input.Right!.Button!.AClick!;
            _deleteBtn = input.Right!.Button!.BClick!;
        }

        [Action]
        public async Task Export()
        {
            if (_recMesh == null)
                return;

            var objWriter = new ObjWriter();

            objWriter.Add(_recMesh);

            File.WriteAllText(Path.Combine(_lastPath!, "reconstruct_final.obj"), objWriter.Text());

            if (_atlasTex != null)
            {
                await EngineApp.RenderThread;

                var data = GlImageProc.Read(_atlasTex.ToGlTexture(), TextureFormat.Rgb8);

                var jpeg = TurboJpegLib.Compress(new TurboJpegLib.ImageData
                {
                    Data = data![0]!.Content!.AsSpan().ToArray(),
                    Width = (int)_atlasTex.Width,
                    Height = (int)_atlasTex.Height
                }, 90, TurboJpegLib.TJPF.TJPF_RGB);

                File.WriteAllBytes(Path.Combine(_lastPath!, "reconstruct_final.jpg"), jpeg);
            }

            Log.Info(this, "Exported on {0}", _lastPath);
        }

        public TriangleMesh? Mesh => _recMesh;

        public string SessionPath => _sessionPath;

        public int DepthMapSize { get; set; }

        public int GridSize { get; set; }

        public bool SolveExposure { get; set; }

        public bool SplatMode { get; set; }

        public bool Optimize { get; set; }

        public bool Clip { get; set; }

        public bool UseMeshCache { get; set; }

        public bool UseDepthOcclusion { get; set; }

        public bool BuildAtlas { get; set; }

        public bool ComputeIndices { get; set; }

        public bool ShowWireframe { get; set; }

        public bool UnwrapUv { get; set; }

        public bool FillHoles { get; private set; }

        public bool UseSrgb { get; set; }

        public MeshCollapseParams CollapseParams { get; set; }

        public UvAtlasProjectionParams UvProjParams { get; set; }

        public MeshUvUnwrapperParams UvUnwrapParams { get; set; }

        public VoxelMeshReconstructorParams MeshParams { get; set; }

        public DepthGeometryGeneratorParams GeneratorParams { get; set; }

        public FrameExposureSolverParams ExposeParams { get; set; }

        public MeshTextureProjectionParams ProjParams { get; set; }

        public MeshHoleFillerParams HoleParams { get; set; }

        public string? DebugTriangles { get; set; }

    }
}