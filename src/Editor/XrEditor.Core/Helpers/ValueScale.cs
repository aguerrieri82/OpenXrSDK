using CanvasUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public class ValueScale : IValueScale
    {
        public float ScaleMin { get; set; }

        public float ScaleMax { get; set; }

        public float ScaleStep { get; set; }

        public float ScaleSmallStep { get; set; }

        public string? Format(float scaleValue)
        {
            return scaleValue.ToString();
        }

        public virtual float ScaleToValue(float scaleValue)
        {
            return scaleValue;
        }

        public virtual float ValueToScale(float value)
        {
            return value;
        }
    }
}
