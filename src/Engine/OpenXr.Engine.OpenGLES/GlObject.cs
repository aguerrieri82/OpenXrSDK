﻿using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGLES
{
    public abstract class GlObject : IDisposable
    {
        protected uint _handle;
        protected GL _gl;

        protected GlObject(GL gl)
        {
            _gl = gl;
        }


        public abstract void Dispose();


        public uint Handle => _handle;


        public static implicit operator uint(GlObject obj)
        {
            return obj._handle;
        }
    }
}
