using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.META;

namespace OpenXr.Framework.Oculus
{
    public struct XrPerformanceValue
    {
        public string Name;
        
        public float Value;

        public PerformanceMetricsCounterUnitMETA Unit;

        public override string ToString()
        {
            var unit = Unit switch
            {
                PerformanceMetricsCounterUnitMETA.PercentageMeta => "%",
                PerformanceMetricsCounterUnitMETA.MillisecondsMeta => "ms",
                PerformanceMetricsCounterUnitMETA.BytesMeta => "B",
                PerformanceMetricsCounterUnitMETA.HertzMeta => "Hz",
                _ => ""
            };

            return string.IsNullOrEmpty(unit) ? $"{Name}: {Value}" : $"{Name}: {Value} {unit}";
        }
    }

    public class XrPerformance
    {
        readonly XrApp _app;
        readonly MetaPerformanceMetrics _metrics;
        readonly Dictionary<string, ulong> _counters = [];

        public XrPerformance(XrApp app)
        {
            _app = app;

            if (!_app.Xr.TryGetInstanceExtension<MetaPerformanceMetrics>(null, _app.Instance, out var metrics))
                throw new NotSupportedException(MetaPerformanceMetrics.ExtensionName);

            _metrics = metrics;
            LoadCounters();
        }

        public bool IsEnabled()
        {
            var state = new PerformanceMetricsStateMETA
            {
                Type = StructureType.PerformanceMetricsStateMeta
            };

            _app.CheckResult(_metrics.GetPerformanceMetricsStateMeta(_app.Session, ref state), "GetPerformanceMetricsStateMeta");

            return state.Enabled != 0;
        }

        public XrPerformanceValue Read(string name)
        {
            if (!_counters.TryGetValue(name, out var path))
                throw new KeyNotFoundException(name);

            var counter = new PerformanceMetricsCounterMETA
            {
                Type = StructureType.PerformanceMetricsCounterMeta
            };

            _app.CheckResult(_metrics.QueryPerformanceMetricsCounterMeta(_app.Session, path, ref counter), "QueryPerformanceMetricsCounterMeta");

            var value = float.NaN;

            if ((counter.CounterFlags & PerformanceMetricsCounterFlagsMETA.FloatValueValidBitMeta) != 0)
                value = counter.FloatValue;
            else if ((counter.CounterFlags & PerformanceMetricsCounterFlagsMETA.UintValueValidBitMeta) != 0)
                value = counter.UintValue;

            return new XrPerformanceValue
            {
                Name = name,
                Value = value,
                Unit = counter.CounterUnit
            };
        }

        public XrPerformanceValue[] ReadAll()
        {
            var result = new XrPerformanceValue[_counters.Count];
            var i = 0;

            foreach (var name in _counters.Keys)
                result[i++] = Read(name);

            return result;
        }

        public void ReadAll(Dictionary<string, float> result)
        {
            foreach (var name in _counters.Keys)
                result[name] = Read(name).Value;
        }

        public void SetEnabled(bool enabled)
        {
            var state = new PerformanceMetricsStateMETA
            {
                Type = StructureType.PerformanceMetricsStateMeta,
                Enabled = enabled ? 1u : 0u
            };

            _app.CheckResult(_metrics.SetPerformanceMetricsStateMeta(_app.Session, in state), "SetPerformanceMetricsStateMeta");
        }

        private unsafe void LoadCounters()
        {
            uint count = 0;

            _app.CheckResult(_metrics.EnumeratePerformanceMetricsCounterPathsMeta(_app.Instance, 0, &count, null), "EnumeratePerformanceMetricsCounterPathsMeta");

            var paths = new ulong[count];

            fixed (ulong* pPaths = paths)
                _app.CheckResult(_metrics.EnumeratePerformanceMetricsCounterPathsMeta(_app.Instance, count, &count, pPaths), "EnumeratePerformanceMetricsCounterPathsMeta");

            foreach (var path in paths)
            {
                var name = _app.PathToString(path);

                if (name.StartsWith("/perfmetrics_meta/"))
                    name = name["/perfmetrics_meta/".Length..];

                _counters[name] = path;
            }
        }

        public IReadOnlyCollection<string> Counters => _counters.Keys;

    }
}