using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface IQuodDepthCull
    {
        void Cull(IQuodTexture texture);
    }
}
