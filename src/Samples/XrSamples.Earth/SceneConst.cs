using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrSamples.Earth
{
    public static class SceneConst
    {
        public static float Unit(float value)
        {
            return value / 1000f;
        }

        public static Vector3 Unit(Vector3 value)
        {
            return new Vector3(Unit(value.X), Unit(value.Y), Unit(value.Z));    
        }


        public static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public static Vector2 NormalToUV(Vector3 normal)
        {
            // Compute longitude (λ) and latitude (φ) from the normalized vector
            var longitude = Math.Atan2(normal.Z, normal.X); // Range: -π to π
            var latitude = Math.Asin(normal.Y);            // Range: -π/2 to π/2

            // Convert longitude to UV's u (range: 0 to 1)
            var u = (float)((longitude / (2 * Math.PI)) + 0.5);

            // Convert latitude to UV's v (range: 0 to 1)
            var v = (float)((latitude / Math.PI) + 0.5);

            // Return the UV coordinates
            return new Vector2(1 - u, 1 - v);
        }

        public static double ToJulianDate(DateTime utc)
        {
            //utc += TimeSpan.FromHours(5 - 0.5);

            // Adapted from astronomical formulas
            // integer day, month, year
            int year = utc.Year;
            int month = utc.Month;
            int day = utc.Day;

            // If Jan or Feb, treat them as months 13,14 of previous year
            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }

            int A = year / 100;
            int B = 2 - A + (A / 4);

            // integer day count
            double dayCount = Math.Floor(365.25 * (year + 4716))
                            + Math.Floor(30.6001 * (month + 1))
                            + day + B - 1524.5; // note the .5 offset to start day at noon

            // fraction of the day
            double fractionOfDay =
                (utc.Hour * 3600.0 + utc.Minute * 60.0 + utc.Second + utc.Millisecond / 1000.0)
                / 86400.0;

            return dayCount + fractionOfDay;
        }

        public const float UniverseOrbitScale = 1f;

        public const float UniversePlanetScale = 1f;

        public static readonly float AU = Unit(149597870.7f * UniverseOrbitScale);
    }
}
