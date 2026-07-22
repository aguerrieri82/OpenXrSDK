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
        protected GlVertexLayout _layout;
        protected readonly GlBuffer<TVertexType> _vBuf;
        protected GlBuffer<TIndexType>? _iBuf;
        protected long _vBufVersion;
        protected long _iBufVersion;
        protected readonly DrawElementsType _drawType;

        public GlVertexArray(GL gl, TVertexType[] vertices, TIndexType[]? index, GlVertexLayout layout)
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

            Create();
            Build();
        }

        public void Build()
        {
            if (_handle == 0)
                throw new InvalidOperationException();

            Bind();

            _vBuf.Bind();
            _vBufVersion = _vBuf.CreateVersion;

            if (_iBuf != null)
            {
                _iBuf.Bind();
                _iBufVersion = _iBuf.CreateVersion;
            }
            else
            {
                GlState.Current.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
                _iBufVersion = 0;
            }

            Configure();

            Unbind();

            _vBuf.Unbind();

            _iBuf?.Unbind();
        }

        protected void Create()
        {
            _handle = _gl.GenVertexArray();
        }

        public unsafe void DrawInstances(PrimitiveType primitive, int count)
        {
            if (_iBuf != null)
                _gl.DrawElementsInstanced(primitive, _iBuf.ArrayLength, _drawType, null, (uint)count);
            else
                _gl.DrawArraysInstanced(primitive, 0, _vBuf.ArrayLength, (uint)count);
        }

        public unsafe void Draw(PrimitiveType primitive = PrimitiveType.Triangles)
        {
            if (_iBuf != null)
            {
                if (_iBuf.ArrayLength == 0)
                    return;
                _gl.DrawElements(primitive, _iBuf.ArrayLength, _drawType, null);
            }
            else
            {
                if (_vBuf.ArrayLength == 0)
                    return;
                _gl.DrawArrays(primitive, 0, _vBuf.ArrayLength);
            }
        }

        public void UpdateLayout(GlVertexLayout layout)
        {
            _layout = layout;

            Bind();

            _vBuf.Bind();

            Configure();

            _vBuf.Unbind();

            Unbind();
        }

        protected unsafe void Configure()
        {
            foreach (var attr in _layout.Attributes!)
            {
                _gl.EnableVertexAttribArray(attr.Location);
                _gl.VertexAttribPointer(attr.Location, (int)attr.Count, attr.Type, false, _layout.Size, (void*)attr.Offset);
            }
        }

        public void Update(TVertexType[] vertices, TIndexType[]? indices = null)
        {
            if (EnableDebug)
                GlDebug.Log(this, "Update VA {0}", _handle);

            _vBuf.UpdateRange(vertices, 0, false);
            _vBuf.ArrayLength = (uint)vertices.Length;

            var hasIndices = indices != null && indices.Length > 0;

            var rebuild = false;

            if (hasIndices)
            {
                if (_iBuf == null)
                {
                    _iBuf = new GlBuffer<TIndexType>(
                        _gl,
                        indices!,
                        BufferTargetARB.ElementArrayBuffer);

                    rebuild = true;

                }
                else
                    _iBuf.UpdateRange(indices!, 0, false);

                _iBuf.ArrayLength = (uint)indices!.Length;
            }
            else
            {
                if (_iBuf != null)
                {
                    _iBuf.Dispose();
                    _iBuf = null;
                    rebuild = true;
                }
            }

            if (!rebuild && (_vBufVersion != _vBuf.CreateVersion || (_iBuf != null && _iBuf.CreateVersion != _iBufVersion)))
                rebuild = true;

            if (rebuild)
                Build();
        }

        public void Bind()
        {
            GlState.Current.BindVertexArray(_handle);
        }

        public void Unbind()
        {
            GlState.Current.BindVertexArray(0);
        }

        public override void Dispose()
        {
            if (_handle != 0)
            {
                Unbind();

                _gl.DeleteVertexArray(_handle);

                if (EnableDebug)
                    GlDebug.Log(this, "VA {0} deleted", _handle);
            }

            _iBuf?.Dispose();

            _vBuf.Dispose();

            base.Dispose();
        }

        public GlVertexLayout Layout => _layout;

        public GlBuffer<TVertexType> VBuf => _vBuf;

        public GlBuffer<TIndexType>? IBuf => _iBuf;

        #region IGlVertexArray

        void IGlVertexArray.Update(object vertexSpan, object? indexSpan)
        {
            Update((TVertexType[])vertexSpan, (TIndexType[]?)indexSpan);
        }

        Type IGlVertexArray.VertexType => typeof(TVertexType);

        Type IGlVertexArray.IndexType => typeof(TIndexType);

        #endregion

    }
}
