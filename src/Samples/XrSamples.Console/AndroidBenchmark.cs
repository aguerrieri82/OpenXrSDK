using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace XrSamples;

public sealed partial class AndroidBenchmark
{
    private const string PackageName = "net.eusoft.oculus";
    private const string RemoteSettingsDirectory = "/sdcard/XrSamples";
    private const string RemoteSettingsPath = "/sdcard/XrSamples/GameSettings.json";
    private const string ResultsRoot = @"D:\Projects\XrEditor";
    private const int CompositorSkipRenderingMilliseconds = 5 * 60 * 1000;
    private static readonly JsonSerializerOptions ReportJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly object _consoleLock = new();
    private readonly List<(string Name, string Value)> _previousSystemProperties = [];
    private int _requestedGpuLevel;
    private bool _disableDynamicFoveation;
    private bool _disableProximitySensor;
    private bool _proximitySensorOverridden;
    private string _activityName = "Resolving...";
    private string _status = "Starting...";
    private string _settingsPath = string.Empty;
    private string _resultPath = string.Empty;
    private BenchmarkRun _run = new();
    private LoopRecord? _currentLoop;
    private EngineSample? _pendingEngineSample;
    private QuestSample? _pendingQuestSample;
    private int _loopStarts;
    private int _completedLoops;
    private int _measuredLoops;
    private double? _profileFrameTimeMs;
    private double? _profileFrameNumber;
    private double? _questGpuTimeMs;
    private int? _actualFps;
    private int? _targetFps;
    private int? _applicationTargetFps;
    private int? _aswFps;
    private string? _aswType;
    private bool? _aswEnabled;
    private int? _minimumFps;
    private int? _loopMinimumFps;
    private int? _gpuClockMHz;
    private int? _observedGpuLevel;
    private double? _gpuLoad;
    private double? _temperatureC;
    private int _loopBelowTargetReports;
    private int _loopStaleFrames;
    private int _loopTears;
    private int _belowTargetReports;
    private int _staleFrames;
    private int _tears;
    private readonly List<double> _loopProfileSamples = [];
    private readonly List<double> _loopQuestSamples = [];
    private readonly List<double> _loopAswSamples = [];
    private readonly List<double> _allProfileSamples = [];
    private readonly List<double> _allQuestSamples = [];
    private readonly List<double> _allAswSamples = [];
    private SampleStats? _lastProfileStats;
    private SampleStats? _lastQuestStats;
    private SampleStats? _overallProfileStats;
    private SampleStats? _overallQuestStats;
    private SampleStats? _overallAswStats;

    public static async Task RunAsync(string? settingsPath = null, int gpuLevel = 2, bool disableDynamicFoveation = true, bool disableProximitySensor = true, CancellationToken cancellationToken = default)
    {
        if (gpuLevel < 0)
            throw new ArgumentOutOfRangeException(nameof(gpuLevel));

        var benchmark = new AndroidBenchmark { _requestedGpuLevel = gpuLevel, _disableDynamicFoveation = disableDynamicFoveation, _disableProximitySensor = disableProximitySensor };
        await benchmark.RunCoreAsync(settingsPath, cancellationToken);
    }

    private async Task RunCoreAsync(
        string? settingsPath,
        CancellationToken cancellationToken)
    {
        _settingsPath = Path.GetFullPath(
            settingsPath ?? Path.Combine(AppContext.BaseDirectory, "GameSettings.DnD.json"));

        if (!File.Exists(_settingsPath))
            throw new FileNotFoundException("Android benchmark settings file not found.", _settingsPath);

        InitializeReport(ResultsRoot);

        using var stopSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        ConsoleCancelEventHandler cancelHandler = (_, args) =>
        {
            args.Cancel = true;
            stopSource.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            Render();

            await RunAdbAsync(stopSource.Token, "get-state");
            await ReadEnvironmentAsync(stopSource.Token);
            await ApplyBenchmarkDeviceSettingsAsync(stopSource.Token);

            SetStatus("Uploading settings...");
            await RunAdbAsync(
                stopSource.Token,
                "shell", "mkdir", "-p", RemoteSettingsDirectory);
            await RunAdbAsync(
                stopSource.Token,
                "push", _settingsPath, RemoteSettingsPath);

            SetStatus("Stopping existing application...");
            await RunAdbAsync(
                stopSource.Token,
                "shell", "am", "force-stop", PackageName);

            SetStatus("Resolving GameActivity...");
            _activityName = await ResolveActivityAsync(stopSource.Token);
            _run.Environment.Activity = _activityName;
            SaveReport();

            await RunAdbAsync(stopSource.Token, "logcat", "-c");

            using var logcat = StartAdb(
                "logcat", "-v", "epoch",
                "OpenGLRender:D", "XrCameraPlayer:W", "VrApi:I", "*:S");

            var logcatErrors = logcat.StandardError.ReadToEndAsync();

            try
            {
                SetStatus("Launching GameActivity...");
                await RunAdbAsync(
                    stopSource.Token,
                    "shell", "am", "start",
                    "-n", _activityName,
                    "-a", "android.intent.action.MAIN",
                    "-c", "com.oculus.intent.category.VR");

                await RunAdbAsync(stopSource.Token, "shell", "am", "broadcast", "-a", "com.oculus.vrruntimeservice.COMPOSITOR_SKIP_RENDERING", "--ei", "milliseconds", CompositorSkipRenderingMilliseconds.ToString(CultureInfo.InvariantCulture));

                SetStatus("Collecting GPU statistics. Press Ctrl+C to stop.");
                await ReadLogcatAsync(logcat, stopSource.Token);
            }
            finally
            {
                if (!logcat.HasExited)
                    logcat.Kill(true);

                await logcatErrors;
            }
        }
        catch (OperationCanceledException) when (stopSource.IsCancellationRequested)
        {
            SetStatus("Stopped.");
        }
        finally
        {
            FinalizePendingQuestSample();

            if (_currentLoop is { Completed: false })
                _currentLoop.Summary = CreateLoopSummary();

            _run.FinishedAtUtc = DateTimeOffset.UtcNow;
            await RestoreBenchmarkDeviceSettingsAsync();
            SaveReport();
            Console.CancelKeyPress -= cancelHandler;

            if (!Console.IsOutputRedirected)
                Console.CursorVisible = true;
        }
    }

