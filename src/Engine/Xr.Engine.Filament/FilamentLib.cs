using System.Runtime.InteropServices;

namespace Xr.Engine.Filament
{
    internal static class FilamentLib
    {
        internal struct InitializeOptions
        {

        };

        [DllImport("filament-native")]
        internal static extern void Initialize(ref InitializeOptions options);
    }
}
