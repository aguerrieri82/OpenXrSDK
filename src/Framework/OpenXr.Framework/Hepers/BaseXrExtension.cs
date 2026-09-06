using Silk.NET.Core;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenXr.Framework
{
    public abstract class BaseXrExtension
    {

        public BaseXrExtension(XR xr, Instance instance)
        {
            var fun = new PfnVoidFunction();

            foreach (var prop in GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var propType = prop.FieldType;
                if (!propType.IsSubclassOf(typeof(Delegate)))
                    continue;

                var name = "xr" + prop.Name;

                var res = xr.GetInstanceProcAddr(instance, name, ref fun);
                if (res != Result.Success)
                    throw new NotSupportedException(name);

                prop.SetValue(this, Marshal.GetDelegateForFunctionPointer(fun, propType));
            }
        }
    }
}
