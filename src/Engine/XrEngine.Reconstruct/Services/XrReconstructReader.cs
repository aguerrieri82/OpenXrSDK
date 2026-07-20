using Common.Interop;
using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using XrEngine.Devices;
using XrEngine.Media;
using XrMath;

namespace XrEngine.Reconstruct
{
    public class ExportFrame
    {
        public float[]? PoseMatrix { get; set; }

        public int FrameNumber { get; set; }

        public string? FramePath { get; set; }
    }

    public struct FrameStats<T> where T : struct
    {
        public T Min;

        public T Max;

        public int[]? Histogram;

        public T CutLower;

        public T CutUpper;
    }

    public class DepthFrame
    {
        public IMemoryBuffer<Vector3>? ProjData { get; set; }

        public Matrix4x4 View { get; set; }

        public Matrix4x4 Proj { get; set; }

        public IMemoryBuffer<ushort>? Data { get; set; }

        public IMemoryBuffer<byte>? ImageData { get; set; }

        public FrameStats<ushort> StatsZ { get; set; }

        public FrameStats<Vector3> StatsProj { get; set; }

    }

    public class ColorFrame
    {
        public IMemoryBuffer<byte>? Data { get; set; }

        public Matrix4x4 View { get; set; }

        public Matrix4x4 Proj { get; set; }

        public Pose3? Pose { get; set; }

        public long Time { get; set; }
    }

    public class EyesFrame<T> where T : new()
    {
        public EyesFrame()
        {
            Left = new();
            Right = new();
        }

        public T Left { get; }

        public T Right { get; }

        public uint Width { get; set; }

        public uint Height { get; set; }

        public long Time { get; set; }

        public uint Frame { get; set; }
    }

    public class XrReconstructReader
    {
        IList<RecordFrameData> _meta = [];
        Stream? _zStream;
        private IVideoReader? _leftColorReader;
        private IVideoReader? _rightColorReader;
        private string? _basePath;
        private IVideoReader? _scrColorReader;
        private RecordStats? _stats;
        private TriangleMesh? _sceneModel;

        static readonly JsonSerializerOptions JSON_OPT = new JsonSerializerOptions()
        {
            IncludeFields = true,
        };

        public XrReconstructReader()
        {
        }

        public void ExportFrames(string outPath)
        {
            var frameArray = new List<ExportFrame>();
            var lastFrameIndex = -1;
            var toSkip = 0;
            foreach (var item in _meta.Take(_meta.Count - 6))
            {
                toSkip++;

                if (lastFrameIndex == item.LeftColor!.Frame)
                    continue;

                if ((toSkip % 10) != 0)
                    continue;

                ReadColor(item.LeftColor!.Frame);

                var obj = new ExportFrame()
                {
                    PoseMatrix = item.LeftColor.Pose!.Value.ToMatrix().ToFloatArray(),
                    FrameNumber = item.LeftColor.Frame,
                    FramePath = Path.Combine(_basePath!, "Frames", "Left" + item.LeftColor.Frame + ".img")
                };
                frameArray.Add(obj);
                lastFrameIndex = item.LeftColor.Frame;
            }
            File.WriteAllText(outPath, JsonSerializer.Serialize(frameArray));
        }

