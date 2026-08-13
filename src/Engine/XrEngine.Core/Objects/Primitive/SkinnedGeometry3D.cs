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

        int IVertexAttributes.BufferCount => 1;

        Array IVertexAttributes.GetBuffer(int index)
        {
            return _skin;
        }
    }
}
