using Silk.NET.OpenXR;
using Xr.Math;

namespace OpenXr.Framework
{

    public delegate Quad3 GetQuadDelegate();


    public class XrQuadLayer : XrBaseLayer<CompositionLayerQuad>
    {
        protected Swapchain _swapchain;
        protected GetQuadDelegate _getQuad;


        public unsafe XrQuadLayer(GetQuadDelegate getQuad)
        {
            _getQuad = getQuad;
            _header->Type = StructureType.CompositionLayerQuad;
        }


        protected override bool Update(ref CompositionLayerQuad layer, ref View[] views, XrSwapchainInfo[] swapchains, long predTime)
        {
            var quad = _getQuad();

            layer.Size.Width = quad.Size.X;
            layer.Size.Height = quad.Size.Y;
            layer.Pose = quad.Pose.ToPoseF();

            return true;
        }
    }
}
