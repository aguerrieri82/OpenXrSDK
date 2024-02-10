using System.Runtime.InteropServices;

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
            set
            {

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
