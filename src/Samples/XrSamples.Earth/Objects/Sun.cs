﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class Sun : Planet
    {
        public Sun()
        {
            Name = "Sun";
            SphereRadius = Unit(696340 * UniversePlanetScale);
            BaseColor = "#C26700FF";

            Create();

            var pbr = (IPbrMaterial)_sphere!.Materials[0];
            pbr.EmissiveColor = new Color(1, 1, 0.5f);
        }
    }
}
