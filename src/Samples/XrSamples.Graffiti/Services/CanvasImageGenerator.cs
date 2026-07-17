using SkiaSharp;
using XrEngine;
using XrEngine.OpenGL;
using XrEngine.OpenXr;

namespace XrSamples.Graffiti
{
    public class CanvasImageGenerator
    {
        readonly GlSimulationPass _simulation;

        public static CanvasImageGenerator Build(IXrEnginePlatform platform)
        {
            var app = new EngineApp();
            var scene = new MainScene(true);

            app.OpenScene(scene);

            var engApp = new XrEngineAppBuilder()
                .UseApp(app)
                .UsePlatform(platform)
                .UseOpenGL()
                .Build();

            return new CanvasImageGenerator((OpenGLRender)engApp.App.Renderer);
        }

        public CanvasImageGenerator()
            : this((OpenGLRender)EngineApp.Current.Renderer)
        {
        }

        public CanvasImageGenerator(OpenGLRender render)
        {
            _simulation = render.Pass<GlSimulationPass>()!;
            _simulation._isFirstSizeUpdate = false;
        }

        protected List<CanvasRecordCommand> ProcessUndo(CanvasRecording recording)
        {
            var newCommands = new List<CanvasRecordCommand>();
            var undoPoints = new List<int>();

            int? openSprayStart = null;

            for (var i = 0; i < recording.Commands.Count; i++)
            {
                var cmd = recording.Commands[i];

                if (cmd is UndoCommand)
                {
                    if (newCommands.Count == 0)
                        continue;

                    int removeFrom;

                    if (openSprayStart != null)
                    {
                        // Undo while still spraying:
                        // remove from the first SprayCommand of the open spray session.
                        removeFrom = openSprayStart.Value;
                    }
                    else if (newCommands[^1] is SprayCloseCommand)
                    {
                        // Undo immediately after spray close:
                        // remove the whole just-closed spray session, including SprayClose.
                        //
                        // The last undo point is the SprayClose itself, so skip it.
                        undoPoints.RemoveAt(undoPoints.Count - 1);

                        removeFrom = undoPoints.Count > 0
                            ? undoPoints[^1] + 1
                            : 0;
                    }
                    else if (undoPoints.Count > 0)
                    {
                        // Normal undo:
                        // remove commands after the last committed undo point.
                        removeFrom = undoPoints[^1] + 1;
                    }
                    else
                    {
                        removeFrom = 0;
                    }

                    newCommands.RemoveRange(removeFrom, newCommands.Count - removeFrom);

                    while (undoPoints.Count > 0 && undoPoints[^1] >= newCommands.Count)
                        undoPoints.RemoveAt(undoPoints.Count - 1);

                    openSprayStart = null;

                    continue;
                }

                if (cmd is SprayCommand && openSprayStart == null)
                    openSprayStart = newCommands.Count;

                newCommands.Add(cmd);

                if (cmd is SprayCloseCommand)
                {
                    openSprayStart = null;
                    undoPoints.Add(newCommands.Count - 1);
                }
                else if (cmd is ClearCommand)
                {
                    openSprayStart = null;
                    undoPoints.Add(newCommands.Count - 1);
                }
            }

            return newCommands;
        }


        public SKBitmap Generate(CanvasRecording recording, float texelSize, int fps = 72)
        {
            var scene = EngineApp.Current!.ActiveScene!;

            var ctx = new GlUpdateContext
            {
                Scene = scene,
                MainCamera = scene.ActiveCamera,
                PassCamera = scene.ActiveCamera,    
            };

            var canvas = ctx.Scene!.Descendants<PaintCanvas>().First();
            var can = ctx.Scene!.Descendants<Can>().First();
            var tracker = can.Component<SprayTracker>();

            var frameTime = 1f / fps;

            can.SoundEnabled = false;

            var isSpraying = false;

            Log.Info(this, "Simulation Start");

            _simulation.ReconstructMode = true;

            var newCommands = ProcessUndo(recording);

            foreach (var action in newCommands)
            {
                var interpolateOn = false;

                if (action is ParamsCommand paramsCmd)
                {
                    canvas.DryRoughness = paramsCmd.DryRoughness;
                    canvas.WetRoughness = paramsCmd.WetRoughness;
                    canvas.NormalScale = paramsCmd.NormalScale;
                    canvas.DryRate = paramsCmd.DryRate;
                    canvas.DripRate = paramsCmd.DripRate;
                    canvas.PaintOpacityScale = paramsCmd.PaintOpacityScale;

                    tracker.SpreadAngle = paramsCmd.SpreadAngle;
                    tracker.SprayCenter = paramsCmd.SprayCenter;
                    tracker.SprayDirection = paramsCmd.SprayDirection;
                    tracker.SprayRadius = paramsCmd.SprayRadius;
                    tracker.RadialFalloff = paramsCmd.RadialFalloff;
                    tracker.BaseDensity = paramsCmd.BaseDensity;
                }
                else if (action is CanvasCommand canvasCmd)
                {
                    canvas.SetCanvasSize(canvasCmd.Size, canvasCmd.Pose, texelSize);
                }
                else if (action is ChangeColorCommand colorCmd)
                    can.Color = colorCmd.Color;

                else if (action is SprayCommand sprayCmd)
                {
                    can.SetWorldPose(sprayCmd.CanPose);
                    can.SprayAperture = sprayCmd.Aperture;
                    if (!isSpraying)
                    {
                        interpolateOn = true;
                        isSpraying = true;
                    }
                }
                else if (action is SprayCloseCommand)
                {
                    isSpraying = false;
                    can.SprayAperture = 0;
                }
                else if (action is ClearCommand)
                {
                    _simulation.ClearCanvas();
                }
                else if (action is UndoCommand)
                {
                    throw new NotImplementedException();
                }

                var deltaStep = action.Time - ctx.Time;
                var stepCount = 1;
                var curAperture = can.SprayAperture;

                if (deltaStep > frameTime * 1.5 && interpolateOn)
                {
                    stepCount = (int)Math.Round(deltaStep / frameTime);
                    deltaStep = deltaStep / stepCount;
                    if (stepCount > 1)
                        can.SprayAperture = 0;
                }

                ctx.Time = action.Time;
                ctx.DeltaTime = deltaStep;

                for (var i = 0; i < stepCount; i++)
                {
                    if (i == stepCount - 1)
                        can.SprayAperture = curAperture;

                    _simulation.Render(ctx);

                    ctx.Frame++;
                    ctx.Time += deltaStep;

                    if (ctx.Frame % 100 == 0)
                    {
                        _simulation.Gl.Finish();
                        Log.Debug(this, "Simulation {0}...", ctx.Frame);
                    }
                }

                ctx.Time = action.Time;
            }

            can.SoundEnabled = true;

            Log.Debug(this, "Simulation End, finish...");

            _simulation.Gl.Finish();
            _simulation.ReconstructMode = false;

            Log.Info(this, "Finish complete");

            var texture = canvas.ColorTexture.ToGlTexture().Read(TextureFormat.Rgba32);
            var image = ImageUtils.ToBitmap(texture![0], false, SKAlphaType.Unpremul);
            return image!;
        }
    }
}
