#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEngine.OpenGL
{
    public class GlVertexArray<TVertexType, TIndexType> : GlObject, IGlVertexArray
        where TVertexType : unmanaged
        where TIndexType : unmanaged
    {
        protected readonly GlVertexLayout _layout;
        protected readonly GlBuffer<TVertexType> _vBuf;
        protected readonly GlBuffer<TIndexType>? _iBuf;
        protected readonly DrawElementsType _drawType;


        public unsafe GlVertexArray(GL gl, TVertexType[] vertices, TIndexType[]? index, GlVertexLayout layout)
            : this(gl,
                  new GlBuffer<TVertexType>(gl, vertices.AsSpan(), BufferTargetARB.ArrayBuffer),
                  index == null || index.Length == 0 ? null : new GlBuffer<TIndexType>(gl, index.AsSpan(), BufferTargetARB.ElementArrayBuffer),
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
            GlDebug.Log($"GenVertexArray {_handle}");

            Bind();

            _vBuf.Bind();

            _iBuf?.Bind();

            Configure();

            Unbind();
        }

        public unsafe void Draw(PrimitiveType primitive = PrimitiveType.Triangles)
        {

            if (_iBuf != null)
            {
                _gl.DrawElements(primitive, _iBuf.Length, _drawType, null);
                GlDebug.Log($"DrawElements Triangles {_iBuf.Length} {_drawType}");
            }

            else
            {
                _gl.DrawArrays(primitive, 0, _vBuf.Length);
                GlDebug.Log($"DrawArrays Triangles {_vBuf.Length}");
            }
        }

        protected unsafe void Configure()
        {
            foreach (var attr in _layout.Attributes!)
            {
                _gl.EnableVertexAttribArray(attr.Location);
                _gl.VertexAttribPointer(attr.Location, (int)attr.Count, attr.Type, false, _layout.Size, (void*)attr.Offset); 
            }
        }

        public void Bind()
        {
            _gl.BindVertexArray(_handle);
            GlDebug.Log($"BindVertexArray {_handle}");
        }

        public void Unbind()
        {
            _gl.BindVertexArray(0);
            GlDebug.Log($"BindVertexArray NULL");
        }

        public override void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
            _handle = 0;
            GC.SuppressFinalize(this);
        }

        public GlVertexLayout Layout => _layout;

        internal void Update(TVertexType[] vertices, TIndexType[]? indices = null)
        {
            _vBuf.Update(vertices);
            _iBuf?.Update(indices);
        }

        void IGlVertexArray.Update(object vertexSpan, object? indexSpan)
        {
            Update((TVertexType[])vertexSpan, (TIndexType[]?)indexSpan);
        }

        Type IGlVertexArray.VertexType => typeof(TVertexType);

        Type IGlVertexArray.IndexType => typeof(TIndexType);

    }
}
