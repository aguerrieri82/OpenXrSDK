using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace XrEngine.OpenXr
{
    public class XrPerformanceQuery : Behavior<Scene3D>
    {
        readonly Dictionary<string, float> _performances = [];
        private XrOculusPlugin? _oculus;

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

        protected override void Update(RenderContext ctx)
        {
            if (_oculus == null)
            {
                _oculus = XrApp.Current?.Plugin<XrOculusPlugin>();
                _oculus?.Performance.SetEnabled(true);
            }
            else
                _oculus.Performance.ReadAll(_performances);
        }


        public Dictionary<string, float> Performances => _performances;
    }
}
