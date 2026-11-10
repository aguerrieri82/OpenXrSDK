using XrEngine;

namespace XrSamples.Graffiti
{
    public class CanvasState
    {
        public required TextureData Wet { get; init; }

        public required TextureData Dry { get; init; }

        public TextureData? Color { get; init; }
    }
}
