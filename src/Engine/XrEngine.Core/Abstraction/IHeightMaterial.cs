using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public interface IHeightMaterial : IMaterial
    {
        float HeightTessFactor { get; set; }

        float HeightScale { get; set; }

        Texture2D? HeightMap { get; set; }
    }
}
