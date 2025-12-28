
using Common.Interop;
using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using XrEngine.Media;
using XrMath;


namespace XrEngine.Reconstruct
{

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
        private static readonly bool _fixView = false;
        private QuestSensorFusion? _fusionLeft;

        static readonly JsonSerializerOptions JSON_OPT = new JsonSerializerOptions()
        {
            IncludeFields = true,
        };


        public XrReconstructReader()
        {



        }


        public void Open(string path)
        {
            _basePath = path;

            var outZPath = Path.Combine(path, "out-z.bin");
            var outMetaPath = Path.Combine(path, "out-meta.json");
            var outPath1 = Path.Combine(path, "outL.mp4");
            var outPath2 = Path.Combine(path, "outR.mp4");

            var lines = File.ReadAllLines(outMetaPath);

            _meta = new List<RecordFrameData>();

            foreach (var line in lines)
            {
                var info = JsonSerializer.Deserialize<RecordFrameData>(line, JSON_OPT)!;
                _meta.Add(info);

                if (info.LeftColor?.CameraParams != null)
                    _fusionLeft = new QuestSensorFusion(info.LeftColor.CameraParams);
            }

            using var zStreamZip = new GZipStream(File.OpenRead(outZPath), CompressionMode.Decompress);
            _zStream = File.Open(Path.Combine(path, "out-z-dec.bin"), FileMode.OpenOrCreate);
            zStreamZip.CopyTo(_zStream);

            _leftColorReader = Context.RequireNew<IVideoReader>();
            _rightColorReader = Context.RequireNew<IVideoReader>();

            _leftColorReader.Open(new Uri(outPath1), TextureFormat.Rgb24);
            _rightColorReader.Open(new Uri(outPath2), TextureFormat.Rgb24);


        }

