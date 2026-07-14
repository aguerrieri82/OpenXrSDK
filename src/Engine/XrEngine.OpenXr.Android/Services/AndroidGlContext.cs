using OpenXr.Framework.Android;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Text;
using XrEngine.OpenGL;

namespace XrEngine.OpenXr.Android
{
    public class AndroidGlContext : IGlContext
    {
        readonly OpenGLESContext _ctx;
        readonly GL _gl;
        Thread? _ownerThread;

        public AndroidGlContext(OpenGLESContext ctx, GL gl)
        {
            _ctx = ctx;
            _gl = gl;
        }


        public void Dispose()
        {
            _ctx.Destroy();
        }

        public void Release()
        {
            _ctx.Release();

            _ownerThread = null;

            if (AndroidPlatform._currentGlContext == this)
                AndroidPlatform._currentGlContext = null;   
        }

        public void Take()
        {
            _ctx.Take();

            _ownerThread = Thread.CurrentThread;

            AndroidPlatform._currentGlContext = this;
        }

        public AndroidGlContext CreateShared(bool debugMode)
        {
            var newCtx = _ctx.CreateShared(debugMode);
            return new AndroidGlContext(newCtx, _gl);
        }

        public GL Gl => _gl;

        public Thread? OwnerThread => _ownerThread;

    }
}
