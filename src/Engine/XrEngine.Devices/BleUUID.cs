using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Devices
{
    public struct BleUUID
    {

        public static Guid FromInt(uint value)
        {
            return new Guid(value, (ushort)0x0000, (ushort)0x1000, (byte)0x80, (byte)0x00, (byte)0x00, (byte)0x80, (byte)0x5F, (byte)0x9B, (byte)0x34, (byte)0xFB);
        }
    }
}
