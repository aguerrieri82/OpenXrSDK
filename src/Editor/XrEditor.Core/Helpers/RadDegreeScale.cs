using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public class RadDegreeScale : IValueScale
    {
        RadDegreeScale() { }

        public string? Format(float scaleValue)
        {
            return $"{Math.Round( scaleValue, DecimalDigits)} °";
        }

        public float ScaleToValue(float value)
        {
            return value / 180f * MathF.PI;
        }

        public float ValueToScale(float value)
        {
            return value / MathF.PI * 180f;
        }

        public int DecimalDigits => 1;

        public float ScaleMin => -180;

        public float ScaleMax => 180;

        public float ScaleStep => 1f;

        public float ScaleSmallStep => 1f;


        public static readonly RadDegreeScale Instance = new();

    }
}
