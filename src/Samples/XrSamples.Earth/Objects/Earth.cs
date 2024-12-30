using System.Numerics;
using XrEngine;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class Earth : Planet
    {
        public Earth()
        {
            SphereRadius = Unit(6371.0f);
            AxisTilt = (float)DegreesToRadians(-23.5f);
            BaseColor = Color.White;
            AtmosphereColor = "#005FFF09";
            AtmosphereHeight = Unit(40);
            SubLevels = 6;
            Orbit = Orbit.Earth();

            Albedo = AssetLoader.Instance.Load<Texture2D>("res://asset/world.topo.bathy.200411.3x21600x10800.jpg");
            Albedo.WrapS = WrapMode.Repeat;
            Albedo.WrapT = WrapMode.Repeat;
            Albedo.Format = TextureFormat.SBgra32;
            Albedo.MinFilter = ScaleFilter.LinearMipmapLinear;
            Albedo.MipLevelCount = 20;

            HeightMap = new HeightMapSettings
            {
                Texture = AssetLoader.Instance.Load<Texture2D>("res://asset/gebco_08_rev_elev_21600x10800.png"),
                ScaleFactor = Unit(6.4f),
                NormalStrength = new Vector3(-10, -10, 10),
                NormalMode = HeightNormalMode.Sobel,
                SphereRadius = SphereRadius,
                TargetTriSize = 5
            };

            HeightMap.Texture.WrapS = WrapMode.Repeat;
            HeightMap.Texture.WrapT = WrapMode.Repeat;
            HeightMap.Texture.MinFilter = ScaleFilter.Linear;
            HeightMap.Texture.MagFilter = ScaleFilter.Linear;
            HeightMap.Texture.MipLevelCount = 20;
            HeightMap.Texture.MinFilter = ScaleFilter.LinearMipmapLinear;

            Create();

        }

    }
}
