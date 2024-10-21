using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public interface ITextureFilterProvider
    {
        void Kernel3x3(Texture2D src, Texture2D dst, float[] data);

        void Blur(Texture2D src, Texture2D dst) => Kernel3x3(src, dst,
        [
            1 / 16f, 2 / 16f, 1 / 16f,
            2 / 16f, 4 / 16f, 2 / 16f,
            1 / 16f, 2 / 16f, 1 / 16f,
        ]);
    }
}
