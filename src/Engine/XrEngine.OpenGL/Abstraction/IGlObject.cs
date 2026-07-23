#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Diagnostics;

namespace XrEngine.OpenGL
{
    public interface IGlObject
    {
        public uint Handle { get; }

        IGlContext Owner { get; }

        GL GL { get; }

        void SetLabel(string? label);

    }
}
