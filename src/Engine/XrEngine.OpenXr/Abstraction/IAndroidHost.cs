using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.OpenXr
{
    public interface IAndroidHost
    {
        nint NativeContext { get; }

        nint NativeJniEnv { get; }
    }
}
