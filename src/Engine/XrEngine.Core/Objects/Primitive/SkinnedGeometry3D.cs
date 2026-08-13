namespace XrEngine.Objects
{
    public class SkinnedGeometry3D : Geometry3D, IAttributesSource
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

        int IAttributesSource.BufferCount => 1;

        Array IAttributesSource.GetBuffer(int index)
        {
            return _skin;
        }
    }
}
