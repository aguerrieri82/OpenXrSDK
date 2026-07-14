using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.OpenGL
{
    public interface IGlContextProvider
    {
        IGlContext CreateShared();

        IGlContext? Current { get; }
    }
}
