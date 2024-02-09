using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL
{
    public class GlVertexArray<TVertexType, TIndexType> : GlObject
        where TVertexType : unmanaged
        where TIndexType : unmanaged
    {
        GlVertexLayout _layout;



        public unsafe GlVertexArray(GL gl, TVertexType[] vertices, TIndexType[] index, GlVertexLayout layout)
            : this(gl, 
                  new GlBuffer<TVertexType>(gl, vertices.AsSpan(), BufferTargetARB.ArrayBuffer),
                  new GlBuffer<TIndexType>(gl, index.AsSpan(), BufferTargetARB.ElementArrayBuffer),
                  layout)
        {

        }


        public GlVertexArray(GL gl, GlBuffer<TVertexType> vbo, GlBuffer<TIndexType> ebo, GlVertexLayout layout)
            : base(gl)
        {
            _gl = gl;
            _layout = layout;
            _handle = _gl.GenVertexArray();

            Bind();
            vbo.Bind();
            ebo.Bind();
            Configure();
            Unbind();
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
