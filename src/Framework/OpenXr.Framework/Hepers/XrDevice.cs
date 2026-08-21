#if __ANDROID__
using Android.OS;
#endif

namespace OpenXr.Framework
{
    public static class XrDevice
    {
#if __ANDROID__
        public static bool IsMetaQuest =
                string.Equals(Build.Manufacturer, "Oculus", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Build.Manufacturer, "Meta", StringComparison.OrdinalIgnoreCase) ||
                (Build.Model?.Contains("Quest", StringComparison.OrdinalIgnoreCase) == true);
#else
        public static bool IsMetaQuest = true;
#endif

    }
}
