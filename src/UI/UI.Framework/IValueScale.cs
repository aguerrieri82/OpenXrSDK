using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public interface IValueScale
    {
        float ToScale(float value);

        float FromScale(float scaleValue);   
    }
}
