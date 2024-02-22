﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
  

    public interface IShaderHandler
    {
        void UpdateShader(ShaderUpdateBuilder bld);
    }
}