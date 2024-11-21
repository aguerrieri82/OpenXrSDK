namespace Fftw
{

    public unsafe class FftwBuffer<T> : IDisposable where T : struct
    {
        private readonly int _length;

        public FftwBuffer(int length)
        {
            Pointer = (T*)FftwLib.fftw_malloc(length * sizeof(T));
            _length = length;
        }


        public void Dispose()
        {
            if (Pointer != null)
            {
                FftwLib.fftw_free((nint)Pointer);
                Pointer = null;
            }
            GC.SuppressFinalize(this);  
        }

        public void CopyFrom(FftwBuffer<T> src, int srcOffset, int count)
        {
            Buffer.MemoryCopy(&src.Pointer[srcOffset], Pointer, _length * sizeof(T), count * sizeof(T));
        }

        public void CopyFrom(T[] src, int srcOffset, int count)
        {
            fixed (T* pSrc = src)
                Buffer.MemoryCopy(&pSrc[srcOffset], Pointer, _length * sizeof(T), count * sizeof(T));
        }

        public void CopyTo(ref T[] dst, int srcOffset, int count)
        {
            Array.Resize(ref dst, count);

            fixed (T* pDst = dst)
                Buffer.MemoryCopy(&Pointer[srcOffset], pDst, dst.Length * sizeof(T), count * sizeof(T));
        }

        public Span<T> ToSpan()
        {
            return ToSpan(0, _length);
        }

        public Span<T> ToSpan(int offset, int count)
        {
            return new Span<T>(&Pointer[offset], count);
        }

        public int Length => _length;


        public T* Pointer;

    }
}
