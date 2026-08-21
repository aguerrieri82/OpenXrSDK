using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface IScreenRefractionSource
    {

       Texture2D?[] GetRefractionTextures(PerspectiveCamera camera);

        int Priority { get; }
    }
}