        public unsafe void Open(string path)
        {
            _basePath = path;

            var outZPath = Path.Combine(path, "out-z.bin");
            var outMetaPath = Path.Combine(path, "out-meta.json");
            var outPath1 = Path.Combine(path, "outL.mp4");
            var outPath2 = Path.Combine(path, "outR.mp4");
            var outPath3 = Path.Combine(path, "outScr.mp4");
            var statsPath = Path.Combine(path, "stats.json");
            var scenePath = Path.Combine(path, "scene.obj");

            var lines = File.ReadAllLines(outMetaPath);

            _meta = new List<RecordFrameData>();

            foreach (var line in lines)
            {
                var info = JsonSerializer.Deserialize<RecordFrameData>(line, JSON_OPT)!;
                _meta.Add(info);

                if (info.LeftColor?.CameraParams != null)
                    LeftCamera = info.LeftColor.CameraParams;

                if (info.RightColor?.CameraParams != null)
                    RightCamera = info.RightColor.CameraParams;
            }

            using var zStreamZip = new GZipStream(File.OpenRead(outZPath), CompressionMode.Decompress);
            _zStream = File.Open(Path.Combine(path, "out-z-dec.bin"), FileMode.OpenOrCreate);
            zStreamZip.CopyTo(_zStream);

            _leftColorReader = Context.RequireNew<IVideoReader>();
            _rightColorReader = Context.RequireNew<IVideoReader>();
            _scrColorReader = Context.RequireNew<IVideoReader>();

            _leftColorReader.Open(new Uri(outPath1), TextureFormat.Rgb24);
            _rightColorReader.Open(new Uri(outPath2), TextureFormat.Rgb24);
            _scrColorReader.Open(new Uri(outPath3), TextureFormat.Rgb24);

            _stats = JsonSerializer.Deserialize<RecordStats>(File.ReadAllText(statsPath), JSON_OPT)!;

            _sceneModel = AssetLoader.Instance.Load<TriangleMesh>(scenePath);

            /*
            var img = new PngLib.ImageData();
            var pngBuf = new PngLib.MemoryBuffer();

            var stream = new MemoryStream();

            var startFrame = _meta.Min(a => a.LeftDepth.Frame);
            var endFrame = _meta.Max(a => a.LeftDepth.Frame);

            var count = 0;

            for (var i = startFrame; i <= endFrame; i++)
            {
                var frame = ReadDepth(i);
                if (frame == null)
                {
                    Debug.WriteLine("Missing frame {0}", i);
                    continue;
                }

                count++;
                using var leftData = frame!.Left.Data!.MemoryLock();
                using var rightData = frame.Right.Data!.MemoryLock();

                PngLib.EncodeGray16(leftData, (int)frame.Width, (int)frame.Height, 3, ref pngBuf);
                stream.Write(pngBuf.Span);

                PngLib.EncodeGray16(rightData, (int)frame.Width, (int)frame.Height, 3, ref pngBuf);
                stream.Write(pngBuf.Span);

                Log.Info(this, "Compressing {0} - {1} MB", i, stream.Length / 1024 / 1024);
            }

            pngBuf.Dispose();
            */
        }

        public void ReconstructDepth(DepthFrame frame, uint width, uint height, float zCutOff, uint maxW = 320)
        {
            var zData = frame.Data!.AsSpan();

            frame.ProjData = MemoryBuffer.CreateOrResize(frame.ProjData, (uint)zData.Length);

            var zProjData = frame.ProjData!.AsSpan();

            var camera = new PerspectiveCamera
            {
                View = frame.View,
                Projection = frame.Proj,
                ViewSize = new Size2I(width, height)
            };

            if (maxW == 0)
                maxW = width;

            for (var y = 0; y < height; y++)
            {
                var ySrc = (int)(height - 1 - y);
                for (var x = 0; x < maxW; x++)
                {
                    var srcIndex = ySrc * (int)width + x;
                    var dstIndex = y * (int)width + x;

                    var z = zData[srcIndex] / (float)ushort.MaxValue;

                    if (z > zCutOff)
                        continue;

                    var ndcX = ((x + 0.5f) / width) * 2f - 1f;
                    var ndcY = 1f - ((y + 0.5f) / height) * 2f;
                    var ndcZ = z * 2f - 1f;

                    zProjData[dstIndex] = camera.Unproject(new Vector3(ndcX, ndcY, ndcZ));
                }
            }
        }

        public void ComputeStats(DepthFrame frame, float cutPerc = 5)
        {
            var stats = frame.StatsZ;

            stats.Histogram = new int[ushort.MaxValue + 1];

            stats.Min = frame.Data!.AsSpan()[0];
            stats.Max = stats.Min;

            foreach (var point in frame.Data.AsSpan())
            {
                stats.Min = Math.Min(stats.Min, point);
                stats.Max = Math.Max(stats.Max, point);
                stats.Histogram[point]++;
            }

            var pointSum = 0;
            var cutTrade = (int)(frame.Data.Size * (cutPerc / 100.0f));
            for (var i = 0; i < stats.Histogram.Length; i++)
            {
                pointSum += stats.Histogram[i];
                if (pointSum >= cutTrade)
                {
                    stats.CutLower = (ushort)i;
                    break;
                }
            }
            pointSum = 0;
            for (var i = stats.Histogram.Length - 1; i >= 0; i--)
            {
                pointSum += stats.Histogram[i];
                if (pointSum >= cutTrade)
                {
                    stats.CutUpper = (ushort)i;
                    break;
                }
            }

            frame.StatsZ = stats;
        }

