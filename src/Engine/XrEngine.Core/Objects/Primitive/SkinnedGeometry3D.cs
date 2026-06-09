using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Objects
{
    public class SkinnedGeometry3D : Geometry3D
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
    }
}
