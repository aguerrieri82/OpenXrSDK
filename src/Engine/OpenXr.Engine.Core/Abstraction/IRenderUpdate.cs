﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public interface IRenderUpdate
    {
        void Update(RenderContext ctx);
    }
}
