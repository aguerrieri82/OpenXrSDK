using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public enum TextureFormat
    { 
        Deph32Float
    }

    public enum WrapMode
    {
        ClampToEdge = 33071
    }

    public enum ScaleFilter
    {
        Nearest = 9728
    }


    public class Texture2D : Texture
    {
        public uint Width { get; set; }

        public uint Height { get; set; }

        public WrapMode WrapS { get; set; }

        public WrapMode WrapT { get; set; }

        public ScaleFilter MagFilter { get; set; }

        public ScaleFilter MinFilter { get; set; }

        public TextureFormat Format { get; set; }
    }
}
