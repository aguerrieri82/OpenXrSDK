using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;

namespace XrSamples
{
    public class NullProgressLogger : IProgressLogger
    {
        public void BeginTask(string? message = null)
        {

        }

        public void EndTask()
        {

        }

        public void LogMessage(string text, LogLevel level = LogLevel.Info, bool retain = false)
        {

        }

        public void LogProgress(double current, double total, string? message = null, bool retain = false)
        {
 
        }
    }
}

