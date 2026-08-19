using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public interface IAnimationValueHandler<T>
    {
        T Interpolate(T start, T end, float t);

        T Combine(T value, T offset);

        T Remove(T value, T offset);
    }
}
