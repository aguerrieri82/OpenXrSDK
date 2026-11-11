using XrEngine;

namespace XrSamples.Graffiti
{
    public class CapSelector : SelectorRing
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

        public CapSelector()
        {
            ButtonScale = 1;
            Caps = SprayCap.Presets;
            _activeIndex = Caps.IndexOf(SprayCap.Stock);
        }

        protected override Object3D CreateButton(int index) => CapModel.Create(MODELS[Caps[index]]);

        public SprayCap ActiveCap => Caps[_activeIndex];

        public IList<SprayCap> Caps { get; set; }

        public override int Count => Caps.Count;
    }
}
