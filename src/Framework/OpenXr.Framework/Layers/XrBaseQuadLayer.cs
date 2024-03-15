using Silk.NET.OpenXR;
using XrMath;

namespace OpenXr.Framework
{

    public delegate Quad3 GetQuadDelegate();



    public abstract class XrBaseQuadLayer : XrBaseLayer<CompositionLayerQuad>
    {
        protected Swapchain _swapchain;
        protected GetQuadDelegate _getQuad;


        public unsafe XrBaseQuadLayer(GetQuadDelegate getQuad)
        {
            _getQuad = getQuad;
            _header->Type = StructureType.CompositionLayerQuad;

        }


        protected override bool Update(ref CompositionLayerQuad layer, ref View[] views, long predTime)
        {
            var quad = _getQuad();

            layer.Size.Width = quad.Size.X;
            layer.Size.Height = quad.Size.Y;
            layer.Pose = quad.Pose.ToPoseF();

            return true;
        }

        public override void Destroy()
        {
            if (_swapchain.Handle != 0)
            {
                _xrApp!.Xr.DestroySwapchain(_swapchain);
                _swapchain.Handle = 0;
            }
            base.Destroy();
        }

    }
}
