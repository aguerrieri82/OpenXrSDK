using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Audio
{
    public abstract class  AlObject
    {
        protected AL _al;
        protected uint _handle;

        public AlObject(AL al, uint handle)
        {
            _al = al;

            _handle = handle;
        }
    }
}
