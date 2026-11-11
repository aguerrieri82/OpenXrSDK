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

    public class GraffitiStore
    {
        static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            IncludeFields = true,
            WriteIndented = true
        };

        readonly string _path;
        Guid? _currentId;

        public GraffitiStore()
        {
            _path = Path.Combine(XrPlatform.Current!.PersistentPath, "Graffiti", "Saves");
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

            var placer = Scene.Component<CanvasPlacer>();
            placer.Mode = CanvasPlacerMode.Draw;
            placer.IsEnabled = true;

            _currentId = null;

            await clear;
        }

        public async Task<GraffitiInfo> SaveAsync(bool asNew)
        {
            await EngineApp.MainThread;

            var scene = Scene;
            var canvas = scene.Descendants<PaintCanvas>().First();
            var canvasPose = canvas.GetWorldPose();
            var anchor = canvas.Component<SpatialAnchorGrid>().GetClosestAnchor(canvasPose.Position, out _);

            var info = new GraffitiInfo
            {
                Id = asNew || _currentId == null ? Guid.NewGuid() : _currentId.Value,
                Date = DateTime.Now,
                Size = canvas.Size,
                TexelSize = canvas.TexelSize,
                AnchorId = anchor?.Id,
                AnchorPose = anchor == null ? canvasPose : anchor.CurrentWorldPose.Inverse().Multiply(canvasPose)
            };

            var dir = Directory.CreateDirectory(Path.Combine(_path, info.Id.ToString())).FullName;

            scene.Component<CanvasRecorder>().SaveTo(Path.Combine(dir, "recording.json"));

            var state = await canvas.ReadStateAsync();

            PvrTranscoder.Instance.SaveTexture(File.Create(Path.Combine(dir, "wet.pvr")), [state.Wet]);
            PvrTranscoder.Instance.SaveTexture(File.Create(Path.Combine(dir, "dry.pvr")), [state.Dry]);

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

            TextureData LoadTexture(string fileName)
            {
                using var stream = File.OpenRead(Path.Combine(dir, fileName));
                return PvrTranscoder.Instance.LoadTexture(stream)[0];
            }

            var state = new CanvasState
            {
                Wet = LoadTexture("wet.pvr"),
                Dry = LoadTexture("dry.pvr")
            };

            var recording = File.ReadAllText(Path.Combine(dir, "recording.json"));

            await EngineApp.MainThread;

            var scene = Scene;
            var canvas = scene.Descendants<PaintCanvas>().First();

            _currentId = id;

            canvas.SetCanvasSize(info.Size, canvas.GetWorldPose(), info.TexelSize);
            var load = canvas.LoadStateAsync(state);

            scene.Component<CanvasRecorder>().Resume(recording);
            scene.Component<InputController>().CloseSelector();

            var grid = canvas.Component<SpatialAnchorGrid>();

            if (originalPose && grid.Anchors.Any(a => a.Id == info.AnchorId))
                grid.SetReferenceAnchor(info.AnchorId!.Value, info.AnchorPose);
            else
            {
                var placer = scene.Component<CanvasPlacer>();
                placer.Mode = CanvasPlacerMode.Place;
                placer.IsEnabled = true;
            }

            await load;
        }

        public void Delete(Guid id)
        {
            Directory.Delete(Path.Combine(_path, id.ToString()), true);

            if (_currentId == id)
                _currentId = null;
        }

        static MainScene Scene => (MainScene)EngineApp.Current!.ActiveScene!;
    }
}
