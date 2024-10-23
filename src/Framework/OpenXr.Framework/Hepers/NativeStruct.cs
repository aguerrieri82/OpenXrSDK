using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenXr.Framework
{
    public unsafe struct NativeStruct<T> : IDisposable where T : unmanaged
    {
        private T* _value;

        public NativeStruct()
        {
            _value = null;
        }

        public ref T ValueRef => ref *_value;

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

        public readonly T* Pointer => _value;

        public static implicit operator T?(NativeStruct<T> value)
        {
            if (value.Pointer == null)
                return null;
            return *value.Pointer;
        }

        public void Dispose()
        {
            if (_value != null)
            {
                Marshal.FreeHGlobal(new nint(_value));
                _value = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
