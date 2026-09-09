using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.OpenXr
{
    public interface IAppBuilder
    {
        XrEngineApp Build(XrEngineAppBuilder builder); 
    }
}
