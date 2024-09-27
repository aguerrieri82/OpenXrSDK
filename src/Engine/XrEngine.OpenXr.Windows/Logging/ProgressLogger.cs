using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace XrEngine.OpenXr.Windows
{
    public class ProgressLogger : IProgressLogger
    {
        private readonly ILogger _logger;

        public ProgressLogger()
        {
            _logger = Context.Require<ILogger>();   

        }

        public void BeginTask(string? message = null)
        {

        }

        public void EndTask()
        { 

        }

        public void LogMessage(string text, LogLevel level = LogLevel.Info, bool retain = false)
        {
            var msLevel = level switch {
                LogLevel.Info => MsLogLevel.Information,
                LogLevel.Warning => MsLogLevel.Warning, 
                LogLevel.Error => MsLogLevel.Error,
                LogLevel.Debug => MsLogLevel.Debug,
                _  => MsLogLevel.Information,
            };

            _logger.Log(msLevel, text);
        }

        public void LogProgress(double current, double total, string? message = null, bool retain = false)
        {

        }
    }
}
