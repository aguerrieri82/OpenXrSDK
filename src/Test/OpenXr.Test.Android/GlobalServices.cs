using Microsoft.Extensions.DependencyInjection;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Test.Android
{
    internal class GlobalServices
    {
        public static XrApp? App { get; set; }

        public static IServiceProvider? ServiceProvider { get; internal set; }
    }
}
