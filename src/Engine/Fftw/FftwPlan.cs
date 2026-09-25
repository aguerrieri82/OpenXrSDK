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
            if (Handle != 0)
                FftwLib.fftw_destroy_plan(Handle);
        }

        public readonly nint Handle;
    }
}