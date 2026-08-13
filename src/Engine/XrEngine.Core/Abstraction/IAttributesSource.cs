using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface IAttributesSource
    {
        int BufferCount { get; }

        Array GetBuffer(int index);
    }
}
