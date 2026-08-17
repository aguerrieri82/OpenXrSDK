using System.Diagnostics;

namespace XrEngine.Objects
{
    public class SkinnedGeometry3D : Geometry3D, IVertexAttributes
    {
        private SkinData[] _skin;

        public SkinnedGeometry3D()
        {
            _skin = [];
        }

        public SkinData[] Skin
        {
            get => _skin;
            set => _skin = value;
        }

        public override void NotifyLoaded()
        {
            base.NotifyLoaded();

            if (this.Is(EngineObjectFlags.GpuOnly))
                _skin = [];
        }

        int IVertexAttributes.BufferCount => 1;

        Array IVertexAttributes.GetBuffer(int index)
        {
            Debug.Assert(index == 0);

            return _skin;
        }
    }
}
