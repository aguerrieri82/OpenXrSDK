#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.Lighting
{

    public struct GpuVoxelFaceData
    {
        public Vector3I Cell;
        public int Face;
        public VoxelTriangleSide Side;
        public Color BaseColor;
        public Vector3 Normal;
        public float Roughness;
        public float Metallic;
    }

    public class GpuMeshVoxelizerParams
    {
        public GpuMeshVoxelizerParams()
        {
            Passes = 2;
            AxisEps = 0.1f;
            BoundsPadding = 2;
            AddBackFaces = true;
        }

        public int Passes { get; set; }

        public float AxisEps { get; set; }

        public int BoundsPadding { get; set; }

        public bool AddBackFaces { get; set; }
    }

    public sealed class GpuMeshVoxelizer : IDisposable, IMeshVoxelizer
    {
        #region STRUCTS 

        private enum ScanAxis
        {
            X,
            Y,
            Z
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Rgba32Pixel
        {
            public byte R;
            public byte G;
            public byte B;
            public byte A;

            public readonly Color ToColor()
            {
                const float s = 1.0f / 255.0f;
                return new Color(R * s, G * s, B * s, A * s);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RgbHalfPixel
        {
            public Half R;
            public Half G;
            public Half B;

            public readonly Vector3 ToVector3()
            {
                return new Vector3((float)R, (float)G, (float)B);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Rgb24Pixel
        {
            public byte R;
            public byte G;
            public byte B;

            public readonly float Roughness => R / 255.0f;
            public readonly float Metallic => G / 255.0f;
            public readonly bool IsFront => B > 127;
        }

        private sealed class AxisTarget : IDisposable
        {
            public GlTexture Color = null!;
            public GlTexture Normal = null!;
            public GlTexture Material = null!;
            public GlTexture Depth = null!;

            public uint Width;
            public uint Height;
            public uint Layers;

            public void Dispose()
            {
                Color?.Dispose();
                Normal?.Dispose();
                Material?.Dispose();
                Depth?.Dispose();
            }
        }

        private const int NegX = 0;
        private const int PosX = 1;
        private const int NegY = 2;
        private const int PosY = 3;
        private const int NegZ = 4;
        private const int PosZ = 5;

        #endregion

        private readonly GL _gl;
        private readonly int _viewsPerBatch;
        private readonly GlMultiViewFrameBuffer _scanFbo;
        private readonly GlTextureFrameBuffer _texFb;

        private AxisTarget? _xTarget;
        private AxisTarget? _yTarget;
        private AxisTarget? _zTarget;

        private VoxelGridDesc _grid;
        private Vector3I _voxelMin;
        private Vector3I _voxelMax;
        private GlSimpleProgram? _scanProgram;
        private GpuMeshVoxelizerParams _params;
        private readonly Dictionary<Geometry3D, GlVertexSourceHandle> _vertexHandles = [];

        public GpuMeshVoxelizer(GL gl)
        {
            _gl = gl;

            _gl.GetInteger((GLEnum)0x9631, out var maxViews);

            _viewsPerBatch = maxViews;
            _scanFbo = new GlMultiViewFrameBuffer(gl);
            _texFb = new GlTextureFrameBuffer(gl);
            _params = new GpuMeshVoxelizerParams();
        }

        public void SetParams(GpuMeshVoxelizerParams param)
        {
            _params = param;
        }

        GlSimpleProgram LoadProgram(int viewCount)
        {
            return GlImageProc.LoadProgram(_gl,
                "[XrEngine.Lighting]voxelizer_scan.frag",
                "[XrEngine.Lighting]voxelizer_scan.vert",
                [$"VOXELIZER_VIEW_COUNT {viewCount}"], ["GL_OVR_multiview2"])!;
        }

        bool TryComputeVoxelBounds(Bounds3 bounds)
        {
            var gridSize = new Vector3(
                _grid.Size.X * _grid.VoxelSize,
                _grid.Size.Y * _grid.VoxelSize,
                _grid.Size.Z * _grid.VoxelSize);

            var gridMax = _grid.Origin + gridSize;
            var boundsMin = Vector3.Max(bounds.Min, _grid.Origin);
            var boundsMax = Vector3.Min(bounds.Max, gridMax);

            if (boundsMin.X > boundsMax.X ||
                boundsMin.Y > boundsMax.Y ||
                boundsMin.Z > boundsMax.Z)
            {
                return false;
            }

            var padding = Math.Max(0, _params.BoundsPadding);

            var invVoxelSize = 1.0f / _grid.VoxelSize;

            var minX = (int)MathF.Floor((boundsMin.X - _grid.Origin.X) * invVoxelSize) - padding;
            var minY = (int)MathF.Floor((boundsMin.Y - _grid.Origin.Y) * invVoxelSize) - padding;
            var minZ = (int)MathF.Floor((boundsMin.Z - _grid.Origin.Z) * invVoxelSize) - padding;

            var maxX = (int)MathF.Ceiling((boundsMax.X - _grid.Origin.X) * invVoxelSize) + padding;
            var maxY = (int)MathF.Ceiling((boundsMax.Y - _grid.Origin.Y) * invVoxelSize) + padding;
            var maxZ = (int)MathF.Ceiling((boundsMax.Z - _grid.Origin.Z) * invVoxelSize) + padding;

            if (maxX <= minX)
                maxX = minX + 1;

            if (maxY <= minY)
                maxY = minY + 1;

            if (maxZ <= minZ)
                maxZ = minZ + 1;

            _voxelMin = new Vector3I(
                Math.Clamp(minX, 0, _grid.Size.X),
                Math.Clamp(minY, 0, _grid.Size.Y),
                Math.Clamp(minZ, 0, _grid.Size.Z));

            _voxelMax = new Vector3I(
                Math.Clamp(maxX, 0, _grid.Size.X),
                Math.Clamp(maxY, 0, _grid.Size.Y),
                Math.Clamp(maxZ, 0, _grid.Size.Z));

            if (_voxelMin.X >= _voxelMax.X ||
                _voxelMin.Y >= _voxelMax.Y ||
                _voxelMin.Z >= _voxelMax.Z)
            {
                return false;
            }

            return true;
        }

        public void SetGrid(VoxelGridDesc grid)
        {
            _grid = grid;
        }

        public IList<GpuVoxelFaceData> Voxelize(IReadOnlyList<TriangleMesh> meshes)
        {
            var realMeshes = meshes.Where(a =>
            {
                if (!a.IsVisible)
                    return false;
                if (a.TryComponent<LightFieldReceiver>(out var rec))
                    return rec.IsOccluder;
                return true;
            }).ToArray();

            if (realMeshes.Length == 0)
                return [];

            var boundsBuilder = new Bounds3Builder();

            foreach (var mesh in realMeshes)
                boundsBuilder.Add(mesh.WorldBounds);

            var bounds = boundsBuilder.Result;

            if (!TryComputeVoxelBounds(bounds))
                return [];

            EnsureTargets();

            ScanAxisVolume(realMeshes, ScanAxis.X, _xTarget!);
            ScanAxisVolume(realMeshes, ScanAxis.Y, _yTarget!);
            ScanAxisVolume(realMeshes, ScanAxis.Z, _zTarget!);

            var result = new List<GpuVoxelFaceData>();

            _texFb.Bind();
            _texFb.BindRead(ReadBufferMode.ColorAttachment0);

            _gl.Flush();

            ReadAxisVolume(ScanAxis.X, _xTarget!, result);
            ReadAxisVolume(ScanAxis.Y, _yTarget!, result);
            ReadAxisVolume(ScanAxis.Z, _zTarget!, result);

            return result;
        }

        private void EnsureTargets()
        {
            EnsureAxisTarget(ref _xTarget, (uint)_grid.Size.Z, (uint)_grid.Size.Y, (uint)_grid.Size.X);
            EnsureAxisTarget(ref _yTarget, (uint)_grid.Size.X, (uint)_grid.Size.Z, (uint)_grid.Size.Y);
            EnsureAxisTarget(ref _zTarget, (uint)_grid.Size.X, (uint)_grid.Size.Y, (uint)_grid.Size.Z);
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
            target.Normal.Allocate(width, height, layers, TextureFormat.RgbFloat16);

            target.Material = new GlTexture(_gl);
            target.Material.Allocate(width, height, layers, TextureFormat.Rgb24);

            target.Depth = new GlTexture(_gl);
            target.Depth.Allocate(width, height, layers, TextureFormat.Depth24);
        }

        private void ScanAxisVolume(
            IReadOnlyList<TriangleMesh> meshes,
            ScanAxis axis,
            AxisTarget target)
        {
            Log.Debug(this, "Scan axis {0}", axis);

            var glState = GlState.Current;

            glState.SetUseDepth(true);
            glState.SetWriteDepth(true);
            glState.EnableFeature(EnableCap.CullFace, false);
            glState.SetAlphaMode(AlphaMode.Opaque);
            glState.SetColorMask(true, true, true, true, true);

            uint lastProgram = 0;

            int firstSlice;
            int lastSlice;

            switch (axis)
            {
                case ScanAxis.X:
                    firstSlice = _voxelMin.X;
                    lastSlice = _voxelMax.X;
                    break;

                case ScanAxis.Y:
                    firstSlice = _voxelMin.Y;
                    lastSlice = _voxelMax.Y;
                    break;

                default:
                    firstSlice = _voxelMin.Z;
                    lastSlice = _voxelMax.Z;
                    break;
            }

            for (var baseSlice = firstSlice; baseSlice < lastSlice; baseSlice += _viewsPerBatch)
            {
                var viewCount = Math.Min(_viewsPerBatch, lastSlice - baseSlice);

                _scanProgram = LoadProgram(viewCount);

                if (_scanProgram.Handle != lastProgram)
                {
                    _scanProgram.SetUniform("uGridOrigin", _grid.Origin);
                    _scanProgram.SetUniform("uVoxelSize", _grid.VoxelSize);
                    _scanProgram.SetUniform("uGridSize", _grid.Size);
                    _scanProgram.SetUniform("uAxis", (float)axis);
                    lastProgram = _scanProgram.Handle;
                }

                BindScanTarget(target, baseSlice, viewCount);

                GlState.Current!.SetView(new Rect2I(0, 0, target.Width, target.Height));

                _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                _scanProgram.SetUniform("uBaseSlice", baseSlice);

                for (var j = 0; j < _params.Passes; j++)
                {
                    for (var i = 0; i < viewCount; ++i)
                    {
                        var slice = baseSlice + i;
                        var eps = j * _params.AxisEps * _grid.VoxelSize;
                        var matrix = CreateSliceViewProjection(axis, slice, eps);
                        _scanProgram.SetUniform($"uViewProj[{i}]", matrix);
                    }

                    DrawMeshes(meshes);
                }

            }
        }

        private Matrix4x4 CreateSliceViewProjection(ScanAxis axis, int slice, float axisEps)
        {
            var size = new Vector3(
                _grid.Size.X * _grid.VoxelSize,
                _grid.Size.Y * _grid.VoxelSize,
                _grid.Size.Z * _grid.VoxelSize);

            var min = _grid.Origin;
            var max = _grid.Origin + size;

            float d0;
            float d1;

            var result = new Matrix4x4
            {
                M44 = 1.0f
            };

            var eps = MathF.Max(_grid.VoxelSize * 0.001f, 1e-6f);

            switch (axis)
            {
                case ScanAxis.X:
                    d0 = min.X + slice * _grid.VoxelSize - eps + axisEps;
                    d1 = d0 + _grid.VoxelSize + eps + axisEps;

                    result.M31 = 2.0f / (max.Z - min.Z);
                    result.M22 = 2.0f / (max.Y - min.Y);
                    result.M13 = 2.0f / (d1 - d0);

                    result.M41 = -(max.Z + min.Z) / (max.Z - min.Z);
                    result.M42 = -(max.Y + min.Y) / (max.Y - min.Y);
                    result.M43 = -(d1 + d0) / (d1 - d0);
                    break;

                case ScanAxis.Y:
                    d0 = min.Y + slice * _grid.VoxelSize - eps + axisEps;
                    d1 = d0 + _grid.VoxelSize + eps + axisEps;

                    result.M11 = 2.0f / (max.X - min.X);
                    result.M32 = 2.0f / (max.Z - min.Z);
                    result.M23 = 2.0f / (d1 - d0);

                    result.M41 = -(max.X + min.X) / (max.X - min.X);
                    result.M42 = -(max.Z + min.Z) / (max.Z - min.Z);
                    result.M43 = -(d1 + d0) / (d1 - d0);
                    break;

                default:
                    d0 = min.Z + slice * _grid.VoxelSize - eps + axisEps;
                    d1 = d0 + _grid.VoxelSize + eps + axisEps;

                    result.M11 = 2.0f / (max.X - min.X);
                    result.M22 = 2.0f / (max.Y - min.Y);
                    result.M33 = 2.0f / (d1 - d0);

                    result.M41 = -(max.X + min.X) / (max.X - min.X);
                    result.M42 = -(max.Y + min.Y) / (max.Y - min.Y);
                    result.M43 = -(d1 + d0) / (d1 - d0);
                    break;
            }

            return result;
        }

        private void DrawMeshes(IReadOnlyList<TriangleMesh> meshes)
        {
            foreach (var mesh in meshes)
            {
                SetMeshUniforms(mesh);

                var geo = mesh.Geometry!;

                var ctx = Context.Require<IGlContextProvider>().Current;

                if (!_vertexHandles.TryGetValue(geo, out var handle) || handle.VertexArray.Owner != ctx)
                {
                    handle = geo.GetProp<GlVertexSourceHandle>(OpenGLRender.Props.GlResId);

                    handle ??= GlVertexSourceHandle.Create(_gl, mesh);

                    if (handle.VertexArray.Owner != ctx)
                        handle = handle.Clone();

                    _vertexHandles[geo] = handle;
                }

                if (handle.NeedUpdate)
                    handle.Update();

                handle.Bind();
                handle.Draw();
                handle.Unbind();
            }
        }

        private void SetMeshUniforms(TriangleMesh mesh)
        {
            Debug.Assert(_scanProgram != null);

            _scanProgram.SetUniform("uWorld", mesh.WorldMatrix);
            _scanProgram.SetUniform("uNormalMatrix", mesh.NormalMatrix);

            var mat = mesh.Materials.OfType<PbrMaterial>().FirstOrDefault();

            if (mat != null)
            {
                _scanProgram.SetUniform("uBaseColorFactor", mat.Color);
                _scanProgram.SetUniform("uMetallicFactor", mat.Metalness);
                _scanProgram.SetUniform("uRoughnessFactor", mat.Roughness);

                if (mat.ColorMap != null)
                {
                    GlState.Current.LoadTexture(mat.ColorMap.ToGlTexture(), 0);
                    _scanProgram.SetUniform("uHasColorMap", true);
                }
                else
                {
                    _scanProgram.SetUniform("uHasColorMap", false);
                }

                if (mat.MetallicRoughnessMap != null)
                {
                    GlState.Current.LoadTexture(mat.MetallicRoughnessMap.ToGlTexture(), 1);
                    _scanProgram.SetUniform("uHasMetallicRoughnessMap", true);
                }
                else
                {
                    _scanProgram.SetUniform("uHasMetallicRoughnessMap", false);
                }
            }
            else
            {
                _scanProgram.SetUniform("uBaseColorFactor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                _scanProgram.SetUniform("uMetallicFactor", 0.0f);
                _scanProgram.SetUniform("uRoughnessFactor", 0.8f);
                _scanProgram.SetUniform("uHasColorMap", false);
                _scanProgram.SetUniform("uHasMetallicRoughnessMap", false);
            }
        }

        private void ReadAxisVolume(
            ScanAxis axis,
            AxisTarget target,
            List<GpuVoxelFaceData> faces)
        {
            Log.Debug(this, "Read axis {0}", axis);

            var colors = Array.Empty<Rgba32Pixel>();
            var normals = Array.Empty<RgbHalfPixel>();
            var materials = Array.Empty<Rgb24Pixel>();

            _gl.PixelStore(PixelStoreParameter.PackAlignment, 1);

            int firstLayer;
            int lastLayer;
            Rect2I readRect;

            switch (axis)
            {
                case ScanAxis.X:
                    firstLayer = _voxelMin.X;
                    lastLayer = _voxelMax.X;
                    readRect = new Rect2I(
                        _voxelMin.Z,
                        _voxelMin.Y,
                        (uint)(_voxelMax.Z - _voxelMin.Z),
                        (uint)(_voxelMax.Y - _voxelMin.Y));
                    break;

                case ScanAxis.Y:
                    firstLayer = _voxelMin.Y;
                    lastLayer = _voxelMax.Y;
                    readRect = new Rect2I(
                        _voxelMin.X,
                        _voxelMin.Z,
                        (uint)(_voxelMax.X - _voxelMin.X),
                        (uint)(_voxelMax.Z - _voxelMin.Z));
                    break;

                default:
                    firstLayer = _voxelMin.Z;
                    lastLayer = _voxelMax.Z;
                    readRect = new Rect2I(
                        _voxelMin.X,
                        _voxelMin.Y,
                        (uint)(_voxelMax.X - _voxelMin.X),
                        (uint)(_voxelMax.Y - _voxelMin.Y));
                    break;
            }

            for (var layer = firstLayer; layer < lastLayer; ++layer)
            {
                ReadLayer(target.Color, readRect, layer, ref colors);
                ReadLayer(target.Normal, readRect, layer, ref normals);
                ReadLayer(target.Material, readRect, layer, ref materials);

                UnpackAxisLayer(
                    axis,
                    layer,
                    readRect,
                    colors,
                    normals,
                    materials,
                    faces);
            }
        }

        private void ReadLayer<T>(
            GlTexture texture,
            Rect2I rect,
            int layer,
            ref T[] result) where T : struct
        {

            _texFb.Attach(texture, FramebufferAttachment.ColorAttachment0, false, layer);
            _texFb.Check();

            var len = checked((int)(rect.Width * rect.Height));

            if (result.Length < len)
                result = new T[len];

            GlUtils.GetPixelFormat(texture.InternalFormat.ToTextureFormat(), out var pf, out var pt);

            unsafe
            {
                fixed (T* ptr = result)
                {
                    _gl.ReadPixels(
                        rect.X,
                        rect.Y,
                        rect.Width,
                        rect.Height,
                        pf,
                        pt,
                        ptr);
                }
            }
        }

        private void UnpackAxisLayer(
            ScanAxis axis,
            int layer,
            Rect2I rect,
            Rgba32Pixel[] colors,
            RgbHalfPixel[] normals,
            Rgb24Pixel[] materials,
            List<GpuVoxelFaceData> faces)
        {
            var width = (int)rect.Width;
            var height = (int)rect.Height;

            for (var py = 0; py < height; ++py)
            {
                for (var px = 0; px < width; ++px)
                {
                    var srcIndex = px + py * width;

                    var color = colors[srcIndex].ToColor();

                    if (color.A <= 0.0f)
                        continue;

                    AddVoxelFace(
                        axis,
                        layer,
                        rect.X + px,
                        rect.Y + py,
                        color,
                        normals[srcIndex].ToVector3(),
                        materials[srcIndex],
                        faces);
                }
            }
        }

        private void AddVoxelFace(
            ScanAxis axis,
            int layer,
            int px,
            int py,
            Color color,
            Vector3 normal,
            Rgb24Pixel material,
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
                    y = py;
                    z = px;
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

            var isFront = material.IsFront;

            faces.Add(new GpuVoxelFaceData
            {
                Cell = new Vector3I(x, y, z),
                Face = isFront ? frontFace : backFace,
                Side = isFront ? VoxelTriangleSide.Front : VoxelTriangleSide.Back,

                BaseColor = color,
                Normal = normal,
                Roughness = material.Roughness,
                Metallic = material.Metallic
            });

            if (_params.AddBackFaces)
            {
                faces.Add(new GpuVoxelFaceData
                {
                    Cell = new Vector3I(x, y, z),
                    Face = !isFront ? frontFace : backFace,
                    Side = !isFront ? VoxelTriangleSide.Front : VoxelTriangleSide.Back,

                    BaseColor = color,
                    Normal = -normal,
                    Roughness = material.Roughness,
                    Metallic = material.Metallic
                });
            }

        }

        private void BindScanTarget(AxisTarget target, int baseLayer, int viewCount)
        {
            _scanFbo.BaseViewIndex = (uint)baseLayer;
            _scanFbo.NumViews = (uint)viewCount;

            _scanFbo.BindDraw(
                DrawBufferMode.ColorAttachment0,
                DrawBufferMode.ColorAttachment1,
                DrawBufferMode.ColorAttachment2);

            _scanFbo.Attach(target.Color, FramebufferAttachment.ColorAttachment0, true);
            _scanFbo.Attach(target.Normal, FramebufferAttachment.ColorAttachment1, true);
            _scanFbo.Attach(target.Material, FramebufferAttachment.ColorAttachment2, true);
            _scanFbo.Attach(target.Depth, FramebufferAttachment.DepthAttachment, false);
        }

        public void Dispose()
        {
            _xTarget?.Dispose();
            _yTarget?.Dispose();
            _zTarget?.Dispose();
            _scanFbo?.Dispose();
            _texFb.Dispose();
        }
    }
}