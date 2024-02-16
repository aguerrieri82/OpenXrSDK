using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public enum AlphaMode
    {
        Opaque,
        Mask,
        Blend
    }

    public enum PbrMaterialType
    {
        Unlit,
        Specular,
        Metallic
    }

    public class PbrMetallicRoughness
    {
        public Color BaseColorFactor { get; set; }

        public float MetallicFactor { get; set; }

        public float RoughnessFactor { get; set; }

        public Texture2D? BaseColorTexture { get; set; }

        public Texture2D? MetallicRoughnessTexture { get; set; }  
    }

    public class PbrMaterial : Material
    {
        public PbrMaterial()
        {
            AlphaMode = AlphaMode.Opaque;
            Type = PbrMaterialType.Unlit;
            AlphaCutoff = 0.5f;

        }

        public static PbrMaterial CreateDefault()
        {
            return new PbrMaterial()
            {
                Type = PbrMaterialType.Metallic,
                Name = "Default Material",
                MetallicRoughness = new PbrMetallicRoughness
                {
                    BaseColorFactor = new Color(1, 1, 1, 1),
                    MetallicFactor = 1,
                    RoughnessFactor = 1
                }
            };
        }

        public PbrMetallicRoughness? MetallicRoughness { get; set; }

        public Texture2D? NormalTexture { get; set; }

        public Texture2D? OcclusionTexture { get; set; }

        public Texture2D? EmissiveTexture { get; set; }

        public Vector3 EmissiveFactor { get; set; }

        public float AlphaCutoff {  get; set; } 

        public AlphaMode AlphaMode { get; set; }

        public bool HasClearcoat { get; set; }

        public bool HasSheen { get; set; }

        public bool HasTransmission { get; set; }

        public bool HasIOR { get; set; }

        public bool HasSpecular { get; set; }


        public bool HasEmissiveStrength { get; set; }

        public bool HasVolume { get; set; }

        public bool HasIridescence { get; set; }

        public bool HasAnisotropy { get; set; }

        public PbrMaterialType Type { get; set; }   


    }
}
