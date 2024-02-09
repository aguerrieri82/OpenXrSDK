using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL
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
            Bind();
            fixed (void* d = data)
            {
                _gl.BufferData<T>(bufferType, data, BufferUsageARB.StaticDraw);
            }
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
