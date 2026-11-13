using XrEngine;

namespace XrSamples.Graffiti
{
    public class CapSelector : SelectorRing
    {
        public CapSelector()
        {
            ButtonScale = 1;
            Caps = SprayCap.Presets;
            _activeIndex = Caps.IndexOf(SprayCap.Stock);
        }

        protected override Object3D CreateButton(int index) => CapFactory.Create(Caps[index]);

        public SprayCap ActiveCap => Caps[_activeIndex];

        public IList<SprayCap> Caps { get; set; }

        public override int Count => Caps.Count;
    }
}
