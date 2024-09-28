using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public interface IShadowMaterial
    {
        bool ReceiveShadows { get; set; }

        Color ShadowColor { get; set; }
    }
}
