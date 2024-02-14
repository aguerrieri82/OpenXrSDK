﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace OpenXr.Engine.OpenGL
{
    public class GlDefaultRenderTarget : IGlRenderTarget
    {
        GL _gl;
        
        public GlDefaultRenderTarget(GL gl)
        {
            _gl = gl;   
        }

        public void Begin()
        {

        }

        public void Dispose()
        {
        }

        public void End()
        {
        }

        public uint QueryTexture(FramebufferAttachment attachment)
        {
            throw new NotSupportedException();
        }
    }
}