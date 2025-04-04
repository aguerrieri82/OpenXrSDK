﻿using System.Numerics;

namespace XrEngine
{
    public enum HeightNormalMode
    {
        Fast,
        Sobel,
        Geometry
    }

    public class HeightMapSettings : TessellationSettings
    {
        public float ScaleFactor { get; set; }

        public float SphereRadius { get; set; }

        public Vector3 SphereWorldCenter { get; set; }

        public Vector3 NormalStrength { get; set; }

        public HeightNormalMode NormalMode { get; set; }

        public Texture2D? Texture { get; set; }

        public float? MaskValue { get; set; }
    }
}
