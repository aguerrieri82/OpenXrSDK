using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Resources;
using System.Text;

namespace XrEngine
{
    public enum ResourceSlotType
    {
        Texture,
        Sampler,
        UniformBuffer,
        Buffer
    }

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

        public override string ToString()
        {
            return Convert.ToString(unchecked((long)Value), 2).PadLeft(64, '0');
        }

        public static implicit operator ulong(SlotMask mask) => mask.Value;

        public static implicit operator SlotMask(ulong value) => new() { Value = value };


        public ulong Value;

        public readonly int Max;

        public static readonly SlotMask Empty = new();
    }

    public readonly struct ResourceSlot
    {
        public ResourceSlot(int slot, string name)
        {
            Slot = slot;
            Name = $"{name.ToUpper()}_SLOT";
        }

        public ResourceSlot(int slot)
        {
            Slot = slot;
        }

        public ResourceSlot(string name)
            : this(-1, name)
        {
        }

        public static implicit operator int (ResourceSlot r)
        {
            return r.Slot;
        }

        public static implicit operator uint(ResourceSlot r)
        {
            return (uint)r.Slot;
        }

        public static implicit operator ResourceSlot(int i)
        {
            return new ResourceSlot(i);
        }

        public readonly int Slot;

        public readonly string? Name;

        public static IEnumerable<ResourceSlot> Enumerate(Type source)
        {
            foreach (var field in source.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(ResourceSlot) && field.IsInitOnly)
                    yield return (ResourceSlot)field.GetValue(null)!;
            }
        }

        public static SlotMask FillMask(Type source)
        {
            SlotMask mask = default;

            foreach (var slot in Enumerate(source))
            {
                if (slot.Slot != -1)
                    mask.Add(slot.Slot);
            }

            return mask;
        }
    }
}
