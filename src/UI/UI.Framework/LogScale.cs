using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public class LogScale : IValueScale
    {
        LogScale() { }

        public float FromScale(float scaleValue)
        {
            return MathF.Pow(10, scaleValue);
        }

        public float ToScale(float value)
        {
            return MathF.Log10(value);
        }


        public static readonly LogScale Instance = new LogScale();  
    }
}
