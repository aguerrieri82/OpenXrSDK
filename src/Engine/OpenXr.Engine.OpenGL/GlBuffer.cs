#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGLES
{
    public class GlBuffer<T> : GlObject where T : unmanaged
    {
        private BufferTargetARB _bufferType;

        public unsafe GlBuffer(GL gl, Span<T> data, BufferTargetARB bufferType)
            : base(gl)
        {
            _gl = gl;
            _bufferType = bufferType;
            _handle = _gl.GenBuffer();

            Update(data);
        }

        public void Update(Span<T> data)
        {
            Bind();

            _gl.BufferData<T>(_bufferType, data, BufferUsageARB.StaticDraw);

            Unbind();
        }

        public void Bind()
        {
            _gl.BindBuffer(_bufferType, _handle);
        }

        public void Unbind()
        {
            _gl.BindBuffer(_bufferType, 0);
        }

        public override void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}
