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
            DiffuseStrength = 1;
            SpecularStrength = 1;
            UseAllFaces = true;
        }

        public IList<Texture3D>? Textures;

        public Vector3 Origin;

        public Vector3I Size;

        public float VoxelSize;

        public float DiffuseStrength;

        public float SpecularStrength;

        public bool UseAllFaces;

    }
}
