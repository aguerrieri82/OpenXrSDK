﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public struct ObjectId : IEquatable<ObjectId>
    {
        static int _lastId = 1;


        public static ObjectId New()
        {
            return new ObjectId() { Value = _lastId++ };
        }

        public override readonly int GetHashCode()
        {
            return Value;
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

        public static implicit operator int (ObjectId obj)
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

        public int Value;
    }
}