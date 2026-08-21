using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface IRefractionMaterial : IMaterial
    {

       bool HasRefraction { get; }
    }
}
