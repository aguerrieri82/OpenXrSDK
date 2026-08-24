using System.Numerics;

namespace XrEngine
{
    public struct SlotMask
    {
        public SlotMask(int max)
        {
            Max = max;
            Value = max == 64 ? 0 : ulong.MaxValue << max;
        }

        public readonly bool Has(int slot)
        {
            return (Value & (1UL << slot)) != 0;
        }

        public int Allocate(SlotMask reserved)
        {
            var free = ~Value;
            var preferred = free & ~reserved.Value;
            var available = preferred != 0 ? preferred : free;

            if (available == 0)
                throw new InvalidOperationException("No slots available");

            var result = BitOperations.TrailingZeroCount(available);

            Add(result);

            return result;
        }

        public void Add(int slot)
        {
            Value |= 1UL << slot;
        }

        public void Remove(int slot)
        {
            Value &= ~(1UL << slot);
        }

        public void Clear()
        {
            Value = 0;
        }

        public static SlotMask operator |(SlotMask mask, int slot)
        {
            mask.Add(slot);
            return mask;
        }

        public static SlotMask operator &(SlotMask mask, int slot)
        {
            mask.Value &= 1UL << slot;
            return mask;
        }

        public readonly override string ToString()
        {
            var max = Max == 0 ? 64 : Max;
            var value = max == 64 ? Value : Value & ((1UL << max) - 1);

            return Convert.ToString(unchecked((long)value), 2)
                  .PadLeft(max, '0');
        }

        public static implicit operator ulong(SlotMask mask) => mask.Value;

        public static implicit operator SlotMask(ulong value) => new() { Value = value };


        public ulong Value;

        public readonly int Max;

        public static readonly SlotMask Empty = new();
    }
}
