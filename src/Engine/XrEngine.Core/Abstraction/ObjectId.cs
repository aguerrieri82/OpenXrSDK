using System.Diagnostics.CodeAnalysis;

namespace XrEngine
{
    public struct ObjectId : IEquatable<ObjectId>
    {

        public static ObjectId New()
        {
            return new ObjectId() { Value = Guid.NewGuid() };
        }


        public override readonly int GetHashCode()
        {
            return Value.GetHashCode();
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

        public static implicit operator Guid(ObjectId obj)
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

        public Guid Value;
    }
}