    private async Task ApplyBenchmarkDeviceSettingsAsync(CancellationToken cancellationToken)
    {
        SetStatus("Applying benchmark device settings...");
        await CaptureAndSetSystemPropertyAsync("debug.oculus.gpuLevel", _requestedGpuLevel.ToString(CultureInfo.InvariantCulture), cancellationToken);

        if (_disableDynamicFoveation)
        {
            await CaptureAndSetSystemPropertyAsync("debug.oculus.foveation.level", "0", cancellationToken);
            await CaptureAndSetSystemPropertyAsync("debug.oculus.foveation.dynamic", "0", cancellationToken);
        }

        if (_disableProximitySensor)
        {
            _proximitySensorOverridden = true;
            await RunAdbAsync(cancellationToken, "shell", "am", "broadcast", "-a", "com.oculus.vrpowermanager.prox_close");
        }
    }

    private async Task CaptureAndSetSystemPropertyAsync(string name, string value, CancellationToken cancellationToken)
    {
        var previousValue = await RunAdbAsync(cancellationToken, "shell", "getprop", name);
        _previousSystemProperties.Add((name, previousValue));
        await RunAdbAsync(cancellationToken, "shell", "setprop", name, value);
    }

    private async Task RestoreBenchmarkDeviceSettingsAsync()
    {
        if (_proximitySensorOverridden)
        {
            try
            {
                await RunAdbAsync(CancellationToken.None, "shell", "am", "broadcast", "-a", "com.oculus.vrpowermanager.automation_disable");
            }
            catch
            {
                // Preserve the benchmark result if the device disconnected before cleanup.
            }
        }

        for (var index = _previousSystemProperties.Count - 1; index >= 0; index--)
        {
            var property = _previousSystemProperties[index];

            try
            {
                await RunAdbAsync(CancellationToken.None, "shell", "setprop", property.Name, property.Value);
            }
            catch
            {
                // Preserve the benchmark result even if the device disconnected before cleanup.
            }
        }
    }

    private async Task<string> ResolveActivityAsync(CancellationToken cancellationToken)
    {
        var output = await RunAdbAsync(
            cancellationToken,
            "shell", "cmd", "package", "resolve-activity", "--brief",
            "-a", "android.intent.action.MAIN",
            "-c", "com.oculus.intent.category.VR",
            "-p", PackageName);

        var component = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .LastOrDefault(line =>
                line.StartsWith(PackageName + "/", StringComparison.Ordinal) &&
                line.EndsWith("GameActivity", StringComparison.Ordinal));

        return component ?? throw new InvalidOperationException(
            $"Unable to resolve GameActivity for package '{PackageName}'.\n{output}");
    }

