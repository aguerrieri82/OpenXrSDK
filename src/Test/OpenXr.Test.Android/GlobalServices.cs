using OpenXr.Framework;

namespace OpenXr.Test.Android
{
    internal class GlobalServices
    {
        public static XrApp? App { get; set; }

        public static IServiceProvider? ServiceProvider { get; internal set; }
    }
}
