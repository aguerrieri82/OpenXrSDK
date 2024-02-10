using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public enum ApiType
    {
        OpenGLES
    }

    public interface IApiProvider
    {
        T GetApi<T>() where T : class;   
    }
}