        public void ComputeStatsProj(DepthFrame frame)
        {
            var stats = frame.StatsProj;

            stats.Min = frame.ProjData!.AsSpan()[0];
            stats.Max = stats.Min;

            foreach (var point in frame.ProjData.AsSpan())
            {
                if (!point.IsFinite())
                    continue;
                stats.Min = Vector3.Min(stats.Min, point);
                stats.Max = Vector3.Max(stats.Max, point);
            }

            frame.StatsProj = stats;
        }

        public void GenerateImage(DepthFrame frame, ushort min, ushort max)
        {
            var zData = frame.Data!.AsSpan();

            frame.ImageData = MemoryBuffer.CreateOrResize(frame.ImageData, (uint)zData.Length);

            var imgData = frame.ImageData!.AsSpan();

            for (var i = 0; i < zData.Length; i++)
                imgData[i] = (byte)(Math.Clamp((zData[i] - min) / (float)(max - min), 0f, 1f) * 255);

        }

        public void GenerateImage(DepthFrame frame, Func<Vector3, float> axis, float min, float max)
        {
            var projData = frame.ProjData!.AsSpan();

            frame.ImageData = MemoryBuffer.CreateOrResize(frame.ImageData, (uint)projData.Length);

            var imgData = frame.ImageData!.AsSpan();

            for (var i = 0; i < projData.Length; i++)
            {
                if (projData[i] == Vector3.Zero)
                    imgData[i] = 0;
                else
                    imgData[i] = (byte)(Math.Clamp((axis(projData[i]) - min) / (max - min), 0f, 1f) * 255);

            }

        }

        public EyesFrame<ColorFrame> ReadColor(int frameIndex)
        {
            Debug.Assert(_leftColorReader != null);
            Debug.Assert(_rightColorReader != null);

            var leftIndex = _meta.Index().Where(a => a.Item.LeftColor!.Frame == frameIndex).Select(a => a.Index).FirstOrDefault();
            var rightIndex = _meta.Index().Where(a => a.Item.RightColor!.Frame == frameIndex).Select(a => a.Index).FirstOrDefault();

            var leftMeta = _meta[leftIndex].LeftColor!;
            var rightMeta = _meta[rightIndex].RightColor!;

            var result = new EyesFrame<ColorFrame>();
            var leftData = new TextureData();
            var rightData = new TextureData();

            var cacheLeft = Path.Combine(_basePath!, "Frames", "Left" + frameIndex + ".img");
            var cacheRight = Path.Combine(_basePath!, "Frames", "Right" + frameIndex + ".img");

            if (File.Exists(cacheLeft))
            {
                var bytes = File.ReadAllBytes(cacheLeft);
                leftData.Data = MemoryBuffer.Create(bytes);
                leftData.Width = 1280;
                leftData.Height = 1280;
                leftData.Format = TextureFormat.Rgb24;
            }
            else
            {
                _leftColorReader!.SeekToFrame(frameIndex - 1);
                _leftColorReader!.TryDecodeNextFrame(leftData);
                File.WriteAllBytes(cacheLeft, leftData.Data!.AsSpan());
            }

            result.Left.Proj = MathUtils.CreateMatrix(leftMeta.Proj!);
            result.Left.View = MathUtils.CreateMatrix(leftMeta.View!);

            result.Left.Data = leftData.Data;
            result.Left.Pose = leftMeta.Pose;
            result.Width = leftData.Width;
            result.Height = leftData.Height;
            result.Time = leftMeta.Time;

            if (File.Exists(cacheRight))
            {
                var bytes = File.ReadAllBytes(cacheRight);
                rightData.Data = MemoryBuffer.Create(bytes);
                rightData.Width = 1280;
                rightData.Height = 1280;
                rightData.Format = TextureFormat.Rgb24;
            }
            else
            {
                _rightColorReader.SeekToFrame(frameIndex);
                _rightColorReader.TryDecodeNextFrame(rightData);
                if (rightData.Data != null)
                    File.WriteAllBytes(cacheRight, rightData.Data.AsSpan());
            }

            result.Right.Proj = MathUtils.CreateMatrix(rightMeta.Proj!);
            result.Right.View = MathUtils.CreateMatrix(rightMeta.View!);
            result.Right.Pose = rightMeta.Pose;
            result.Right.Data = rightData.Data;

            return result;
        }

