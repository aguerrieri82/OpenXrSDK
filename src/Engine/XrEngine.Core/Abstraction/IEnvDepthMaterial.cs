﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public interface IEnvDepthMaterial : IMaterial
    {
        bool UseEnvDepth { get; set; }
    }
}
