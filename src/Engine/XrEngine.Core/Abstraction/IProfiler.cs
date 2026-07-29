using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface IGpuProfiler : IProfiler
    {
    }

    [Flags]
    public enum ProfilerStatType
    {
        Unknown = 0,
        Time = 0x1,
        Count = 0x2,    
    }

    public interface IProfilerStat
    {
        string? Name { get; }

        long Frame { get; }

        ulong Value { get; }

        ProfilerStatType Type { get; }  
    }

    public interface IProfiler
    {

        IReadOnlyList<IProfilerStat> GetStats();

        Dictionary<string, double> Averages { get; }

        bool IsEnabled { get; set; }

        int MaxStats { get; set; }
    }
}
