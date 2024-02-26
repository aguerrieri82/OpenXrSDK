using System.Diagnostics.CodeAnalysis;

namespace Xr.Engine
{
    public struct ObjectId : IEquatable<ObjectId>
    {
        static uint _lastId = 1;


        public static ObjectId New()
        {
            return new ObjectId() { Value = _lastId++ };
        }

        public override readonly int GetHashCode()
        {
            return (int)Value;
        }

        public readonly bool Equals(ObjectId other)
        {
            return Value == other.Value;
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is ObjectId other)
                return other.Value == Value;
            return false;
        }

        public static implicit operator uint(ObjectId obj)
        {
            return obj.Value;
        }

        public override readonly string ToString()
        {
            return Value.ToString();
        }


        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return left.Value != right.Value;
        }

        public uint Value;
    }
}
