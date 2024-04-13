using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public enum LogLevel
    {
        Info,
        Error,
        Success,
        Warning,
        Debug
    }

    public interface IProgressLogger
    {
        void BeginTask(string? message = null);

        void EndTask();

        void LogMessage(string text, LogLevel level = LogLevel.Info, bool retain = false);

        void LogProgress(double current, double total, string? message = null, bool retain = false);
    }
}
