
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using System.Runtime.InteropServices;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.Lighting
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GpuVoxelFaceData
    {
        public Vector3I Cell;
        public int Face;

        public VoxelTriangleSide Side;

        public Vector4 BaseColor;
        public Vector3 Normal;
        public float Roughness;
        public float Metallic;
    }

    public sealed class GpuSceneVoxelizer : IDisposable
    {
        private enum ScanAxis
        {
            X,
            Y,
            Z
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct ScanPixel
        {
            public Vector4 Color;
            public Vector4 Normal;
            public Vector4 Material;
        }

        private sealed class AxisTarget : IDisposable
        {
            public GlTexture Color = null!;
            public GlTexture Normal = null!;
            public GlTexture Material = null!;
            public GlTexture Depth = null!;

            public GlTexture ColorAtlas = null!;
            public GlTexture NormalAtlas = null!;
            public GlTexture MaterialAtlas = null!;

            public uint Width;
            public uint Height;
            public uint Layers;
            public uint AtlasWidth;
            public uint AtlasHeight;
            public uint AtlasColumns;

            public void Dispose()
            {
                Color?.Dispose();
                Normal?.Dispose();
                Material?.Dispose();
                Depth?.Dispose();
                ColorAtlas?.Dispose();
                NormalAtlas?.Dispose();
                MaterialAtlas?.Dispose();
            }
        }

        private const int NegX = 0;
        private const int PosX = 1;
        private const int NegY = 2;
        private const int PosY = 3;
        private const int NegZ = 4;
        private const int PosZ = 5;

        private readonly GL _gl;
        private readonly int _viewsPerBatch;
        private readonly bool _usePackReadback;

        private readonly GlMultiViewFrameBuffer _scanFbo;
        private readonly GlMultiViewFrameBuffer _packFbo;

        private readonly GlSimpleProgram _scanProgram;
        private readonly GlSimpleProgram _packProgram;

        private AxisTarget? _xTarget;
        private AxisTarget? _yTarget;
        private AxisTarget? _zTarget;

        private VoxelGridDesc _grid;

        public GpuSceneVoxelizer(GL gl, int viewsPerBatch = 1, bool usePackReadback = true)
        {
            _gl = gl;
            _viewsPerBatch = Math.Max(1, viewsPerBatch);
            _usePackReadback = usePackReadback;

            _scanFbo = new GlMultiViewFrameBuffer(gl);
            _packFbo = new GlMultiViewFrameBuffer(gl);

            _scanProgram = new GlSimpleProgram(
                gl,
                "GpuSceneVoxelizer.Scan.vert",
                "GpuSceneVoxelizer.Scan.frag",
                a => Embedded.GetString<GpuSceneVoxelizer>(a));

            if (_viewsPerBatch > 1)
                _scanProgram.AddExtension("GL_OVR_multiview2");

            _scanProgram.Build();

            _packProgram = new GlSimpleProgram(
                gl,
                "GpuSceneVoxelizer.Pack.vert",
                "GpuSceneVoxelizer.Pack.frag",
                a => Embedded.GetString<GpuSceneVoxelizer>(a));

            _packProgram.Build();
        }

        public List<GpuVoxelFaceData> Voxelize(
            IReadOnlyList<TriangleMesh> meshes,
            VoxelGridDesc grid)
        {
            _grid = grid;

            EnsureTargets(grid);

            ScanAxisVolume(meshes, ScanAxis.X, _xTarget!);
            ScanAxisVolume(meshes, ScanAxis.Y, _yTarget!);
            ScanAxisVolume(meshes, ScanAxis.Z, _zTarget!);

            var result = new List<GpuVoxelFaceData>();

            ReadAxisVolume(ScanAxis.X, _xTarget!, result);
            ReadAxisVolume(ScanAxis.Y, _yTarget!, result);
            ReadAxisVolume(ScanAxis.Z, _zTarget!, result);

            return result;
        }

        private void EnsureTargets(VoxelGridDesc grid)
        {
            EnsureAxisTarget(ref _xTarget, (uint)grid.Size.Y, (uint)grid.Size.Z, (uint)grid.Size.X);
            EnsureAxisTarget(ref _yTarget, (uint)grid.Size.X, (uint)grid.Size.Z, (uint)grid.Size.Y);
            EnsureAxisTarget(ref _zTarget, (uint)grid.Size.X, (uint)grid.Size.Y, (uint)grid.Size.Z);
        }

        private void EnsureAxisTarget(
            ref AxisTarget? target,
            uint width,
            uint height,
            uint layers)
        {
            if (target != null &&
                target.Width == width &&
                target.Height == height &&
                target.Layers == layers)
            {
                return;
            }

            target?.Dispose();

            target = new AxisTarget
            {
                Width = width,
                Height = height,
                Layers = layers
            };

            target.Color = new GlTexture(_gl);
            target.Color.Allocate(width, height, layers, TextureFormat.Rgba32);

            target.Normal = new GlTexture(_gl);
            target.Normal.Allocate(width, height, layers, TextureFormat.RgbaFloat16);

            target.Material = new GlTexture(_gl);
            target.Material.Allocate(width, height, layers, TextureFormat.Rgba32);

            target.Depth = new GlTexture(_gl);
            target.Depth.Allocate(width, height, layers, TextureFormat.Depth24);

            target.AtlasColumns = (uint)Math.Ceiling(Math.Sqrt(layers));
            uint atlasRows = (layers + target.AtlasColumns - 1) / target.AtlasColumns;

            target.AtlasWidth = target.AtlasColumns * width;
            target.AtlasHeight = atlasRows * height;

            target.ColorAtlas = new GlTexture(_gl);
            target.ColorAtlas.Allocate(target.AtlasWidth, target.AtlasHeight, 1, TextureFormat.Rgba32);

            target.NormalAtlas = new GlTexture(_gl);
            target.NormalAtlas.Allocate(target.AtlasWidth, target.AtlasHeight, 1, TextureFormat.RgbaFloat16);

            target.MaterialAtlas = new GlTexture(_gl);
            target.MaterialAtlas.Allocate(target.AtlasWidth, target.AtlasHeight, 1, TextureFormat.Rgba32);
        }

        private void ScanAxisVolume(
            IReadOnlyList<TriangleMesh> meshes,
            ScanAxis axis,
            AxisTarget target)
        {
            GlState.Current!.SetUseDepth(true);
            GlState.Current.SetWriteDepth(true);
            GlState.Current.EnableFeature(EnableCap.CullFace, false);
            GlState.Current.SetAlphaMode(AlphaMode.Opaque);
            GlState.Current.SetColorMask(true, true, true, true, true);

            _scanProgram.Use();

            _scanProgram.SetUniform("uGridOrigin", _grid.Origin);
            _scanProgram.SetUniform("uVoxelSize", _grid.VoxelSize);
            _scanProgram.SetUniform("uGridSize", _grid.Size);
            _scanProgram.SetUniform("uAxis", (float)axis);

            for (int baseSlice = 0; baseSlice < target.Layers; baseSlice += _viewsPerBatch)
            {
                int viewCount = Math.Min(_viewsPerBatch, (int)target.Layers - baseSlice);

                BindScanTarget(target, baseSlice, viewCount);

                GlState.Current!.SetView(new Rect2I(0, 0, target.Width, target.Height));

                _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                _scanProgram.SetUniform("uBaseSlice", baseSlice);
                SetScanMatrices(axis, baseSlice, viewCount);

                DrawMeshes(meshes);
            }
        }

        private void SetScanMatrices(ScanAxis axis, int baseSlice, int viewCount)
        {
            Span<float> data = stackalloc float[_viewsPerBatch * 16];

            for (int i = 0; i < viewCount; ++i)
            {
                int slice = baseSlice + i;
                Matrix4x4 matrix = CreateSliceViewProjection(axis, slice);
                CopyMatrix(matrix, data.Slice(i * 16, 16));
            }

            _scanProgram.SetUniform("uViewProj", data.ToArray());
        }

        private Matrix4x4 CreateSliceViewProjection(ScanAxis axis, int slice)
        {
            Vector3 size = new Vector3(
                _grid.Size.X * _grid.VoxelSize,
                _grid.Size.Y * _grid.VoxelSize,
                _grid.Size.Z * _grid.VoxelSize);

            Vector3 min = _grid.Origin;
            Vector3 max = _grid.Origin + size;

            float s0 = slice * _grid.VoxelSize;
            float s1 = s0 + _grid.VoxelSize;

            // OpenGL clip convention is intentionally hidden here: these matrices create
            // a one-voxel slab in the scanned axis and map the other two axes to the viewport.
            return axis switch
            {
                ScanAxis.X => Matrix4x4.CreateOrthographicOffCenter(
                    min.Y,
                    max.Y,
                    min.Z,
                    max.Z,
                    s0,
                    s1) * Matrix4x4.CreateLookAt(
                        new Vector3(min.X + s0 - _grid.VoxelSize, 0.0f, 0.0f),
                        new Vector3(min.X + s0, 0.0f, 0.0f),
                        Vector3.UnitY),

                ScanAxis.Y => Matrix4x4.CreateOrthographicOffCenter(
                    min.X,
                    max.X,
                    min.Z,
                    max.Z,
                    s0,
                    s1) * Matrix4x4.CreateLookAt(
                        new Vector3(0.0f, min.Y + s0 - _grid.VoxelSize, 0.0f),
                        new Vector3(0.0f, min.Y + s0, 0.0f),
                        Vector3.UnitZ),

                _ => Matrix4x4.CreateOrthographicOffCenter(
                    min.X,
                    max.X,
                    min.Y,
                    max.Y,
                    s0,
                    s1) * Matrix4x4.CreateLookAt(
                        new Vector3(0.0f, 0.0f, min.Z + s0 - _grid.VoxelSize),
                        new Vector3(0.0f, 0.0f, min.Z + s0),
                        Vector3.UnitY),
            };
        }

        private void DrawMeshes(IReadOnlyList<TriangleMesh> meshes)
        {
            foreach (var mesh in meshes)
            {
                if (mesh == null)
                    continue;

                SetMeshUniforms(mesh);

                var handle = mesh.GetGlResource(a =>
                    GlVertexSourceHandle.Create(_gl, mesh));

                handle.Bind();
                handle.Draw();
            }
        }

        private void SetMeshUniforms(TriangleMesh mesh)
        {
            _scanProgram.SetUniform("uWorld", mesh.WorldMatrix);
            _scanProgram.SetUniform("uNormalMatrix", mesh.NormalMatrix);

            var mat = mesh.Materials.OfType<PbrV2Material>().FirstOrDefault();

            if (mat != null)
            {
                _scanProgram.SetUniform("uBaseColorFactor", mat.Color);
                _scanProgram.SetUniform("uMetallicFactor", mat.Metalness);
                _scanProgram.SetUniform("uRoughnessFactor", mat.Roughness);

                if (mat.ColorMap != null)
                {
                    GlState.Current!.LoadTexture(mat.ColorMap.ToGlTexture(), 0);
                    _scanProgram.SetUniform("uHasColorMap", 1.0f);
                    _scanProgram.SetUniform("uColorMap", 0.0f);
                }
                else
                {
                    _scanProgram.SetUniform("uHasColorMap", 0.0f);
                }

                if (mat.MetallicRoughnessMap != null)
                {
                    GlState.Current!.LoadTexture(mat.MetallicRoughnessMap.ToGlTexture(), 1);
                    _scanProgram.SetUniform("uHasMetallicRoughnessMap", 1.0f);
                    _scanProgram.SetUniform("uMetallicRoughnessMap", 1.0f);
                }
                else
                {
                    _scanProgram.SetUniform("uHasMetallicRoughnessMap", 0.0f);
                }
            }
            else
            {
                _scanProgram.SetUniform("uBaseColorFactor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                _scanProgram.SetUniform("uMetallicFactor", 0.0f);
                _scanProgram.SetUniform("uRoughnessFactor", 0.8f);
                _scanProgram.SetUniform("uHasColorMap", 0.0f);
                _scanProgram.SetUniform("uHasMetallicRoughnessMap", 0.0f);
            }
        }

        private void ReadAxisVolume(
            ScanAxis axis,
            AxisTarget target,
            List<GpuVoxelFaceData> faces)
        {
            if (_usePackReadback)
                ReadAxisVolumePacked(axis, target, faces);
            else
                ReadAxisVolumeLayers(axis, target, faces);
        }

        private void ReadAxisVolumePacked(
            ScanAxis axis,
            AxisTarget target,
            List<GpuVoxelFaceData> faces)
        {
            PackAxisTexture(target, target.Color, target.ColorAtlas);
            PackAxisTexture(target, target.Normal, target.NormalAtlas);
            PackAxisTexture(target, target.Material, target.MaterialAtlas);

            Vector4[] colors = ReadAtlas(target.ColorAtlas, target.AtlasWidth, target.AtlasHeight);
            Vector4[] normals = ReadAtlas(target.NormalAtlas, target.AtlasWidth, target.AtlasHeight);
            Vector4[] materials = ReadAtlas(target.MaterialAtlas, target.AtlasWidth, target.AtlasHeight);

            UnpackAxis(axis, target, colors, normals, materials, faces);
        }

        private void ReadAxisVolumeLayers(
            ScanAxis axis,
            AxisTarget target,
            List<GpuVoxelFaceData> faces)
        {
            for (int layer = 0; layer < target.Layers; ++layer)
            {
                Vector4[] colors = ReadLayer(target.Color, target.Width, target.Height, layer);
                Vector4[] normals = ReadLayer(target.Normal, target.Width, target.Height, layer);
                Vector4[] materials = ReadLayer(target.Material, target.Width, target.Height, layer);

                UnpackAxisLayer(axis, target, layer, colors, normals, materials, faces);
            }
        }

        private void PackAxisTexture(AxisTarget target, GlTexture source, GlTexture atlas)
        {
            _packFbo.BaseViewIndex = 0;
            _packFbo.NumViews = 1;

            _packFbo.BindAttachment(atlas, FramebufferAttachment.ColorAttachment0, true);
            _packFbo.BindDraw(DrawBufferMode.ColorAttachment0);

            GlState.Current!.SetView(new Rect2I(0, 0, target.AtlasWidth, target.AtlasHeight));

            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _packProgram.Use();

            GlState.Current!.LoadTexture(source, 0);
            _packProgram.SetUniform("uSource", 0.0f);
            _packProgram.SetUniform("uTileSize", new Vector2(target.Width, target.Height));
            _packProgram.SetUniform("uAtlasColumns", (float)target.AtlasColumns);
            _packProgram.SetUniform("uLayerCount", (float)target.Layers);

            DrawFullScreenTriangle();
        }

        private Vector4[] ReadAtlas(GlTexture atlas, uint width, uint height)
        {
            _packFbo.BaseViewIndex = 0;
            _packFbo.NumViews = 1;
            _packFbo.BindAttachment(atlas, FramebufferAttachment.ColorAttachment0, false);

            var result = new Vector4[width * height];

            unsafe
            {
                fixed (Vector4* ptr = result)
                {
                    _gl.ReadPixels(
                        0,
                        0,
                        width,
                        height,
                        PixelFormat.Rgba,
                        PixelType.Float,
                        ptr);
                }
            }

            return result;
        }

        private Vector4[] ReadLayer(GlTexture texture, uint width, uint height, int layer)
        {
            _packFbo.BaseViewIndex = (uint)layer;
            _packFbo.NumViews = 1;
            _packFbo.BindAttachment(texture, FramebufferAttachment.ColorAttachment0, false);

            var result = new Vector4[width * height];

            unsafe
            {
                fixed (Vector4* ptr = result)
                {
                    _gl.ReadPixels(
                        0,
                        0,
                        width,
                        height,
                        PixelFormat.Rgba,
                        PixelType.Float,
                        ptr);
                }
            }

            return result;
        }

        private void UnpackAxis(
            ScanAxis axis,
            AxisTarget target,
            Vector4[] colors,
            Vector4[] normals,
            Vector4[] materials,
            List<GpuVoxelFaceData> faces)
        {
            for (int layer = 0; layer < target.Layers; ++layer)
            {
                int tileX = (int)(layer % target.AtlasColumns);
                int tileY = (int)(layer / target.AtlasColumns);

                for (int py = 0; py < target.Height; ++py)
                {
                    for (int px = 0; px < target.Width; ++px)
                    {
                        int atlasX = tileX * (int)target.Width + px;
                        int atlasY = tileY * (int)target.Height + py;
                        int srcIndex = atlasX + atlasY * (int)target.AtlasWidth;

                        Vector4 material = materials[srcIndex];

                        if (material.W <= 0.0f)
                            continue;

                        AddVoxelFace(
                            axis,
                            layer,
                            px,
                            py,
                            colors[srcIndex],
                            normals[srcIndex],
                            material,
                            faces);
                    }
                }
            }
        }

        private void UnpackAxisLayer(
            ScanAxis axis,
            AxisTarget target,
            int layer,
            Vector4[] colors,
            Vector4[] normals,
            Vector4[] materials,
            List<GpuVoxelFaceData> faces)
        {
            for (int py = 0; py < target.Height; ++py)
            {
                for (int px = 0; px < target.Width; ++px)
                {
                    int srcIndex = px + py * (int)target.Width;

                    Vector4 material = materials[srcIndex];

                    if (material.W <= 0.0f)
                        continue;

                    AddVoxelFace(
                        axis,
                        layer,
                        px,
                        py,
                        colors[srcIndex],
                        normals[srcIndex],
                        material,
                        faces);
                }
            }
        }

        private static void AddVoxelFace(
            ScanAxis axis,
            int layer,
            int px,
            int py,
            Vector4 color,
            Vector4 normal,
            Vector4 material,
            List<GpuVoxelFaceData> faces)
        {
            int x;
            int y;
            int z;
            int frontFace;
            int backFace;

            switch (axis)
            {
                case ScanAxis.X:
                    x = layer;
                    y = px;
                    z = py;
                    frontFace = NegX;
                    backFace = PosX;
                    break;

                case ScanAxis.Y:
                    x = px;
                    y = layer;
                    z = py;
                    frontFace = NegY;
                    backFace = PosY;
                    break;

                default:
                    x = px;
                    y = py;
                    z = layer;
                    frontFace = NegZ;
                    backFace = PosZ;
                    break;
            }

            bool isFront = material.Z > 0.5f;

            faces.Add(new GpuVoxelFaceData
            {
                Cell = new Vector3I(x, y, z),
                Face = isFront ? frontFace : backFace,
                Side = isFront ? VoxelTriangleSide.Front : VoxelTriangleSide.Back,

                BaseColor = color,
                Normal = DecodeNormal(normal),
                Roughness = material.X,
                Metallic = material.Y
            });
        }

        private static Vector3 DecodeNormal(Vector4 encoded)
        {
            var normal = new Vector3(
                encoded.X * 2.0f - 1.0f,
                encoded.Y * 2.0f - 1.0f,
                encoded.Z * 2.0f - 1.0f);

            return normal.LengthSquared() > 1e-8f
                ? Vector3.Normalize(normal)
                : Vector3.UnitY;
        }

        private void BindScanTarget(AxisTarget target, int baseLayer, int viewCount)
        {
            _scanFbo.BaseViewIndex = (uint)baseLayer;
            _scanFbo.NumViews = (uint)viewCount;

            _scanFbo.BindDraw(
                DrawBufferMode.ColorAttachment0,
                DrawBufferMode.ColorAttachment1,
                DrawBufferMode.ColorAttachment2);

            _scanFbo.BindAttachment(target.Color, FramebufferAttachment.ColorAttachment0, true);
            _scanFbo.BindAttachment(target.Normal, FramebufferAttachment.ColorAttachment1, true);
            _scanFbo.BindAttachment(target.Material, FramebufferAttachment.ColorAttachment2, true);
            _scanFbo.BindAttachment(target.Depth, FramebufferAttachment.DepthAttachment, false);

        }

        private void DrawFullScreenTriangle()
        {
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        private static void CopyMatrix(Matrix4x4 value, Span<float> data)
        {
            data[0] = value.M11;
            data[1] = value.M12;
            data[2] = value.M13;
            data[3] = value.M14;
            data[4] = value.M21;
            data[5] = value.M22;
            data[6] = value.M23;
            data[7] = value.M24;
            data[8] = value.M31;
            data[9] = value.M32;
            data[10] = value.M33;
            data[11] = value.M34;
            data[12] = value.M41;
            data[13] = value.M42;
            data[14] = value.M43;
            data[15] = value.M44;
        }



        public void Dispose()
        {
            _xTarget?.Dispose();
            _yTarget?.Dispose();
            _zTarget?.Dispose();
            _scanProgram?.Dispose();
            _packProgram?.Dispose();
            _scanFbo?.Dispose();
            _packFbo?.Dispose();
        }
    }
}
