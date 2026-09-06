namespace XrEngine
{
    public static class BufferSlots
    {
        public static readonly ResourceSlot Instance = new(9, nameof(Instance));
        public static readonly ResourceSlot TexCutStyles = new(10, nameof(TexCutStyles));
        public static readonly ResourceSlot Skin = new(18, nameof(Skin));
        public static readonly ResourceSlot Splats = new(18, nameof(Splats));
        public static readonly ResourceSlot SkinMatrices = new(19, nameof(SkinMatrices));
        public static readonly ResourceSlot Morph = new(20, nameof(Morph));

        public static readonly SlotMask Reserved = ResourceSlot.FillMask(typeof(BufferSlots));
    }
}