#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGLES
{
    public class GlVertexArray<TVertexType, TIndexType> : GlObject
        where TVertexType : unmanaged
        where TIndexType : unmanaged
    {
        GlVertexLayout _layout;

        GlBuffer<TVertexType> _vBuf;
        
        GlBuffer<TIndexType>? _iBuf;
        private DrawElementsType _drawType;

        public unsafe GlVertexArray(GL gl, TVertexType[] vertices, TIndexType[] index, GlVertexLayout layout)
            : this(gl, 
                  new GlBuffer<TVertexType>(gl, vertices.AsSpan(), BufferTargetARB.ArrayBuffer),
                  index.Length == 0 ? null : new GlBuffer<TIndexType>(gl, index.AsSpan(), BufferTargetARB.ElementArrayBuffer),
                  layout)
        {

        }


        public GlVertexArray(GL gl, GlBuffer<TVertexType> vBuf, GlBuffer<TIndexType>? iBuf, GlVertexLayout layout)
            : base(gl)
        {
            _gl = gl;
            _layout = layout;
            _vBuf = vBuf;
            _iBuf = iBuf;

            if (typeof(TIndexType) == typeof(uint))
                _drawType = DrawElementsType.UnsignedInt;
            else if (typeof(TIndexType) == typeof(ushort))
                _drawType = DrawElementsType.UnsignedShort;
            else if (typeof(TIndexType) == typeof(byte))
                _drawType = DrawElementsType.UnsignedByte;
            else
                throw new NotSupportedException();

            _handle = _gl.GenVertexArray();

            Bind();

            _vBuf.Bind();

            _iBuf?.Bind();

            Configure();

            Unbind();
        }

        public void Draw(uint count)
        {
   
            if (_iBuf != null)
                _gl.DrawElements(PrimitiveType.Triangles, count, _drawType, 0);
            else
                _gl.DrawArrays(PrimitiveType.Triangles, 0, count);

            var error = _gl.GetError();

            Console.WriteLine(error);

        }
  
        protected unsafe void Configure()
        {

            foreach (var attr in _layout.Attributes!)
            {
                _gl.VertexAttribPointer(attr.Location, (int)attr.Count, attr.Type, false, _layout.Size, (void*)attr.Offset);
                _gl.EnableVertexAttribArray(attr.Location);
            }
        }

        public void Bind()
        {
            _gl.BindVertexArray(_handle);
        }

        public void Unbind()
        {
            _gl.BindVertexArray(0);
        }

        public override void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
        }
    }
}
