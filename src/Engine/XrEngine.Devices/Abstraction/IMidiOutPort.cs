using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Devices
{
    public interface IMidiOutPort
    {
        void Send(byte[] data, int offset, int count);

        void Close();
    }
}
