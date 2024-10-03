using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public static class MaterialFactory
    {
        public static IPbrMaterial CreatePbr(Color color)
        {
            var result = CreatePbr<PbrV2Material>();
            result.Color = color;
            return result;
        }

        public static T CreatePbr<T>() where T: IPbrMaterial, new()
        {
            return new T();
        }


        public static Type DefaultPbr => typeof(PbrV2Material); 
    }
}
