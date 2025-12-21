namespace XrEngine
{
    public static class TextureFilterUtils
    {
        static readonly float[] BLUR_KERNEL_3x3 = [
            1 / 16f, 2 / 16f, 1 / 16f,
            2 / 16f, 4 / 16f, 2 / 16f,
            1 / 16f, 2 / 16f, 1 / 16f,
        ];

        static readonly Dictionary<int, float[]> BLUR_KERNELS = [];

        public static float[] BuildGaussianWeights(int radius, float sigma)
        {
            if (radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius));

            if (!(sigma > 0.0f))
                throw new ArgumentOutOfRangeException(nameof(sigma));

            var w = new float[radius + 1];

            var twoSigma2 = 2.0 * sigma * sigma;
            for (var i = 0; i <= radius; i++)
            {
                double x = i;
                w[i] = (float)Math.Exp(-(x * x) / twoSigma2);
            }

            double sum = w[0];
            for (var i = 1; i <= radius; i++)
                sum += 2.0 * w[i];

            var inv = (float)(1.0 / sum);
            for (var i = 0; i <= radius; i++)
                w[i] *= inv;

            return w;
        }

        public static float[] BuildGaussianWeights(int radius)
        {
            if (!BLUR_KERNELS.TryGetValue(radius, out var data))
            {
                if (radius == 0)
                    return [1.0f];

                var sigma = Math.Max(0.0001f, radius / 3.0f);
                data = BuildGaussianWeights(radius, sigma);
                BLUR_KERNELS[radius] = data;
            }

            return data;
        }


        public static void Blur(this ITextureFilterProvider fp, Texture2D src, Texture2D dst, string key, int activeChannels) =>
            fp.Kernel3x3(src, dst, BLUR_KERNEL_3x3, key, activeChannels);

        public static void BlurX(this ITextureFilterProvider fp, Texture2D src, Texture2D dst, int size, string key, int activeChannels) =>
            fp.KernelX(src, dst, BuildGaussianWeights(size), key, activeChannels);

        public static void BlurY(this ITextureFilterProvider fp, Texture2D src, Texture2D dst, int size, string key, int activeChannels) =>
            fp.KernelY(src, dst, BuildGaussianWeights(size), key, activeChannels);
    }

    public interface ITextureFilterProvider
    {
        void Kernel3x3(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels);

        void KernelX(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels);

        void KernelY(Texture2D src, Texture2D dst, float[] data, string key, int activeChannels);
    }
}
