﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public interface IFunctionView
    {
        void ShowDft(float[] data, uint sampleRate, uint size);
    }
}
