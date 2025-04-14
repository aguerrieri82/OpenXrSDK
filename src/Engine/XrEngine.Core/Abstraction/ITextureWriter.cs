using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface ITextureWriter
    {
        void SaveTexture(Stream stream, IList<TextureData> images);
    }
}
