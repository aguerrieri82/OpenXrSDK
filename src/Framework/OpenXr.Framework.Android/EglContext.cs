using Silk.NET.Core.Contexts;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenXr.Framework.Android
{
    public class EglContext : INativeContext
    {
        [DllImport("libEGL.so", EntryPoint = "eglGetProcAddress")]
        private static extern nint EglGetProcAddress([MarshalAs(UnmanagedType.LPStr)] string name);

        public void Dispose()
        {

        }

        public nint GetProcAddress(string proc, int? slot = null)
        {
            return EglGetProcAddress(proc);
        }

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
        {
            addr = GetProcAddress(proc);
            return addr != 0;
        }
    }
}
