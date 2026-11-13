using XrEngine;

namespace XrSamples.Graffiti
{
    public enum CapType
    {
        SuperSkinny,
        Mtn94,
        Banana,
        Soft,
        FatPink,
        AstroFat,
        NY,
        Transversal,
        Needle
    }

    public static class CapFactory
    {
        static readonly Dictionary<SprayCap, CapType> MODELS = new()
        {
            [SprayCap.ExtraSkinny] = CapType.SuperSkinny,
            [SprayCap.Skinny] = CapType.Banana,
            [SprayCap.SoftSkinny] = CapType.Soft,
            [SprayCap.Stock] = CapType.Mtn94,
            [SprayCap.Fat] = CapType.FatPink,
            [SprayCap.SuperFat] = CapType.AstroFat,
            [SprayCap.Calligraphy] = CapType.Transversal
        };

        static Group3D? _source;

        public static Object3D Create(SprayCap cap)
        {
            _source ??= AssetLoader.Instance.Load<Group3D>("res://asset/Caps.glb");

            return _source.FindByName<Object3D>("Cap_" + MODELS[cap])!.Clone(ObjectCloneFlags.CloneMaterials);
        }
    }
}
