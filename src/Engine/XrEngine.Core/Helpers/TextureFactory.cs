using Common.Interop;
using XrMath;

namespace XrEngine.Helpers
{
    public static class TextureFactory
    {
        public unsafe static Texture2D CreateChecker()
        {
            return CreateChecker(16, 16, Color.White, Color.Black);
        }

        public unsafe static Texture2D CreateChecker(uint sizeX, uint sizeY, Color color1, Color color2)
        {
            var textureData = new TextureData
            {
                Width = sizeX * 2,
                Height = sizeY * 2,
                Format = TextureFormat.Rgba32
            };

            textureData.Data = MemoryBuffer.Create<byte>(textureData.Width * textureData.Height * 4);

            using var dataLock = textureData.Data.MemoryLock();

            var lineSize = textureData.Width * 4;

            for (var y = 0; y < textureData.Height; y++)
            {
                for (var x = 0; x < textureData.Width; x++)
                {
                    var color = (x < sizeX && y < sizeY) || (x >= sizeX && y >= sizeY) ? color1 : color2;
                    color.ToBytes(&dataLock.Data[y * lineSize + (x * 4)]);
                }
            }

            var texture = new Texture2D();
            texture.LoadData(textureData);
            texture.WrapS = WrapMode.Repeat;
            texture.WrapT = WrapMode.Repeat;
            texture.MagFilter = ScaleFilter.Nearest;
            texture.MinFilter = ScaleFilter.Nearest;

            return texture;
        }
    }
}
