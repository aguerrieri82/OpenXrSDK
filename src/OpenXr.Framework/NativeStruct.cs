using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public unsafe struct NativeStruct<T> : IDisposable where T : struct
    {
        private T* _value;

        public NativeStruct()
        {
            _value = null;
        }

        public T? Value
        {
            get => _value == null ? null : *_value;
            set {

                if (value == null)
                    Dispose();
                else
                {
                    if (_value == null)
                        _value = (T*)Marshal.AllocHGlobal(sizeof(T));
                    *_value = value.Value;
                }
            }

        }

        public T* Pointer => _value;

        public void Dispose()
        {
            if (_value != null)
            {
                Marshal.FreeHGlobal(new nint(_value));
                _value = null;
            }
        }
    }
}
