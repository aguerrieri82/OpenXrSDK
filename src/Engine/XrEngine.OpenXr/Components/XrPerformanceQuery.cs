using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System.Diagnostics;

namespace XrEngine.OpenXr
{
    public class XrPerformanceQuery : BaseXrComponent<Scene3D>
    {
        protected readonly Dictionary<string, float> _performances = [];
        protected XrOculusPlugin? _oculus;

        public XrPerformanceQuery()
        {
        }

        protected override void OnDisabled()
        {
            _oculus?.Performance.SetEnabled(false);
        }

        protected override void OnEnabled()
        {
            _oculus?.Performance.SetEnabled(true);
        }

        protected override void AttachXr()
        {
            _oculus = _xrApp!.Plugin<XrOculusPlugin>();

            _oculus.Performance.SetEnabled(true);
        }

        protected override void UpdateWork(RenderContext ctx)
        {
            Debug.Assert(_oculus != null);

            _oculus.Performance.ReadAll(_performances);
        }

        public Dictionary<string, float> Performances => _performances;
    }
}
