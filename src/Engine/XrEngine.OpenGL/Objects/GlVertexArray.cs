﻿#if GLES
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

            Create();

            Bind();

            _vBuf.Bind();

            _iBuf?.Bind();

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
                _gl.DrawElements(primitive, _iBuf.ArrayLength, _drawType, null);
            else
                _gl.DrawArrays(primitive, 0, _vBuf.ArrayLength);
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
            _vBuf.UpdateRange(vertices);
            _iBuf?.UpdateRange(indices);
        }

        public void Bind()
        {
            GlState.Current!.BindVertexArray(_handle);
        }

        public void Unbind()
        {
            GlState.Current!.BindVertexArray(0);
        }

        public override void Dispose()
        {
            if (_handle != 0)
                _gl.DeleteVertexArray(_handle);

            base.Dispose();
        }

        public GlVertexLayout Layout => _layout;

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