        public void ReconstructDepth(DepthFrame frame, uint width, uint height)
        {
            var zData = frame.Data!.AsSpan();

            frame.ProjData = MemoryBuffer.CreateOrResize(frame.ProjData, (uint)zData.Length);

            var zProjData = frame.ProjData!.AsSpan();

            var camera = new PerspectiveCamera();

            camera.View = frame.View;
            camera.Projection = frame.Proj;
            camera.ViewSize = new Size2I(width, height);

            for (var y = 0; y < height; y++)
            {
                var ySrc = (int)(height - 1 - y);
                for (var x = 0; x < width; x++)
                {
                    var srcIndex = ySrc * (int)width + x;
                    var dstIndex = y * (int)width + x;

                    var z = zData[srcIndex] / (float)ushort.MaxValue;

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


        public void ComputeStatsProj(DepthFrame frame, float cutPerc = 5, float bucketSize = 0.01f)
        {
            var stats = frame.StatsProj;

            stats.Min = frame.ProjData!.AsSpan()[0];
            stats.Max = stats.Min;

            foreach (var point in frame.ProjData.AsSpan())
            {
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
                imgData[i] = (byte)(Math.Clamp((axis(projData[i]) - min) / (max - min), 0f, 1f) * 255);

        }

        public EyesFrame<ColorFrame> ReadColor(int frameIndex)
        {
            Debug.Assert(_leftColorReader != null);
            Debug.Assert(_rightColorReader != null);

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
                _leftColorReader.SeekToFrame(frameIndex);
                _leftColorReader.TryDecodeNextFrame(leftData);
                File.WriteAllBytes(cacheLeft, leftData.Data!.AsSpan());
            }


            result.Left.Proj = MathUtils.CreateMatrix(_meta[frameIndex].LeftColor!.Proj!);
            result.Left.View = MathUtils.CreateMatrix(_meta[frameIndex].LeftColor!.View!);
            result.Left.Pose = _meta[frameIndex].LeftColor!.Pose;
            result.Left.Data = leftData.Data;
            result.Width = leftData.Width;
            result.Height = leftData.Height;
            result.Time = _meta[frameIndex].LeftColor!.Time;

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
                File.WriteAllBytes(cacheRight, rightData.Data!.AsSpan());
            }


            result.Right.Proj = MathUtils.CreateMatrix(_meta[frameIndex].RightColor!.Proj!);
            result.Right.View = MathUtils.CreateMatrix(_meta[frameIndex].RightColor!.View!);
            result.Right.Pose = _meta[frameIndex].RightColor!.Pose;
            result.Right.Data = rightData.Data;

            if (_fixView)
            {
                Matrix4x4.Invert(result.Right.Proj, out var projInv);
                result.Right.View = Matrix4x4.Multiply(result.Right.View, projInv);

                Matrix4x4.Invert(result.Left.Proj, out projInv);
                result.Left.View = Matrix4x4.Multiply(result.Left.View, projInv);
            }


            return result;
        }

        public EyesFrame<DepthFrame> ReadDepth(int frameIndex)
        {
            var result = new EyesFrame<DepthFrame>()
            {
                Width = 320,
                Height = 320,
                Time = _meta![frameIndex].LeftDepth!.Time,
                Frame = (uint)frameIndex
            };

            var size = result.Width * result.Height;
            var sizeBytes = size * 2;
            var ofs = frameIndex * sizeBytes * 2;

            _zStream!.Position = ofs;

            result.Left.Data = MemoryBuffer.CreateOrResize(result.Left.Data, size);
            result.Left.Proj = MathUtils.CreateMatrix(_meta![frameIndex].LeftDepth!.Proj!);
            result.Left.View = MathUtils.CreateMatrix(_meta![frameIndex].LeftDepth!.View!);


            result.Right.Data = MemoryBuffer.CreateOrResize(result.Left.Data, size);
            result.Right.Proj = MathUtils.CreateMatrix(_meta![frameIndex].RightDepth!.Proj!);
            result.Right.View = MathUtils.CreateMatrix(_meta![frameIndex].RightDepth!.View!);

            var byteSpan = MemoryMarshal.Cast<ushort, byte>(result.Left.Data.AsSpan());

            _zStream.ReadExactly(byteSpan);

            byteSpan = MemoryMarshal.Cast<ushort, byte>(result.Right.Data.AsSpan());
            _zStream.ReadExactly(byteSpan);

            //Fix view
            if (_fixView)
            {
                Matrix4x4.Invert(result.Right.Proj, out var projInv);
                result.Right.View = Matrix4x4.Multiply(result.Right.View, projInv);

                Matrix4x4.Invert(result.Left.Proj, out projInv);
                result.Left.View = Matrix4x4.Multiply(result.Left.View, projInv);
            }
            return result;

        }

        public int FindColorForDepth(int frameIndex)
        {
            var depthTime = _meta![frameIndex].Time;
            var minFrame = 0;
            var minDif = long.MaxValue;
            for (var i = 0; i < _meta.Count; i++)
            {
                var colorTime = _meta[i].LeftColor!.Time;
                var diff = Math.Abs(colorTime - depthTime);
                if (diff < minDif)
                {
                    minDif = diff;
                    minFrame = i;
                }
            }
            return minFrame;
        }


        public int FindColorForDepthV2(int frameIndex)
        {
            var leftDepth = GetWord(_meta![frameIndex].LeftDepth!);
            var minFrame = 0;
            var minDif = float.MaxValue;
            for (var i = 0; i < _meta.Count; i++)
            {
                var leftColor = GetWord(_meta![i].LeftColor!);

                var diff = (leftColor.Translation - leftDepth.Translation).Length();
                if (diff < minDif)
                {
                    minDif = diff;
                    minFrame = i;
                }
            }
            return minFrame;
        }

        public static Matrix4x4 GetView(EyeData eye)
        {
            var view = MathUtils.CreateMatrix(eye.View!);

            if (_fixView)
            {
                var proj = MathUtils.CreateMatrix(eye.Proj!);
                Matrix4x4.Invert(proj, out var projInv);
                view = view * projInv;
            }
            return view;
        }

        public static Matrix4x4 GetWord(EyeData eye)
        {
            var view = GetView(eye);
            Matrix4x4.Invert(view, out var viewInv);
            return viewInv;
        }

        public IList<RecordFrameData>? Meta => _meta;

        public QuestSensorFusion FusionLeft => _fusionLeft;

    }
}
