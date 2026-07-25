#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL
{
    public unsafe class GlBufferUpdateTracker
    {
        private class BufferState
        {
            public required IBuffer Buffer;
            public required BufferTargetARB Target;

            public byte[] Data = [];
            public byte[] Valid = [];
        }

        private readonly GL _gl;

        private readonly Dictionary<IBuffer, BufferState> _buffers =
            new(ReferenceEqualityComparer.Instance);

        public GlBufferUpdateTracker(GL gl)
        {
            _gl = gl;
        }

        public void Update(
            IBuffer buffer,
            BufferTargetARB target,
            void* data,
            uint sizeBytes,
            int offsetBytes)
        {
            if (sizeBytes == 0)
                return;

            var endBytes = checked(offsetBytes + (int)sizeBytes);

            if (!_buffers.TryGetValue(buffer, out var state))
            {
                state = new BufferState
                {
                    Buffer = buffer,
                    Target = target
                };

                _buffers.Add(buffer, state);
            }

            if (state.Data.Length < endBytes)
            {
                Array.Resize(ref state.Data, endBytes);
                Array.Resize(ref state.Valid, endBytes);
            }

            new ReadOnlySpan<byte>(data, checked((int)sizeBytes))
                .CopyTo(state.Data.AsSpan(offsetBytes, checked((int)sizeBytes)));

            state.Valid.AsSpan(offsetBytes, checked((int)sizeBytes)).Fill(1);
        }

        public void Clear(IBuffer buffer)
        {
            _buffers.Remove(buffer);
        }

        public bool CheckAll()
        {
            var result = true;

            Log.Info(this, "Check begin, {0} total", _buffers.Values.Count);

            foreach (var state in _buffers.Values)
            {
                if (!Check(state))
                    result = false;
            }

            Log.Info(this, "Check end");

            return result;
        }

        private bool Check(BufferState state)
        {
            if (state.Data.Length == 0)
                return true;

            var handle = state.Buffer.Handle;

            GlState.Current.BindBuffer(state.Target, handle);

            var pData = (byte*)_gl.MapBufferRange(
                state.Target,
                0,
                (nuint)state.Data.Length,
                MapBufferAccessMask.ReadBit);

            if (pData == null)
            {
                Log.Error(this, "Unable to map buffer {0} for verification", handle);
                return false;
            }

            var result = true;

            try
            {
                for (var i = 0; i < state.Data.Length; i++)
                {
                    if (state.Valid[i] == 0)
                        continue;

                    if (pData[i] == state.Data[i])
                        continue;

                    Log.Error(
                        this,
                        "BUFFER CORRUPTION handle={0}, offset={1}, expected={2:X2}, actual={3:X2}",
                        handle,
                        i,
                        state.Data[i],
                        pData[i]);

                    result = false;
                    break;
                }
            }
            finally
            {
                _gl.UnmapBuffer(state.Target);
            }

            return result;
        }
    }
}