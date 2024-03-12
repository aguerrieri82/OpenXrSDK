﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public interface IGlProgramFactory
    {
        GlProgram CreateProgram(GL gl, ShaderMaterial material);
    }
}