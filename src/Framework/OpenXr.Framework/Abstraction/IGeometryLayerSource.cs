using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public interface IGeometryLayerSource
    {
        void Initialize(XrApp app, IList<string> extensions);

        SwapchainSubImage Create();

        bool Update(long predTime);

        void OnBeginFrame(Space space, long displayTime);

        void OnEndFrame();

        void Destroy();
    }
}
