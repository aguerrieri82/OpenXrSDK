using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public interface ITimeFunction
    {
        float Value(float t);
    }
}
