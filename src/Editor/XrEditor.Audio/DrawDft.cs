using Fftw;
using SkiaSharp;
using System.Numerics;
using XrMath;
using Color = XrMath.Color;

namespace XrEditor.Audio
{
    public class DrawDft : IDraw2D
    {
        readonly List<Complex[]> _dft = [];

        private readonly float _freqStep;
        private readonly int _maxIndex;
        private readonly int _minIndex;
        private readonly float _min;
        private readonly float _max;
        private readonly float _avg;
        private readonly float _sigma;
        private readonly uint _sampleRate;
        private SKBitmap? _image;

        public unsafe DrawDft(float[] data, uint sampleRate, uint dftSize)
        {
            _sampleRate = sampleRate;

            using FftwBuffer<double> aData = new FftwBuffer<double>(data.Length);
            using FftwBuffer<double> aIn = new FftwBuffer<double>((int)dftSize);
            using FftwBuffer<Complex> aOut = new FftwBuffer<Complex>((int)dftSize / 2 + 1);
            using FftwPlan plan = FftwLib.DftPlan(aIn, aOut);


            fixed (float* pData = data)
            {
                for (int j = 0; j < data.Length; j++)
                    aData.Pointer[j] = pData[j];
            }

            int i = 0;
            while (i < data.Length)
            {
                if (data.Length - i < dftSize)
                    break;

                aIn.CopyFrom(aData, i, (int)dftSize);

                plan.Execute();

                _dft.Add(aOut.ToSpan().ToArray());

                i += (int)dftSize;
            }

            _freqStep = (float)_sampleRate / dftSize;

            _maxIndex = Math.Min(((int)dftSize / 2) - 1, (int)Math.Floor(MaxFreq / _freqStep));
            _minIndex = Math.Min(((int)dftSize / 2) - 1, (int)Math.Floor(MinFreq / _freqStep));

            _min = float.PositiveInfinity;
            _max = float.NegativeInfinity;
            _avg = 0;

            int count = 0;

            IEnumerable<float> values = _dft.SelectMany(a => a.Skip(_minIndex)
                .Take(_maxIndex - _minIndex)
                .Select(a => (float)Math.Log10(a.Magnitude)));

            foreach (float item in values)
            {
                float realItem = float.IsInfinity(item) ? 0 : item;
                _min = MathF.Min(_min, item);
                _max = MathF.Max(_max, item);
                _avg += item;
                count++;
            }

            _avg /= count;

            _sigma = 0;

            foreach (float item in values)
                _sigma += (item - _avg) * (item - _avg);

            _sigma = MathF.Sqrt(_sigma / count);
        }

        public unsafe void Draw(SKCanvas canvas, Rect2 rect)
        {

            if (_image == null)
            {
                int groupSize = 1;

                float min = _avg - _sigma * 3;
                float max = _avg + _sigma * 3;

                //min = _min;
                //max = _max;

                Color[] colors = ["#000000", "#800080", "#800000", "#808000"];

                _image = new SKBitmap(_dft.Count, ((_maxIndex - _minIndex) + 1) / groupSize, SKColorType.Rgba8888, SKAlphaType.Unpremul);

                fixed (byte* pBytes = _image.GetPixelSpan())
                {
                    for (int x = 0; x < _image.Width; x++)
                    {
                        Complex[] dft = _dft[x];
                        for (int y = 0; y < _image.Height; y++)
                        {
                            int index = _minIndex + y * groupSize;

                            float value = 0;
                            for (int k = 0; k < groupSize; k++)
                                value += (float)dft[index + k].Magnitude;

                            value /= groupSize;

                            float alpha = (MathF.Log10(value) - min) / (max - min);

                            Color c;
                            if (alpha >= 1)
                                c = colors[^1];
                            else if (alpha <= 0)
                                c = colors[0];
                            else
                            {
                                float cSize = 1f / (colors.Length - 1);
                                float cOfs = alpha / cSize;
                                int cIndex = (int)Math.Floor(cOfs);
                                float cAlpha = (alpha - (cIndex * cSize)) / cSize;
                                Color c1 = colors[cIndex];
                                Color c2 = colors[cIndex + 1];
                                c = new Color(
                                    c1.R * (1 - cAlpha) + c2.R * cAlpha,
                                    c1.G * (1 - cAlpha) + c2.G * cAlpha,
                                    c1.B * (1 - cAlpha) + c2.B * cAlpha
                                );
                            }

                            int px = (y * _image.Width * 4) + (x * 4);
                            pBytes[px] = (byte)(c.R * 255);
                            pBytes[px + 1] = (byte)(c.G * 255);
                            pBytes[px + 2] = (byte)(c.B * 255);
                            pBytes[px + 3] = 255;
                        }
                    }
                }
            }

            //var sampOp = new SKSamplingOptions(SKFilterMode.Linear);

            canvas.DrawBitmap(_image, new SKRect(0, 0, _image.Width, _image.Height));
        }


        public float MinFreq = 0;

        public float MaxFreq = 1000;

    }
}
