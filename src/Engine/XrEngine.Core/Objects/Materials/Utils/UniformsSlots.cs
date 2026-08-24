namespace XrEngine
{
    public static class UniformsSlots
    {
        public static readonly ResourceSlot Camera = new(0, nameof(Camera));
        public static readonly ResourceSlot Lights = new(1, nameof(Lights));
        public static readonly ResourceSlot Material = new(2, nameof(Material));
        public static readonly ResourceSlot Model = new(3, nameof(Model));
        public static readonly ResourceSlot Ibl = new(4, nameof(Ibl));
        public static readonly ResourceSlot Volume = new(5, nameof(Volume));
        public static readonly ResourceSlot Morph = new(6, nameof(Morph));
        public static readonly ResourceSlot VertexTransform = new(7, nameof(VertexTransform));
        public static readonly ResourceSlot Iridescence = new(8, nameof(Iridescence));
        public static readonly ResourceSlot MultiView = new(10, nameof(MultiView));

        public static readonly SlotMask Reserved = ResourceSlot.FillMask(typeof(UniformsSlots));
    }
}