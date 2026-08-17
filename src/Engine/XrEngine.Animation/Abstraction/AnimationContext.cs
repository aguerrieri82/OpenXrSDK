using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace XrEngine.Animation
{

    public class AnimationContext
    {
        public float Time;

        public float ReferenceTime;

    }

    public class AnimationContext<T> : AnimationContext
    {
        [AllowNull]
        public T StartValue;

        [AllowNull]
        public T EndValue;
    }
}
