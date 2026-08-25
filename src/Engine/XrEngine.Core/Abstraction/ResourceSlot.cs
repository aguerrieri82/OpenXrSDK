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
        Uniform,
        UniformBuffer,
        StorageBuffer,
        Attribute,
        FragmentOutput,
        Image
    }

    public readonly struct ResourceSlot
    {
        public ResourceSlot(int slot, string name)
        {
            Slot = slot;
            SlotName = $"{name.ToUpper()}_SLOT";
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
#if DEBUG
            if (r.Slot == -1)
                throw new InvalidOperationException($"Resource slot '{r.SlotName}' is not resolved");
#endif
            return r.Slot;
        }

        public static implicit operator uint(ResourceSlot r)
        {
            return (uint)(int)r;
        }

        public static implicit operator ResourceSlot(int i)
        {
            return new ResourceSlot(i);
        }

        public readonly int Slot;

        public readonly string? SlotName;

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
