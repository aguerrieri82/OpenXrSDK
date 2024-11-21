using System.Numerics;
using System.Runtime.InteropServices;

namespace Fftw
{
    public enum DftFlags : uint
    {
        FFTW_MEASURE = 0,
        FFTW_DESTROY_INPUT = 1 << 0,
        FFTW_UNALIGNED = 1 << 1,
        FFTW_CONSERVE_MEMORY = 1 << 2,
        FFTW_EXHAUSTIVE = 1 << 4,
        FFTW_PRESERVE_INPUT = 1 << 4,
        FFTW_PATIENT = 1 << 5,
        FFTW_ESTIMATE = 1 << 6,
        FFTW_WISDOM_ONLY = 1 << 21
    }

    public static unsafe class FftwLib
    {
        [DllImport("fftw3")]
        static internal extern nint fftw_plan_dft_r2c_1d(int n0, double* inData, Complex* outComplex, DftFlags flags);

        [DllImport("fftw3")]
        static internal extern nint fftw_plan_dft_c2r_1d(int n0, Complex* inData, double* outReal, DftFlags flags);

        [DllImport("fftw3")]
        static internal extern void fftw_destroy_plan(nint plan);

        [DllImport("fftw3")]
        static internal extern void fftw_execute(nint plan);

        [DllImport("fftw3")]
        static internal extern void fftw_free(nint data);

        [DllImport("fftw3")]
        static internal extern nint fftw_malloc(long size);


        public static void Dft(FftwBuffer<double> inData, FftwBuffer<Complex> outData, DftFlags flags = DftFlags.FFTW_ESTIMATE)
        {
            var result = fftw_plan_dft_r2c_1d(inData.Length, inData.Pointer, outData.Pointer, flags);
            fftw_execute(result);
            fftw_destroy_plan(result);
        }

        public static void Dft(FftwBuffer<Complex> inData, FftwBuffer<double> outData, DftFlags flags = DftFlags.FFTW_ESTIMATE)
        {
            var result = fftw_plan_dft_c2r_1d(outData.Length, inData.Pointer, outData.Pointer, flags);
            fftw_execute(result);
            fftw_destroy_plan(result);
        }

        public static FftwPlan DftPlan(FftwBuffer<double> inData, FftwBuffer<Complex> outData, DftFlags flags = DftFlags.FFTW_ESTIMATE)
        {
            var result = fftw_plan_dft_r2c_1d(inData.Length, inData.Pointer, outData.Pointer, flags);
            return new FftwPlan(result);    
        }
    }
}
