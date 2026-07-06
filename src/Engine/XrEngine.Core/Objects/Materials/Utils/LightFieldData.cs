using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrMath;

namespace XrEngine
{
    public class LightFieldData
    {
        public LightFieldData()
        {
            Strength = 2;
            UseAllFaces = true;
        }

        public IList<Texture3D>? Textures;

        public Vector3 Origin;

        public Vector3I Size;

        public float VoxelSize;

        public float Strength;

        public bool UseAllFaces;

        public bool DirPacked;
    }
}
