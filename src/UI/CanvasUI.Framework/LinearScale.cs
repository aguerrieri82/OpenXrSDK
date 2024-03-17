using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public class LinearScale : IValueScale
    {
        LinearScale() { }

        public float FromScale(float scaleValue)
        {
            return scaleValue;
        }

        public float ToScale(float value)
        {
            return value;
        }

        public static readonly LinearScale Instance = new LinearScale();
    }
}
