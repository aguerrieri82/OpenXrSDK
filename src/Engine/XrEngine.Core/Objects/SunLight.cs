﻿namespace XrEngine
{
    public class SunLight : DirectionalLight
    {
        public SunLight()
        {
            HaloSize = 10;
            HaloFallOff = 80;
            SunRadius = 1.9f;
        }

        public float HaloSize { get; set; }
        public float HaloFallOff { get; set; }
        public float SunRadius { get; set; }

    }
}