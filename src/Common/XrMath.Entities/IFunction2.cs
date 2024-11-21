using System;
using System.Collections.Generic;
using System.Text;

namespace XrMath
{
    public interface IFunction2
    {
        float Value(float x);

        Bounds1 RangeX { get; }
    }
}
