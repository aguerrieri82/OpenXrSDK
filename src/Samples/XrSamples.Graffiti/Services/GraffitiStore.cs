using SkiaSharp;
using System.Numerics;
using System.Text.Json;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples.Graffiti
{
    public class GraffitiInfo
    {
        public Guid Id { get; set; }

        public DateTime Date { get; set; }

        public Vector2 Size { get; set; }

        public float TexelSize { get; set; }

        public Guid? AnchorId { get; set; }

        public Pose3 AnchorPose { get; set; }
    }

    public class GraffitiSession
    {
        public GraffitiInfo Info { get; set; } = new();

        public Guid? CurrentId { get; set; }

        public string? RecordingPath { get; set; }

        public int RecordingCount { get; set; }
    }

    public class GraffitiStore
    {
        static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            IncludeFields = true,
            WriteIndented = true
        };

        readonly string _path;
        readonly string _sessionPath;
        Guid? _currentId;

        public GraffitiStore()
        {
            _path = Path.Combine(XrPlatform.Current!.PersistentPath, "Graffiti", "Saves");
            _sessionPath = Path.Combine(XrPlatform.Current!.PersistentPath, "Graffiti", "Session");
            Directory.CreateDirectory(_path);
        }

        public IList<GraffitiInfo> List()
        {
            return Directory.EnumerateDirectories(_path)
                .Select(a => Path.Combine(a, "meta.json"))
                .Where(File.Exists)
                .Select(a => JsonSerializer.Deserialize<GraffitiInfo>(File.ReadAllText(a), JSON_OPTIONS)!)
                .OrderByDescending(a => a.Date)
                .ToList();
        }

        public string GetThumbnail(Guid id)
        {
            var bytes = File.ReadAllBytes(Path.Combine(_path, id.ToString(), "thumb.png"));
            return "data:image/png;base64," + Convert.ToBase64String(bytes);
        }

        public async Task ClearAsync()
        {
            await EngineApp.MainThread;

            await Scene.Descendants<PaintCanvas>().First().ClearAsync();
        }

        public async Task NewAsync()
        {
            await EngineApp.MainThread;

            var clear = Scene.Descendants<PaintCanvas>().First().ClearAsync();

            Scene.Component<CanvasRecorder>().Resume("[\n]");
            Scene.Component<InputController>().CloseSelector();

            StartPlacement(CanvasPlacerMode.Draw);

            _currentId = null;

            await clear;
        }

        public async Task<GraffitiInfo> SaveAsync(bool asNew)
        {
            await EngineApp.MainThread;

            var canvas = Scene.Descendants<PaintCanvas>().First();

            var info = CreateInfo(canvas, asNew || _currentId == null ? Guid.NewGuid() : _currentId.Value);

            var dir = Directory.CreateDirectory(Path.Combine(_path, info.Id.ToString())).FullName;

            Scene.Component<CanvasRecorder>().SaveTo(Path.Combine(dir, "recording.json"));

            var state = await canvas.ReadStateAsync();

            WriteState(dir, state);

            void SavePng(TextureData data, string fileName)
            {
                using var image = data.ToBitmap(false, SKAlphaType.Unpremul)!;
                using var png = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.Create(Path.Combine(dir, fileName));
                png.SaveTo(stream);
            }

            SavePng(state.Color!, "color.png");
            SavePng(ImageUtils.Resize(state.Color!, 256, (int)(256 * state.Color!.Height / state.Color.Width)), "thumb.png");

            File.WriteAllText(Path.Combine(dir, "meta.json"), JsonSerializer.Serialize(info, JSON_OPTIONS));

            _currentId = info.Id;

            return info;
        }

        public async Task LoadAsync(Guid id, bool originalPose)
        {
            var dir = Path.Combine(_path, id.ToString());

            var info = JsonSerializer.Deserialize<GraffitiInfo>(File.ReadAllText(Path.Combine(dir, "meta.json")), JSON_OPTIONS)!;
            var state = ReadState(dir);
            var recording = File.ReadAllText(Path.Combine(dir, "recording.json"));

            await EngineApp.MainThread;

            var scene = Scene;
            var canvas = scene.Descendants<PaintCanvas>().First();

            _currentId = id;

            var load = Apply(canvas, info, canvas.GetWorldPose(), state, recording, int.MaxValue);

            scene.Component<InputController>().CloseSelector();

            var grid = canvas.Component<SpatialAnchorGrid>();

            if (originalPose && grid.Anchors.Any(a => a.Id == info.AnchorId))
                grid.SetReferenceAnchor(info.AnchorId!.Value, info.AnchorPose);
            else
                StartPlacement(CanvasPlacerMode.Place);

            await load;
        }

        public void Delete(Guid id)
        {
            Directory.Delete(Path.Combine(_path, id.ToString()), true);

            if (_currentId == id)
                _currentId = null;
        }

        public async Task SaveSessionAsync()
        {
            await EngineApp.MainThread;

            var canvas = Scene.Descendants<PaintCanvas>().First();
            var (recordingPath, history, count) = Scene.Component<CanvasRecorder>().Snapshot();

            var session = new GraffitiSession
            {
                Info = CreateInfo(canvas, Guid.Empty),
                CurrentId = _currentId,
                RecordingPath = recordingPath,
                RecordingCount = count
            };

            var state = await canvas.ReadStateAsync();

            var tempPath = _sessionPath + ".tmp";

            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

            Directory.CreateDirectory(tempPath);

            WriteState(tempPath, state);

            if (history != null)
                File.WriteAllText(Path.Combine(tempPath, "recording.json"), history + "\n]");

            File.WriteAllText(Path.Combine(tempPath, "meta.json"), JsonSerializer.Serialize(session, JSON_OPTIONS));

            if (Directory.Exists(_sessionPath))
                Directory.Delete(_sessionPath, true);

            Directory.Move(tempPath, _sessionPath);
        }

        public async Task RestoreSessionAsync()
        {
            var metaPath = Path.Combine(_sessionPath, "meta.json");

            if (!File.Exists(metaPath))
                return;

            var session = JsonSerializer.Deserialize<GraffitiSession>(File.ReadAllText(metaPath), JSON_OPTIONS)!;
            var state = ReadState(_sessionPath);
            var recordingPath = session.RecordingPath ?? Path.Combine(_sessionPath, "recording.json");
            var recording = File.Exists(recordingPath) ? File.ReadAllText(recordingPath) : "[\n]";

            await EngineApp.MainThread;

            var canvas = Scene.Descendants<PaintCanvas>().First();
            var info = session.Info;

            _currentId = session.CurrentId;

            var load = Apply(canvas, info, info.AnchorId == null ? info.AnchorPose : canvas.GetWorldPose(), state, recording, session.RecordingCount);

            var grid = canvas.Component<SpatialAnchorGrid>();

            if (grid.Anchors.Any(a => a.Id == info.AnchorId))
                grid.SetReferenceAnchor(info.AnchorId!.Value, info.AnchorPose);
            else if (info.AnchorId != null)
                StartPlacement(CanvasPlacerMode.Place);

            await load;
        }

        static void StartPlacement(CanvasPlacerMode mode)
        {
            var placer = Scene.Component<CanvasPlacer>();
            placer.Mode = mode;
            placer.IsEnabled = true;
        }

        static GraffitiInfo CreateInfo(PaintCanvas canvas, Guid id)
        {
            var canvasPose = canvas.GetWorldPose();
            var anchor = canvas.Component<SpatialAnchorGrid>().GetClosestAnchor(canvasPose.Position, out _);

            return new GraffitiInfo
            {
                Id = id,
                Date = DateTime.Now,
                Size = canvas.Size,
                TexelSize = canvas.TexelSize,
                AnchorId = anchor?.Id,
                AnchorPose = anchor == null ? canvasPose : anchor.CurrentWorldPose.Inverse().Multiply(canvasPose)
            };
        }

        static Task Apply(PaintCanvas canvas, GraffitiInfo info, Pose3 pose, CanvasState state, string recording, int recordingCount)
        {
            canvas.SetCanvasSize(info.Size, pose, info.TexelSize);

            Scene.Component<CanvasRecorder>().Resume(recording, recordingCount);

            return canvas.LoadStateAsync(state);
        }

        static void WriteState(string dir, CanvasState state)
        {
            PvrTranscoder.Instance.SaveTexture(File.Create(Path.Combine(dir, "wet.pvr")), [state.Wet]);
            PvrTranscoder.Instance.SaveTexture(File.Create(Path.Combine(dir, "dry.pvr")), [state.Dry]);
        }

        static CanvasState ReadState(string dir)
        {
            TextureData LoadTexture(string fileName)
            {
                using var stream = File.OpenRead(Path.Combine(dir, fileName));
                return PvrTranscoder.Instance.LoadTexture(stream)[0];
            }

            return new CanvasState
            {
                Wet = LoadTexture("wet.pvr"),
                Dry = LoadTexture("dry.pvr")
            };
        }

        static MainScene Scene => (MainScene)EngineApp.Current!.ActiveScene!;
    }
}
