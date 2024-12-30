using System.Numerics;
using XrEngine;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class Moon : Planet
    {
        public Moon()
        {
            Name = "Moon";
            SphereRadius = Unit(1737.4f * UniversePlanetScale);
            AxisTilt = (float)DegreesToRadians(-1.54f);
            BaseColor = Color.White;
            SubLevels = 6;
            Orbit = Orbit.Moon();
            RotationOffset = (float)DegreesToRadians(250);
            Albedo = AssetLoader.Instance.Load<Texture2D>("res://asset/lroc_color_poles_hw5x3.tif");
            Albedo.WrapS = WrapMode.Repeat;
            Albedo.WrapT = WrapMode.Repeat;
            Albedo.Format = TextureFormat.SBgra32;
            Albedo.MinFilter = ScaleFilter.LinearMipmapLinear;
            Albedo.MipLevelCount = 20;

            HeightMap = new HeightMapSettings
            {
                Texture = AssetLoader.Instance.Load<Texture2D>("res://asset/ldem_hw5x3.tif"),
                ScaleFactor = 0.01f,
                NormalStrength = new Vector3(100, -100, 1),
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

        public override float RotationAngle(DateTime utcTime)
        {
            double julianDate = ToJulianDate(utcTime);

            // Days since J2000 epoch
            double daysSinceJ2000 = julianDate - 2451545.0;

            // Moon's mean longitude at J2000 (degrees)
            double meanLongitudeAtEpoch = 91.929336; // Reference value in degrees

            // Mean motion of the Moon (degrees per day)
            double meanMotion = 13.176358;

            // Compute the Moon's mean longitude (degrees)
            double meanLongitude = meanLongitudeAtEpoch + (meanMotion * daysSinceJ2000);
            meanLongitude %= 360.0; // Normalize to [0, 360)

            // Convert to radians and return
            return (float)DegreesToRadians(meanLongitude);
        }

    }
}