    private void InitializeReport(string resultsRoot)
    {
        using var settingsDocument = JsonDocument.Parse(File.ReadAllText(_settingsPath));
        var settings = settingsDocument.RootElement.Clone();
        var normalizedParameters = JsonSerializer.Serialize(new { Settings = settings, GpuLevel = _requestedGpuLevel, DisableDynamicFoveation = _disableDynamicFoveation, DisableProximitySensor = _disableProximitySensor, CompositorSkipRenderingMilliseconds });
        var combinationId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedParameters)))[..8].ToLowerInvariant();
        var sampleName = settings.TryGetProperty("SampleName", out var sampleNameProperty) ? sampleNameProperty.GetString() : "Unknown";
        sampleName = SanitizeFileName(sampleName ?? "Unknown");

        var startedAt = DateTimeOffset.UtcNow;
        var runId = $"{startedAt:yyyyMMdd-HHmmssfff}-{sampleName.ToLowerInvariant()}-{combinationId}";
        var resultDirectory = Path.Combine(resultsRoot, "AndroidBenchmarks", $"{sampleName}-{combinationId}");
        Directory.CreateDirectory(resultDirectory);
        _resultPath = Path.Combine(resultDirectory, $"run-{startedAt:yyyyMMdd-HHmmssfff}.json");

        _run = new BenchmarkRun
        {
            RunId = runId,
            CombinationId = combinationId,
            StartedAtUtc = startedAt,
            Parameters = new RunParameters
            {
                SettingsFile = _settingsPath,
                Settings = settings,
                GpuLevel = _requestedGpuLevel,
                DisableDynamicFoveation = _disableDynamicFoveation,
                DisableProximitySensor = _disableProximitySensor,
                CompositorSkipRenderingMilliseconds = CompositorSkipRenderingMilliseconds
            },
            Environment = new RunEnvironment { Package = PackageName }
        };

        SaveReport();
    }

    private async Task ReadEnvironmentAsync(CancellationToken cancellationToken)
    {
        _run.Environment.DeviceSerial = await RunAdbAsync(cancellationToken, "get-serialno");
        _run.Environment.DeviceModel = await RunAdbAsync(cancellationToken, "shell", "getprop", "ro.product.model");

        var packageInfo = await RunAdbAsync(cancellationToken, "shell", "dumpsys", "package", PackageName);
        var versionMatch = PackageVersionRegex().Match(packageInfo);
        _run.Environment.AppVersion = versionMatch.Success ? versionMatch.Groups["value"].Value : null;
    }

    private void SaveReport()
    {
        if (string.IsNullOrEmpty(_resultPath))
            return;

        UpdateOverallReport();
        var json = JsonSerializer.Serialize(_run, ReportJsonOptions);
        var temporaryPath = _resultPath + ".tmp";
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
        File.Move(temporaryPath, _resultPath, true);
    }

    private void UpdateOverallReport()
    {
        var frameBudgetMs = GetFrameBudgetMs();
        _run.Environment.TargetFps = _targetFps;
        _run.Environment.ApplicationTargetFps = _applicationTargetFps;
        _run.Environment.AswFps = _aswFps;
        _run.Environment.AswType = _aswType;
        _run.Environment.AswEnabled = _aswEnabled;
        _run.Environment.GpuLevel = _observedGpuLevel;
        _run.Overall = new OverallSummary
        {
            WarmupLoopsDiscarded = Math.Min(_completedLoops, 1),
            MeasuredLoops = _measuredLoops,
            EngineGpuMs = _overallProfileStats,
            QuestAppGpuMs = _overallQuestStats,
            AswFps = _overallAswStats,
            FrameBudgetMs = frameBudgetMs,
            EngineP95HeadroomMs = frameBudgetMs.HasValue && _overallProfileStats.HasValue
                ? frameBudgetMs.Value - _overallProfileStats.Value.P95
                : null,
            MinimumFps = _minimumFps,
            BelowTargetReports = _belowTargetReports,
            StaleFrames = _staleFrames,
            Tears = _tears
        };
    }

    private double? GetFrameBudgetMs() => _applicationTargetFps > 0 ? 1000.0 / _applicationTargetFps.Value : null;

    private static string SanitizeFileName(string value)
    {
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidCharacter, '-');

        return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    }

    private async Task ReadLogcatAsync(
        Process logcat,
        CancellationToken cancellationToken)
    {
        while (await logcat.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
            ProcessLogLine(line);

        await logcat.WaitForExitAsync(cancellationToken);

        if (logcat.ExitCode != 0)
            throw new InvalidOperationException($"adb logcat exited with code {logcat.ExitCode}.");
    }

    private void ProcessLogLine(string line)
    {
        if (line.Contains("XrCameraPlayer", StringComparison.Ordinal) &&
            line.Contains("Loop START", StringComparison.Ordinal))
        {
            var timestampMatch = EpochTimestampRegex().Match(line);

            if (timestampMatch.Success &&
                TryParseNumber(timestampMatch.Groups["value"].Value, out var timestamp))
            {
                if (_currentLoop != null)
                    CompleteLoop(timestamp);
                else
                    ResetLoopSamples();

                _loopStarts++;
                _currentLoop = new LoopRecord
                {
                    Number = _loopStarts,
                    Warmup = _loopStarts == 1,
                    StartedAtEpoch = timestamp
                };
                _run.Loops.Add(_currentLoop);
                SaveReport();

                Render();
            }

            return;
        }

        var profileFrameMatch = ProfileFrameRegex().Match(line);

        if (profileFrameMatch.Success &&
            TryParseNumber(profileFrameMatch.Groups["value"].Value, out var frameTimeUs))
        {
            _profileFrameTimeMs = frameTimeUs / 1000.0;

            if (_loopStarts > 0)
            {
                _loopProfileSamples.Add(_profileFrameTimeMs.Value);
                _pendingEngineSample = new EngineSample
                {
                    TimestampEpoch = ParseTimestamp(line),
                    FrameGpuMs = _profileFrameTimeMs.Value
                };
                _currentLoop?.EngineSamples.Add(_pendingEngineSample);
            }

            Render();
            return;
        }

        var profileFrameNumberMatch = ProfileFrameNumberRegex().Match(line);

        if (profileFrameNumberMatch.Success &&
            TryParseNumber(profileFrameNumberMatch.Groups["value"].Value, out var frameNumber))
        {
            _profileFrameNumber = frameNumber;

            if (_pendingEngineSample != null)
            {
                _pendingEngineSample.FrameNumber = (long)frameNumber;
                _pendingEngineSample = null;
            }

            Render();
            return;
        }

        if (!line.Contains("VrApi", StringComparison.Ordinal))
            return;

        var questGpuTimeMs = ParseDouble(QuestGpuRegex(), line);
        var fpsMatch = FpsRegex().Match(line);
        var aswMatch = AswRegex().Match(line);
        var staleFrames = ParseInt(StaleRegex(), line);
        var tears = ParseInt(TearRegex(), line);
        var gpuLevel = ParseInt(GpuLevelRegex(), line);
        var gpuClockMHz = ParseInt(GpuClockRegex(), line);
        var gpuLoad = ParseDouble(GpuLoadRegex(), line);
        var temperatureC = ParseDouble(TemperatureRegex(), line);
        var indicators = ParseVrApiIndicators(line);
        var labels = ParseVrApiLabels(line);

        if (questGpuTimeMs.HasValue)
        {
            _questGpuTimeMs = questGpuTimeMs.Value;

            if (_loopStarts > 0)
                _loopQuestSamples.Add(questGpuTimeMs.Value);
        }

        if (fpsMatch.Success)
        {
            FinalizePendingQuestSample();
            _actualFps = int.Parse(fpsMatch.Groups["actual"].Value, CultureInfo.InvariantCulture);
            _targetFps = int.Parse(fpsMatch.Groups["target"].Value, CultureInfo.InvariantCulture);
        }

        _observedGpuLevel = gpuLevel ?? _observedGpuLevel;
        _gpuClockMHz = gpuClockMHz ?? _gpuClockMHz;
        _gpuLoad = gpuLoad ?? _gpuLoad;
        _temperatureC = temperatureC ?? _temperatureC;

        if (_loopStarts > 0)
        {
            _loopStaleFrames += staleFrames ?? 0;
            _loopTears += tears ?? 0;

            if (questGpuTimeMs.HasValue)
            {
                _pendingQuestSample = new QuestSample
                {
                    TimestampEpoch = ParseTimestamp(line),
                    RawVrApiLine = line,
                    Indicators = indicators,
                    Labels = labels
                };
                _currentLoop?.QuestSamples.Add(_pendingQuestSample);
            }
        }

        if (aswMatch.Success)
        {
            _aswFps = int.Parse(aswMatch.Groups["value"].Value, CultureInfo.InvariantCulture);
            _aswType = aswMatch.Groups["type"].Value;
            _aswEnabled = _aswFps > 0 && _aswType.Equals("App", StringComparison.OrdinalIgnoreCase);
            _applicationTargetFps = _aswEnabled == true && _targetFps.HasValue ? _targetFps.Value / 2 : _targetFps;

            if (_loopStarts > 0)
                _loopAswSamples.Add(_aswFps.Value);

            if (_pendingQuestSample != null)
            {
                _pendingQuestSample.AswTimestampEpoch = ParseTimestamp(line);
                _pendingQuestSample.RawAswLine = line;

                foreach (var indicator in indicators)
                    _pendingQuestSample.Indicators[indicator.Key] = indicator.Value;

                foreach (var label in labels)
                    _pendingQuestSample.Labels[label.Key] = label.Value;

                FinalizePendingQuestSample();
            }
        }

        Render();
    }

    private static Dictionary<string, double> ParseVrApiIndicators(string line)
    {
        var result = new Dictionary<string, double>();

        void Add(string name, string pattern, string group = "value", double scale = 1)
        {
            var match = Regex.Match(line, pattern, RegexOptions.CultureInvariant);

            if (match.Success && TryParseNumber(match.Groups[group].Value, out var value))
                result[name] = value * scale;
        }

        Add("fps", @"\bFPS=(?<value>\d+)/");
        Add("displayRefreshHz", @"\bFPS=\d+/(?<value>\d+)");
        Add("predictionMs", @"\bPrd=(?<value>-?\d+(?:\.\d+)?)ms");
        Add("tearCount", @"\bTear=(?<value>\d+)");
        Add("earlyCount", @"\bEarly=(?<value>\d+)");
        Add("staleCount", @"\bStale=(?<value>\d+)");
        Add("stale2Count", @"\bStale2/5/10/max=(?<value>\d+)/");
        Add("stale5Count", @"\bStale2/5/10/max=\d+/(?<value>\d+)/");
        Add("stale10Count", @"\bStale2/5/10/max=\d+/\d+/(?<value>\d+)/");
        Add("staleMax", @"\bStale2/5/10/max=\d+/\d+/\d+/(?<value>\d+)");
        Add("vsync", @"\bVSnc=(?<value>-?\d+)");
        Add("latencyMode", @"\bLat=(?<value>-?\d+)");
        Add("foveationLevel", @"\bFov=(?<value>\d+)");
        Add("cpuMeasuredCore", @"\bCPU(?<value>\d+)/GPU=");
        Add("cpuLevel", @"\bCPU\d+/GPU=(?<value>\d+)/");
        Add("gpuLevel", @"\bCPU\d+/GPU=\d+/(?<value>\d+)");
        Add("cpuClockMHz", @"\bCPU\d+/GPU=\d+/\d+,(?<value>\d+)/\d+MHz");
        Add("gpuClockMHz", @"\bCPU\d+/GPU=\d+/\d+,\d+/(?<value>\d+)MHz");
        Add("timeWarpAffinity", @"\bTA=(?<value>-?\d+)/");
        Add("mainThreadAffinity", @"\bTA=-?\d+/(?<value>-?\d+)/");
        Add("renderThreadAffinity", @"\bTA=-?\d+/-?\d+/(?<value>-?\d+)");
        Add("memoryClockMHz", @"\bMem=(?<value>\d+)MHz");
        Add("freeMemoryMb", @"\bFree=(?<value>\d+)MB");
        Add("powerLevel", @"\bPLS=(?<value>-?\d+)");
        Add("batteryTemperatureC", @"\bTemp=(?<value>-?\d+(?:\.\d+)?)C/");
        Add("sensorTemperatureC", @"\bTemp=-?\d+(?:\.\d+)?C/(?<value>-?\d+(?:\.\d+)?)C");
        Add("timeWarpGpuMs", @"\bTW=(?<value>-?\d+(?:\.\d+)?)ms");
        Add("appGpuMs", @"\bApp=(?<value>-?\d+(?:\.\d+)?)ms");
        Add("guardianGpuMs", @"\bGD=(?<value>-?\d+(?:\.\d+)?)ms");
        Add("cpuAndGpuMs", @"\bCPU&GPU=(?<value>-?\d+(?:\.\d+)?)ms");
        Add("layerCount", @"\bLCnt=(?<value>\d+)");
        Add("directRenderFps", @"\bLCnt=\d+\(DR(?<value>\d+)");
        Add("mergedLayerCount", @"\bLCnt=\d+\(DR\d+,LM(?<value>\d+)\)");
        Add("gpuUtilizationPercent", @"\bGPU%=(?<value>\d+(?:\.\d+)?)", scale: 100);
        Add("cpuUtilizationPercent", @"\bCPU%=(?<value>\d+(?:\.\d+)?)", scale: 100);
        Add("worstCpuUtilizationPercent", @"\bCPU%=\d+(?:\.\d+)?\(W(?<value>\d+(?:\.\d+)?)\)", scale: 100);
        Add("compositorFrameLatencyMinMs", @"\bCFL=(?<value>-?\d+(?:\.\d+)?)/");
        Add("compositorFrameLatencyMaxMs", @"\bCFL=-?\d+(?:\.\d+)?/(?<value>-?\d+(?:\.\d+)?)");
        Add("integratedCompositorFrameLatencyP95Ms", @"\bICFLp95=(?<value>-?\d+(?:\.\d+)?)");
        Add("localDimming", @"\bLD=(?<value>\d+)");
        Add("scaleFactor", @"\bSF=(?<value>-?\d+(?:\.\d+)?)");
        Add("dpuScale", @"\bDpuScale=(?<value>-?\d+(?:\.\d+)?)");
        Add("batterySaver", @"\bLP=(?<value>\d+)");
        Add("dvfs", @"\bDVFS=(?<value>\d+)");
        Add("preemptCount", @"\bPreempt=(?<value>\d+)");
        Add("poseAgeP95Ms", @"\bPoseAgeP95=(?<value>-?\d+(?:\.\d+)?)");
        Add("aswFps", @"\bASW=(?<value>\d+)");
        Add("aswErrorMin", @"\bE=(?<value>-?\d+(?:\.\d+)?)/");
        Add("aswErrorMax", @"\bE=-?\d+(?:\.\d+)?/(?<value>-?\d+(?:\.\d+)?)");
        Add("aswDeltaMin", @"\bD=(?<value>-?\d+(?:\.\d+)?)/");
        Add("aswDeltaMax", @"\bD=-?\d+(?:\.\d+)?/(?<value>-?\d+(?:\.\d+)?)");
        return result;
    }

    private static Dictionary<string, string> ParseVrApiLabels(string line)
    {
        var result = new Dictionary<string, string>();

        void Add(string name, string pattern, string group = "value")
        {
            var match = Regex.Match(line, pattern, RegexOptions.CultureInvariant);

            if (match.Success)
                result[name] = match.Groups[group].Value;
        }

        Add("foveation", @"\bFov=(?<value>[^,\s]+)");
        Add("onlineCoreMask", @"\bOC=(?<value>[^,\s]+)");
        Add("timeWarpPriority", @"\bSP=(?<value>[^,/\s]+)/");
        Add("mainThreadPriority", @"\bSP=[^,/\s]+/(?<value>[^,/\s]+)/");
        Add("renderThreadPriority", @"\bSP=[^,/\s]+/[^,/\s]+/(?<value>[^,\s]+)");
        Add("aswType", @"\bType=(?<value>[^,\s]+)");
        return result;
    }

    private void CompleteLoop(double finishedAtEpoch)
    {
        FinalizePendingQuestSample();
        _completedLoops++;
        var summary = CreateLoopSummary();
        var profileStats = summary.EngineGpuMs;
        var questStats = summary.QuestAppGpuMs;

        if (_currentLoop != null)
        {
            _currentLoop.Completed = true;
            _currentLoop.FinishedAtEpoch = finishedAtEpoch;
            _currentLoop.Summary = summary;
        }

        if (_completedLoops > 1)
        {
            _measuredLoops++;
            _lastProfileStats = profileStats;
            _lastQuestStats = questStats;
            _allProfileSamples.AddRange(_loopProfileSamples);
            _allQuestSamples.AddRange(_loopQuestSamples);
            _allAswSamples.AddRange(_loopAswSamples);
            _overallProfileStats = CalculateStats(_allProfileSamples);
            _overallQuestStats = CalculateStats(_allQuestSamples);
            _overallAswStats = CalculateStats(_allAswSamples);
            _belowTargetReports += _loopBelowTargetReports;
            _staleFrames += _loopStaleFrames;
            _tears += _loopTears;

            if (_loopMinimumFps.HasValue)
                _minimumFps = !_minimumFps.HasValue || _loopMinimumFps < _minimumFps ? _loopMinimumFps : _minimumFps;
        }

        ResetLoopSamples();
        SaveReport();
    }

    private void FinalizePendingQuestSample()
    {
        if (_pendingQuestSample == null)
            return;

        var fps = _pendingQuestSample.Indicators.GetValueOrDefault("fps");
        var targetFps = _pendingQuestSample.Indicators.GetValueOrDefault("displayRefreshHz");
        var aswFps = _pendingQuestSample.Indicators.GetValueOrDefault("aswFps");
        var aswEnabled = aswFps > 0 && _pendingQuestSample.Labels.GetValueOrDefault("aswType")?.Equals("App", StringComparison.OrdinalIgnoreCase) == true;
        var expectedFps = targetFps > 0 ? (aswEnabled ? targetFps / 2 : targetFps) : 0;

        if (fps > 0)
        {
            if (expectedFps > 0 && fps < expectedFps)
                _loopBelowTargetReports++;

            _loopMinimumFps = !_loopMinimumFps.HasValue || fps < _loopMinimumFps
                ? (int)fps
                : _loopMinimumFps;
        }

        if (expectedFps > 0)
            _applicationTargetFps = (int)expectedFps;

        _pendingQuestSample = null;
    }

    private LoopSummary CreateLoopSummary() => new()
    {
        EngineGpuMs = CalculateStats(_loopProfileSamples),
        QuestAppGpuMs = CalculateStats(_loopQuestSamples),
        AswFps = CalculateStats(_loopAswSamples),
        MinimumFps = _loopMinimumFps,
        BelowTargetReports = _loopBelowTargetReports,
        StaleFrames = _loopStaleFrames,
        Tears = _loopTears
    };

    private void ResetLoopSamples()
    {
        _loopProfileSamples.Clear();
        _loopQuestSamples.Clear();
        _loopAswSamples.Clear();
        _loopBelowTargetReports = 0;
        _loopStaleFrames = 0;
        _loopTears = 0;
        _loopMinimumFps = null;
        _pendingEngineSample = null;
        _pendingQuestSample = null;
    }

    private void SetStatus(string status)
    {
        _status = status;
        Render();
    }

    private void Render()
    {
        lock (_consoleLock)
        {
            var frameBudgetMs = GetFrameBudgetMs();
            var engineHeadroomMs = frameBudgetMs.HasValue && _overallProfileStats.HasValue
                ? frameBudgetMs.Value - _overallProfileStats.Value.P95
                : (double?)null;

            var lines = new[]
            {
                "XR ANDROID GPU BENCHMARK",
                new string('═', 72),
                $"Status    {_status}",
                $"Package   {PackageName}",
                $"Activity  {_activityName}",
                $"Settings  {_settingsPath}",
                $"Results   {_resultPath}",
                string.Empty,
                "RUN",
                $"Current loop  {FormatInteger(_loopStarts)}    Completed  {FormatInteger(_completedLoops)}    Measured  {FormatInteger(_measuredLoops)}",
                "First completed loop is discarded as warm-up.",
                string.Empty,
                "LIVE",
                $"Engine Frame GPU  {FormatMilliseconds(_profileFrameTimeMs),12}    Quest App GPU  {FormatMilliseconds(_questGpuTimeMs),12}    Frame N°  {FormatNumber(_profileFrameNumber)}",
                string.Empty,
                "LAST MEASURED LOOP          Average       Median          P95    Samples",
                FormatStatsLine("Engine GPU", _lastProfileStats),
                FormatStatsLine("Quest App", _lastQuestStats),
                string.Empty,
                "ALL MEASURED LOOPS          Average       Median          P95    Samples",
                FormatStatsLine("Engine GPU", _overallProfileStats),
                FormatStatsLine("Quest App", _overallQuestStats),
                string.Empty,
                "RUNTIME HEALTH",
                $"FPS  {FormatFps()}    Minimum  {FormatNullableInteger(_minimumFps)}    Below target  {_belowTargetReports}    Stale  {_staleFrames}    Tears  {_tears}",
                $"Budget  {FormatMilliseconds(frameBudgetMs)}    Engine P95 headroom  {FormatSignedMilliseconds(engineHeadroomMs)}",
                $"GPU  {FormatMhz(_gpuClockMHz)}    Load  {FormatPercent(_gpuLoad)}    Temperature  {FormatTemperature(_temperatureC)}",
                new string('─', 72),
                "Press Ctrl+C to stop"
            };

            if (Console.IsOutputRedirected)
            {
                Console.WriteLine(string.Join(" | ", lines.Where(line => line.Length > 0)));
                return;
            }

            Console.CursorVisible = false;
            var width = Math.Max(1, Console.WindowWidth - 1);
            var originalColor = Console.ForegroundColor;

            try
            {
                for (var index = 0; index < lines.Length; index++)
                {
                    Console.SetCursorPosition(0, index);
                    var line = lines[index];
                    Console.ForegroundColor = GetLineColor(line, originalColor);

                    if (line.Length > width)
                        line = line[..width];

                    Console.Write(line.PadRight(width));
                }
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }

    private static ConsoleColor GetLineColor(string line, ConsoleColor defaultColor)
    {
        if (line == "XR ANDROID GPU BENCHMARK")
            return ConsoleColor.Cyan;

        if (line is "RUN" or "LIVE" or "RUNTIME HEALTH" || line.StartsWith("LAST ") || line.StartsWith("ALL "))
            return ConsoleColor.Yellow;

        if (line.Length > 0 && line.All(character => character is '═' or '─'))
            return ConsoleColor.DarkGray;

        return defaultColor;
    }

    private static string FormatMilliseconds(double? value) =>
        value.HasValue
            ? $"{value.Value.ToString("N3", CultureInfo.InvariantCulture)} ms"
            : "--";

    private static string FormatNumber(double? value) =>
        value.HasValue
            ? value.Value.ToString("N0", CultureInfo.InvariantCulture)
            : "--";

    private static string FormatInteger(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string FormatNullableInteger(int? value) => value.HasValue ? FormatInteger(value.Value) : "--";

    private static string FormatStatsLine(string label, SampleStats? stats) => stats.HasValue
        ? $"{label,-20}{FormatMilliseconds(stats.Value.Average),12}{FormatMilliseconds(stats.Value.Median),13}{FormatMilliseconds(stats.Value.P95),13}{stats.Value.Count,11:N0}"
        : $"{label,-20}{"--",12}{"--",13}{"--",13}{"--",11}";

    private string FormatFps() => _actualFps.HasValue && _targetFps.HasValue ? $"{_actualFps}/{_targetFps}" : "--/--";

    private static string FormatSignedMilliseconds(double? value) => value.HasValue
        ? $"{value.Value:+0.000;-0.000;0.000} ms"
        : "--";

    private static string FormatMhz(int? value) => value.HasValue ? $"{value.Value:N0} MHz" : "--";

    private static string FormatPercent(double? value) => value.HasValue ? $"{value.Value * 100.0:0.0}%" : "--";

    private static string FormatTemperature(double? value) => value.HasValue ? $"{value.Value:0.0} °C" : "--";

    private static SampleStats? CalculateStats(IReadOnlyCollection<double> samples)
    {
        if (samples.Count == 0)
            return null;

        var sorted = samples.Order().ToArray();
        var middle = sorted.Length / 2;
        var median = sorted.Length % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) / 2.0 : sorted[middle];
        var p95 = sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * 0.95) - 1)];
        return new SampleStats(sorted.Average(), median, p95, sorted[0], sorted[^1], sorted.Length);
    }

    private static int? ParseInt(Regex regex, string line)
    {
        var match = regex.Match(line);
        return match.Success && int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static double? ParseDouble(Regex regex, string line)
    {
        var match = regex.Match(line);
        return match.Success && TryParseNumber(match.Groups["value"].Value, out var value) ? value : null;
    }

    private static double? ParseTimestamp(string line) => ParseDouble(EpochTimestampRegex(), line);

    private static bool TryParseNumber(string text, out double value) =>
        double.TryParse(
            text.Replace(",", string.Empty, StringComparison.Ordinal),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);

    private static async Task<string> RunAdbAsync(
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        using var process = StartAdb(arguments);
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch
        {
            if (!process.HasExited)
                process.Kill(true);

            throw;
        }

        var outputText = await output;
        var errorText = await error;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"adb {string.Join(' ', arguments)} failed with exit code {process.ExitCode}.\n{errorText}");
        }

        return outputText.Trim();
    }

    private static Process StartAdb(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "adb",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        return Process.Start(startInfo) ??
            throw new InvalidOperationException("Unable to start adb.");
    }

    [GeneratedRegex(@"^\s*(?<value>\d+(?:\.\d+)?)\s")]
    private static partial Regex EpochTimestampRegex();

    [GeneratedRegex(@"\bFrame\b.*?(?<value>\d[\d,.]*)\s*us")]
    private static partial Regex ProfileFrameRegex();

    [GeneratedRegex(@"\bFrame N°\s*\D*(?<value>\d[\d,.]*)")]
    private static partial Regex ProfileFrameNumberRegex();

    [GeneratedRegex(@"\bApp=(?<value>\d+(?:\.\d+)?)ms")]
    private static partial Regex QuestGpuRegex();

    [GeneratedRegex(@"\bFPS=(?<actual>\d+)/(?<target>\d+)")]
    private static partial Regex FpsRegex();

    [GeneratedRegex(@"\bASW=(?<value>\d+),\s*Type=(?<type>[^,\s]+)")]
    private static partial Regex AswRegex();

    [GeneratedRegex(@"\bCPU\d+/GPU=\d+/\d+,\d+/(?<value>\d+)MHz")]
    private static partial Regex GpuClockRegex();

    [GeneratedRegex(@"\bCPU\d+/GPU=\d+/(?<value>\d+)")]
    private static partial Regex GpuLevelRegex();

    [GeneratedRegex(@"\bGPU%=(?<value>\d+(?:\.\d+)?)")]
    private static partial Regex GpuLoadRegex();

    [GeneratedRegex(@"\bTemp=(?<value>\d+(?:\.\d+)?)C")]
    private static partial Regex TemperatureRegex();

    [GeneratedRegex(@"\bStale=(?<value>\d+)")]
    private static partial Regex StaleRegex();

    [GeneratedRegex(@"\bTear=(?<value>\d+)")]
    private static partial Regex TearRegex();

    [GeneratedRegex(@"\bversionName=(?<value>\S+)")]
    private static partial Regex PackageVersionRegex();

    private sealed class BenchmarkRun
    {
        public int SchemaVersion { get; set; } = 2;
        public string RunId { get; set; } = string.Empty;
        public string CombinationId { get; set; } = string.Empty;
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset? FinishedAtUtc { get; set; }
        public RunParameters Parameters { get; set; } = new();
        public RunEnvironment Environment { get; set; } = new();
        public List<LoopRecord> Loops { get; set; } = [];
        public OverallSummary Overall { get; set; } = new();
    }

    private sealed class RunParameters
    {
        public string SettingsFile { get; set; } = string.Empty;
        public JsonElement Settings { get; set; }
        public int GpuLevel { get; set; }
        public bool DisableDynamicFoveation { get; set; }
        public bool DisableProximitySensor { get; set; }
        public int CompositorSkipRenderingMilliseconds { get; set; }
    }

    private sealed class RunEnvironment
    {
        public string Package { get; set; } = string.Empty;
        public string? Activity { get; set; }
        public string? DeviceSerial { get; set; }
        public string? DeviceModel { get; set; }
        public string? AppVersion { get; set; }
        public int? TargetFps { get; set; }
        public int? ApplicationTargetFps { get; set; }
        public int? AswFps { get; set; }
        public string? AswType { get; set; }
        public bool? AswEnabled { get; set; }
        public int? GpuLevel { get; set; }
    }

    private sealed class LoopRecord
    {
        public int Number { get; set; }
        public bool Warmup { get; set; }
        public bool Completed { get; set; }
        public double StartedAtEpoch { get; set; }
        public double? FinishedAtEpoch { get; set; }
        public List<EngineSample> EngineSamples { get; set; } = [];
        public List<QuestSample> QuestSamples { get; set; } = [];
        public LoopSummary? Summary { get; set; }
    }

    private sealed class EngineSample
    {
        public double? TimestampEpoch { get; set; }
        public long? FrameNumber { get; set; }
        public double FrameGpuMs { get; set; }
    }

    private sealed class QuestSample
    {
        public double? TimestampEpoch { get; set; }
        public double? AswTimestampEpoch { get; set; }
        public string? RawVrApiLine { get; set; }
        public string? RawAswLine { get; set; }
        public Dictionary<string, double> Indicators { get; set; } = [];
        public Dictionary<string, string> Labels { get; set; } = [];
    }

    private class LoopSummary
    {
        public SampleStats? EngineGpuMs { get; set; }
        public SampleStats? QuestAppGpuMs { get; set; }
        public SampleStats? AswFps { get; set; }
        public int? MinimumFps { get; set; }
        public int BelowTargetReports { get; set; }
        public int StaleFrames { get; set; }
        public int Tears { get; set; }
    }

    private sealed class OverallSummary : LoopSummary
    {
        public int WarmupLoopsDiscarded { get; set; }
        public int MeasuredLoops { get; set; }
        public double? FrameBudgetMs { get; set; }
        public double? EngineP95HeadroomMs { get; set; }
    }

    private readonly record struct SampleStats(
        double Average,
        double Median,
        double P95,
        double Minimum,
        double Maximum,
        int Count);
}