        public ColorFrame ReadScreen(int frameIndex)
        {
            var index = _meta.Index().Where(a => a.Item.Screen!.Frame == frameIndex).Select(a => a.Index).FirstOrDefault();

            var meta = _meta[index].Screen!;

            Debug.Assert(_scrColorReader != null);

            var result = new ColorFrame();
            var data = new TextureData();

            var cache = Path.Combine(_basePath!, "Frames", "Screen" + frameIndex + ".img");

            if (File.Exists(cache))
            {
                var bytes = File.ReadAllBytes(cache);
                data.Data = MemoryBuffer.Create(bytes);
                data.Width = 1280;
                data.Height = 1280;
                data.Format = TextureFormat.Rgb24;
            }
            else
            {
                _scrColorReader!.SeekToFrame(frameIndex);
                _scrColorReader!.TryDecodeNextFrame(data);
                File.WriteAllBytes(cache, data.Data!.AsSpan());
            }

            result.Time = meta.Time;
            result.Pose = meta.Pose;
            result.Data = data.Data;
            result.View = MathUtils.CreateMatrix(meta.View!);

            return result;
        }

        public int[,,] ComputeVoxels(Bounds3 maxVolume, float voxelSize, int startFrame, int endFrame, float zCutOff, uint maxW = 288)
        {
            var lastFrame = -1;

            var size = maxVolume.Size / voxelSize;
            var counters = new int[(int)size.X + 1, (int)size.Y + 1, (int)size.Z + 1];

            foreach (var item in _meta)
            {
                var frame = item.LeftDepth!.Frame;

                if (lastFrame == frame)
                    continue;

                if (frame < startFrame)
                    continue;

                if (frame > endFrame)
                    break;

                var depth = ReadDepth(frame);
                if (depth == null)
                    continue;

                ReconstructDepth(depth.Left, depth.Width, depth.Height, zCutOff, maxW);

                foreach (var v in depth.Left.ProjData!.AsSpan())
                {
                    if (!v.IsFinite())
                        continue;

                    if (v.X > maxVolume.Max.X || v.Y > maxVolume.Max.Y || v.Z > maxVolume.Max.Z)
                        continue;

                    if (v.X < maxVolume.Min.X || v.Y < maxVolume.Min.Y || v.Z < maxVolume.Min.Z)
                        continue;

                    var pos = (v - maxVolume.Min) / voxelSize;

                    counters[(int)pos.X, (int)pos.Y, (int)pos.Z]++;
                }

                lastFrame = frame;
            }

            return counters;
        }

        public IList<Vector3> ExtractPoints(int[,,] volume, Bounds3 bounds, float voxelSize, int cutOff)
        {
            var result = new List<Vector3>();

            var halfVoxel = voxelSize * 0.5f;

            for (var x = 0; x < volume.GetLength(0); x++)
            {
                for (var y = 0; y < volume.GetLength(1); y++)
                {
                    for (var z = 0; z < volume.GetLength(2); z++)
                    {
                        var value = volume[x, y, z];
                        if (value < cutOff)
                            continue;

                        result.Add(new Vector3(
                            x * voxelSize + bounds.Min.X + halfVoxel,
                            y * voxelSize + bounds.Min.Y + halfVoxel,
                            z * voxelSize + bounds.Min.Z + halfVoxel
                        ));
                    }
                }
            }

            return result;
        }

        public void SavePoints(string outPath, IList<Vector3> points)
        {
            if (File.Exists(outPath))
                File.Delete(outPath);
            using var stream = new StreamWriter(File.OpenWrite(outPath));
            foreach (var p in points)
                stream.WriteLine(string.Format("{0} {1} {2}", p.X, p.Y, p.Z));
        }

        public EyesFrame<DepthFrame>? ReadDepth(int frameIndex)
        {
            var index = _meta.Index().Where(a => a.Item.LeftDepth!.Frame == frameIndex).Select(a => (int?)a.Index).FirstOrDefault() ?? -1;
            if (index == -1)
                return null;

            var meta = _meta![index];

            var result = new EyesFrame<DepthFrame>()
            {
                Width = 320,
                Height = 320,
                Time = meta.LeftDepth!.Time / 1000,
                Frame = (uint)meta.LeftDepth.Frame
            };

            var size = result.Width * result.Height;
            var sizeBytes = size * 2;
            var ofs = (frameIndex - 1) * sizeBytes * 2;

            _zStream!.Position = ofs;

            result.Left.Data = MemoryBuffer.CreateOrResize(result.Left.Data, size);
            result.Left.Proj = MathUtils.CreateMatrix(meta.LeftDepth!.Proj!);
            result.Left.View = MathUtils.CreateMatrix(meta.LeftDepth!.View!);

            result.Right.Data = MemoryBuffer.CreateOrResize(result.Right.Data, size);
            result.Right.Proj = MathUtils.CreateMatrix(meta.RightDepth!.Proj!);
            result.Right.View = MathUtils.CreateMatrix(meta.RightDepth!.View!);

            var byteSpan = MemoryMarshal.Cast<ushort, byte>(result.Left.Data.AsSpan());

            _zStream.ReadExactly(byteSpan);

            byteSpan = MemoryMarshal.Cast<ushort, byte>(result.Right.Data.AsSpan());
            _zStream.ReadExactly(byteSpan);

            return result;

        }

