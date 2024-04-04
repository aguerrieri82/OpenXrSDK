using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public class LogScale : ValueScale
    {
        public LogScale()
        {
            ScaleMin = ValueToScale(0.001f);
            ScaleMax = ValueToScale(1000f);
            ScaleStep = 0.01f; 
            ScaleSmallStep = 0.01f;

        }

        public override string? Format(float scaleValue)
        {
            return Math.Round(ScaleToValue(scaleValue), DecimalDigits).ToString();
        }

        public override float ScaleToValue(float scaleValue)
        {
            return MathF.Pow(10, scaleValue);
        }

        public override float ValueToScale(float value)
        {
            return MathF.Log10(value);
        }



    }
}
