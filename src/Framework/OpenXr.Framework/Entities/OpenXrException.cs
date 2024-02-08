using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public class OpenXrException : Exception
    {
        public OpenXrException(Result result, string message)
            : base($"OpenXr error: {result} ({message})") 
        { 
            Result = result;    
        }


        public Result Result { get; }
    }
}
