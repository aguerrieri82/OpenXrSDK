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

    public static class CapModel
    {
        static Group3D? _source;

        public static Object3D Create(CapType type)
        {
            _source ??= AssetLoader.Instance.Load<Group3D>("res://asset/Caps.glb");

            return _source.FindByName<Object3D>("Cap_" + type)!.Clone(ObjectCloneFlags.CloneMaterials);
        }
    }
}
