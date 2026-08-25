using System;
using System.Collections.Generic;
using System.Text;
using XrMath;

namespace XrEngine
{
    public struct TextureLayout
    {
        public Texture2D Texture;

        public ITextureLayout Layout;
    }

    public interface IBlurMipPack
    {
        TextureLayout Generate(Texture2D source, Rect2I sourceRect, float? roughness = null);

    }
}
