namespace Fftw
{
    public readonly struct FftwPlan : IDisposable
    {
        public FftwPlan(nint handle)
        {
            Handle = handle;
        }

        public void Execute()
        {
            FftwLib.fftw_execute(Handle);
        }

        public void Dispose()
        {
            FftwLib.fftw_free(Handle);
        }

        public readonly nint Handle;
    }
}
