using System.Buffers.Binary;
using System.IO.Hashing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.Helpers
{
    public class HashBuilder
    {
        [ThreadStatic]
        static HashBuilder? _instance;

        byte[] _buffer = new byte[256];

        private readonly XxHash3 _hash = new();

        public void Add<T>(T value) where T : unmanaged
        {
            _hash.Append(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1)));
        }

        public void Add(string? value)
        {
            if (value == null)
                return;

            var byteCount = Encoding.UTF8.GetByteCount(value);
            var requiredSize = sizeof(int) + byteCount;

            if (_buffer.Length < requiredSize)
                Array.Resize(ref _buffer, requiredSize);

            BinaryPrimitives.WriteInt32LittleEndian(_buffer, byteCount);
            Encoding.UTF8.GetBytes(value, _buffer.AsSpan(sizeof(int), byteCount));
            _hash.Append(_buffer.AsSpan(0, requiredSize));
        }

        public ulong Compute(IReadOnlySet<Guid> values)
        {
            _hash.Reset();

            foreach (var value in values)
                Add(value);

            return _hash.GetCurrentHashAsUInt64();
        }

        public ulong Compute(ReadOnlySpan<byte> data)
        {
            _hash.Reset();
            _hash.Append(data);
            return _hash.GetCurrentHashAsUInt64();
        }

        public ulong Compute(string main, IReadOnlySet<string>? values = null)
        {
            _hash.Reset();

            Add(main);

            if (values != null)
            {
                foreach (var value in values)
                    Add(value);
            }

            return _hash.GetCurrentHashAsUInt64();
        }

        public void Reset()
        {
            _hash.Reset();
        }

        public void Add(Guid value)
        {
            value.TryWriteBytes(_buffer);
            _hash.Append(_buffer.AsSpan(0, 16));
        }

        public ulong Value()
        {
            return _hash.GetCurrentHashAsUInt64();
        }

        public static HashBuilder Instance
        {
            get
            {
                _instance ??= new();
                return _instance;
            }
        }
    }
}
