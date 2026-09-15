using Silk.NET.OpenXR;
using XrMath;

namespace OpenXr.Framework.Oculus
{

    public struct XrPassthroughStyle
    {
        public XrPassthroughStyle()
        {
            Opacity = 1f;
            EdgeColor = Color.Transparent;
            LutWeight = 1f;
        }

        public float Opacity;
        public Color EdgeColor;

        public XrPassthroughBcs? Bcs;
        public byte[]? MonoMap;
        public Color4f[]? ColorMap;

        public float LutWeight;
        public PassthroughColorLutMETA? SourceLut;
        public PassthroughColorLutMETA? TargetLut;
    }

    public struct XrPassthroughBcs
    {
        public float Brightness;
        public float Contrast;
        public float Saturation;
    }

}
