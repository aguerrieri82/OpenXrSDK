using System.Runtime.InteropServices;

namespace OpenXr.Framework
{
    public static class Gpu
    {
        [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface")]
        static extern int LoadNvApi64(int offset);

        [DllImport("nvapi.dll", EntryPoint = "nvapi_QueryInterface")]
        static extern void LoadNvApi32(int offset);

        public static void EnableNvAPi()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (Environment.Is64BitProcess)
                        LoadNvApi64(0);
                    else
                        LoadNvApi32(0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

    }
}
