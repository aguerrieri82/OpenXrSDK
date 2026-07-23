using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Text;

namespace XrEngine.Helpers
{
    public class HashBuilder
    {
        [ThreadStatic]
        static HashBuilder? _instance;

        byte[] _buffer = new byte[256];

        private readonly XxHash3 _hash = new();

        public void Add(string value)
        {
            var byteCount = Encoding.UTF8.GetByteCount(value);
            var requiredSize = sizeof(int) + byteCount;

            if (_buffer.Length < requiredSize)
                Array.Resize(ref _buffer, requiredSize);

            BinaryPrimitives.WriteInt32LittleEndian(_buffer, byteCount);
            Encoding.UTF8.GetBytes(value, _buffer.AsSpan(sizeof(int), byteCount));
            _hash.Append(_buffer.AsSpan(0, requiredSize));
        }

        public ulong Compute(IReadOnlyList<Guid> values)
        {
            _hash.Reset();

            for (int i = 0; i < values.Count; i++)
                Add(values[i]);

            return _hash.GetCurrentHashAsUInt64();
        }

        public ulong Compute(ReadOnlySpan<byte> data)
        {
            _hash.Reset();
            _hash.Append(data);
            return _hash.GetCurrentHashAsUInt64();
        }

        public ulong Compute(string main, IReadOnlyList<string>? values = null)
        {
            _hash.Reset();

            Add(main);

            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                    Add(values[i]);
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
