using Silk.NET.OpenXR;

namespace OpenXr.Framework
{

    public delegate XrQuad GetQuadDelegate();


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

            if (!quad.IsVisible)
                return false;

            layer.Size.Width = quad.Size.X;
            layer.Size.Height = quad.Size.Y;

            layer.Pose.Position = quad.Position.Convert().To<Vector3f>();
            layer.Pose.Orientation = quad.Orientation.Convert().To<Quaternionf>();

            return true;
        }
    }
}
