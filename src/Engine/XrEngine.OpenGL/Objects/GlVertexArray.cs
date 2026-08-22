#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;
using System.Runtime.InteropServices;

namespace XrEngine.OpenGL
{
    public class GlVertexArray<TVertexType, TIndexType> : GlObject, IGlVertexArray
        where TVertexType : unmanaged
        where TIndexType : unmanaged
    {
        public class AttributeInfo
        {
            public IGlBuffer? Buffer;

            public GlVertexLayout? Layout;

            public Type? ElementType;

            public int ElementSize;
        }

        protected GlVertexLayout _mainLayout;
        protected readonly GlBuffer<TVertexType> _vBuf;
        protected GlBuffer<TIndexType>? _iBuf;

        protected List<AttributeInfo>? _attributes;

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
            _mainLayout = layout;
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

        public void AddAttributes(IGlBuffer buffer, GlVertexLayout layout, Type elementType)
        {
            _attributes ??= [];

            _attributes.Add(new AttributeInfo
            {
                Buffer = buffer,
                Layout = layout,
                ElementType = elementType,
                ElementSize = MarshalCache.SizeOf(elementType)
            });

            Bind();

            Configure(buffer, layout);

            Unbind();
        }

        public IList<AttributeInfo>? Attributes => _attributes;

        public void Build()
        {
            if (_handle == 0)
                throw new InvalidOperationException();

            Bind();

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

            Configure(_vBuf, _mainLayout);

            if (_attributes != null)
            {
                foreach (var attr in _attributes)
                    Configure(attr.Buffer!, attr.Layout!);
            }

            Unbind();

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

        public void UpdateMainLayouts(GlVertexLayout layout)
        {
            _mainLayout = layout;

            Bind();

            Configure(_vBuf, _mainLayout);

            Unbind();
        }

        protected unsafe void Configure(IGlBuffer buffer, GlVertexLayout layout)
        {
            buffer.Bind();

            foreach (var attr in layout.Attributes!)
            {
                _gl.EnableVertexAttribArray(attr.Location);

                if (attr.IsIntegerStore)
                    _gl.VertexAttribIPointer(attr.Location, (int)attr.Count, (VertexAttribIType)attr.Type, layout.Size, (void*)attr.Offset);
                else
                    _gl.VertexAttribPointer(attr.Location, (int)attr.Count, attr.Type, attr.IsNormalized, layout.Size, (void*)attr.Offset);
            }

            buffer.Unbind();
        }

        public unsafe void UpdateAttributes(Array data, int index)
        {
            var attr = _attributes![index];

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var ptr = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
                var span = new Span<byte>(ptr, data.Length * attr.ElementSize);
                attr.Buffer!.UpdateRange(span);
            }
            finally
            {
                handle.Free();
            }

        }

        public void UpdateMain(TVertexType[] vertices, TIndexType[]? indices = null)
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

            if (_attributes != null)
            {
                foreach (var attr in _attributes)
                    attr.Buffer?.Dispose();
                _attributes = null;
            }

            _iBuf?.Dispose();

            _vBuf.Dispose();

            base.Dispose();
        }

        public GlVertexLayout MainLayout => _mainLayout;

        public GlBuffer<TVertexType> VBuf => _vBuf;

        public GlBuffer<TIndexType>? IBuf => _iBuf;

        #region IGlVertexArray

        void IGlVertexArray.Update(object vertexSpan, object? indexSpan)
        {
            UpdateMain((TVertexType[])vertexSpan, (TIndexType[]?)indexSpan);
        }

        Type IGlVertexArray.VertexType => typeof(TVertexType);

        Type IGlVertexArray.IndexType => typeof(TIndexType);

        #endregion

    }
}