        public int FindColorForDepth(int frameIndex)
        {
            var index = _meta.Index().Where(a => a.Item.LeftDepth!.Frame == frameIndex).Select(a => a.Index).FirstOrDefault();

            var depthTime = _meta![index].LeftDepth!.Time / 1000;
            var minFrame = 0;
            var minDif = long.MaxValue;
            for (var i = 0; i < _meta.Count; i++)
            {
                var colorTime = _meta[i].LeftColor!.Time;
                var diff = Math.Abs(colorTime - depthTime);
                if (diff < minDif)
                {
                    minDif = diff;
                    minFrame = _meta[i].LeftColor!.Frame;
                }
            }
            return minFrame;
        }

        public int FindScreenForDepth(int frameIndex)
        {
            var index = _meta.Index().Where(a => a.Item.LeftDepth!.Frame == frameIndex).Select(a => a.Index).FirstOrDefault();

            var depthTime = _meta![index].LeftDepth!.Time / 1000;
            var minFrame = 0;
            var minDif = long.MaxValue;
            for (var i = 0; i < _meta.Count; i++)
            {
                var screenTime = _meta[i].Screen!.Time;
                var diff = Math.Abs(screenTime - depthTime);
                if (diff < minDif)
                {
                    minDif = diff;
                    minFrame = _meta[i].Screen!.Frame;
                }
            }
            return minFrame;
        }

        public unsafe byte[] AlignColorToDepth(
            Span<Vector3> depthPixels,
            int depthW,
            int depthH,
            Span<byte> colorPixels,
            int colorW,
            int colorH,
            Matrix4x4 colorViewProj,
            bool flipY = true,
            bool flipX = false)
        {
            var result = new byte[depthW * depthH * 3];

            fixed (Vector3* pDepth = depthPixels)
            fixed (byte* pColor = colorPixels)
            fixed (byte* pResult = result)
            {
                var dstIndex = 0;

                for (var y = 0; y < depthH; y++)
                {
                    for (var x = 0; x < depthW; x++)
                    {
                        var worldPos = pDepth[dstIndex];

                        var colorClip = Vector4.Transform(new Vector4(worldPos, 1.0f), colorViewProj);

                        if (colorClip.W == 0)
                        {
                            dstIndex++;
                            continue;
                        }

                        var invW = 1.0f / colorClip.W;

                        var u = colorClip.X * invW * 0.5f + 0.5f;
                        var v = colorClip.Y * invW * 0.5f + 0.5f;

                        if (flipY)
                            v = 1.0f - v;

                        if (flipX)
                            u = 1.0f - u;

                        var cX = (int)(u * colorW);
                        var cY = (int)(v * colorH);

                        if (cX >= 0 && cX < colorW && cY >= 0 && cY < colorH)
                        {
                            var dstIdx = dstIndex * 3;
                            var srcIdx = (cY * colorW + cX) * 3;

                            pResult[dstIdx] = pColor[srcIdx];
                            pResult[dstIdx + 1] = pColor[srcIdx + 1];
                            pResult[dstIdx + 2] = pColor[srcIdx + 2];
                        }

                        dstIndex++;
                    }
                }
            }

            return result;
        }

        public static Matrix4x4 GetView(EyeData eye)
        {
            return MathUtils.CreateMatrix(eye.View!);
        }

        public static Matrix4x4 GetWord(EyeData eye)
        {
            var view = GetView(eye);
            Matrix4x4.Invert(view, out var viewInv);
            return viewInv;
        }

        public IList<RecordFrameData>? Meta => _meta;

        public RecordStats? Stats => _stats;

        public CameraParams? LeftCamera { get; protected set; }

        public CameraParams? RightCamera { get; protected set; }

        public TriangleMesh? SceneModel => _sceneModel;

        public static readonly XrReconstructReader Current = new XrReconstructReader();
    }
}
