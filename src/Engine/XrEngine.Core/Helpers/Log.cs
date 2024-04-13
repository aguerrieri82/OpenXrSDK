using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public static class Log
    {
        public static void Info(object source, string message, params object[] args)
        {
           Logger.LogMessage(string.Format(message, args), LogLevel.Info);
        }

        public static void Debug(object source, string message, params object[] args)
        {
            Logger.LogMessage(string.Format(message, args), LogLevel.Debug);
        }

        public static IProgressLogger Logger => Context.Require<IProgressLogger>();
    }
}
