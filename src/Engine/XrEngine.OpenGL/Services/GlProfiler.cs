#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Text;
using System.Globalization;

namespace XrEngine.OpenGL
{
    public class GlProfilerEntry : IDisposable
    {
        GlQuery<ulong>? _query;
        GlQuery<ulong>? _endQuery;
        readonly GlProfiler _profiler;
        readonly bool _useCounter;
        bool _isCompleted;
        bool _isEnded;
        ulong _result;
        private bool _isValid;

        internal GlProfilerEntry(GlProfiler profiler, string name, long frame, bool useCounter)
        {
            _profiler = profiler;
            _useCounter = useCounter;

            if (profiler.IsEnabled)
            {
                _query = new GlQuery<ulong>(profiler._gl);

                if (!_useCounter)
                    _query.Begin(QueryTarget.TimeElapsed);
                else
                {
                    _query.Counter();
                    _endQuery = new GlQuery<ulong>(profiler._gl);
                }
            }

            Name = name;
            Frame = frame;
        }

        internal bool Poll()
        {
            if (!_profiler.IsEnabled)
                return false;

            if (_isCompleted)
                return true;

            if (!_isEnded)
                return false;

            if (_useCounter)
            {
                if (_query!.IsCompleted() && _endQuery!.IsCompleted())
                {
                    var endRes = _endQuery.GetResult();
                    var startRes = _query.GetResult();

                    if (endRes < startRes)
                    {
                        _result = 0;
                        _isValid = false;
                    }

                    else
                    {
                        _result = endRes - startRes;
                        _isValid = true;
                    }

                    Destroy();

                    _isCompleted = true;

                    return true;
                }

                return false;
            }

            if (_query!.IsCompleted())
            {
                _result = _query.GetResult();
                _isValid = true;

                Destroy();

                _isCompleted = true;
            }

            return _isCompleted;
        }

        internal void Destroy()
        {
            _isCompleted = true;

            _query?.Dispose();
            _query = null;

            _endQuery?.Dispose();
            _endQuery = null;
        }

        public void Dispose()
        {
            if (!_profiler.IsEnabled)
                return;

            if (!_useCounter)
                _query!.End();
            else
                _endQuery!.Counter();

            _isEnded = true;

            GC.SuppressFinalize(this);
        }

        public ulong Result => _result;

        public string Name { get; }

        public long Frame { get; }

        public bool IsValid => _isValid;
    }

    public struct GlProfilerStat : IProfilerStat
    {
        public string? Name;

        public long Frame;

        public ulong Value;

        readonly ProfilerStatType IProfilerStat.Type => ProfilerStatType.Time;

        readonly string? IProfilerStat.Name => Name;

        readonly long IProfilerStat.Frame => Frame;

        readonly ulong IProfilerStat.Value => Value;
    }

    public class GlProfiler : IGpuProfiler, IDisposable
    {
        protected internal GL _gl;

        protected List<GlProfilerEntry> _entries = [];

        protected Dictionary<string, List<GlProfilerStat>> _stats = [];

        protected Dictionary<string, double> _averages = [];

        public GlProfiler(GL gl)
        {
            _gl = gl;
            MaxStats = 36 * 1;
            IsEnabled = true;

            Context.Implement<IGpuProfiler>(this);
        }

        public GlProfilerEntry Profile(string name, long frame, bool useCounter = false)
        {
            var entry = new GlProfilerEntry(this, name, frame, useCounter);
            if (IsEnabled)
                _entries.Add(entry);
            return entry;
        }

        public void Collect()
        {
            if (!IsEnabled)
            {
                if (_entries.Count > 0)
                    Clear();
                return;
            }

            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];

                if (!entry.Poll())
                    continue;

                if (!_stats.TryGetValue(entry.Name, out var stats))
                {
                    stats = [];
                    _stats[entry.Name] = stats;
                }

                if (entry.IsValid)
                {
                    stats.Add(new GlProfilerStat
                    {
                        Name = entry.Name,
                        Frame = entry.Frame,
                        Value = entry.Result
                    });

                    UpdateAverage(entry.Name);
                }

                while (stats.Count > MaxStats)
                    stats.RemoveAt(0);

                _entries.RemoveAt(i);
            }
        }

        protected void UpdateAverage(string name)
        {
            var stats = _stats[name];

            if (stats.Count == 0)
            {
                _averages[name] = 0;
                return;
            }

            double avg = 0;

            foreach (var stat in stats)
                avg += stat.Value / 1000.0;

            avg /= stats.Count;

            _averages[name] = avg;
        }

        public string GetStatsLog(bool excludeZero = true)
        {
            if (_averages.Count == 0)
                return string.Empty;

            var entries = _averages
                .Where(x => !excludeZero || x.Value != 0)
                .ToArray();

            if (entries.Length == 0)
                return string.Empty;

            var maxNameLen = entries.Max(x => x.Key.Length);

            var sb = new StringBuilder();
            sb.AppendLine("──────────── GPU PROFILE ────────────");

            foreach (var item in entries)
            {
                sb.Append(item.Key.PadRight(maxNameLen));
                sb.Append(" │ ");
                sb.Append(item.Value
                    .ToString("N1", CultureInfo.InvariantCulture)
                    .PadLeft(10));
                sb.AppendLine(" us");
            }

            sb.Append("─────────────────────────────────────");

            return sb.ToString();
        }

        public void Clear()
        {
            foreach (var entry in _entries)
                entry.Destroy();

            _entries.Clear();
        }

        public void Dispose()
        {
            Clear();
            GC.SuppressFinalize(this);
        }

        IReadOnlyList<IProfilerStat> IProfiler.GetStats() =>
            (IReadOnlyList<IProfilerStat>)_stats.Values.ToArray();

        public Dictionary<string, double> Averages => _averages;

        public Dictionary<string, List<GlProfilerStat>> Stats => _stats;

        public int MaxStats { get; set; }

        public bool IsEnabled { get; set; }
    }
}
