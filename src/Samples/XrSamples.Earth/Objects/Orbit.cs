
using System.Numerics;
using XrEngine;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class Orbit
    {
        public Orbit()
        {
            OrbitScale = Vector3.One;
        }

        public LineMesh CreateGeometry(Color color, float stepSize = 0.01f)
        {
            var vertices = new List<Vector3>();

            // Note: angle is in radians, from 0 to 2π
            for (double angle = 0; angle < MathF.PI * 2; angle += stepSize)
            {
                // Both p1 and p2 are computed in radians
                var p1 = GetPosition(angle);
                var p2 = GetPosition((angle + stepSize) % (MathF.PI * 2));
                vertices.Add(p1);
                vertices.Add(p2);
            }

            var res = new LineMesh
            {
                Vertices = vertices.Select(a => new PointData
                {
                    Color = color,
                    Pos = a
                }).ToArray()
            };

            return res;
        }

        public Vector3 GetPosition(DateTime dateTime)
        {
            // Convert to Julian Date
            var julianDate = ToJulianDate(dateTime);

            // Days since J2000 epoch (2000-01-01 12:00 UT)
            var daysSinceJ2000 = julianDate - 2451545.0;

            // M (mean anomaly) in radians
            var M = MeanAnomalyAtEpoch + MeanMotion * daysSinceJ2000;
            M = NormalizeAngle(M); // keep in [0..2π)

            // Pass the mean anomaly (in radians) to our other GetPosition
            return GetPosition(M);
        }

        public Vector3 GetPosition(double M)
        {
            var E = SolveKeplerEquation(M, Eccentricity);

            // 2) True anomaly ν
            var nu = 2.0 * Math.Atan2(
                Math.Sqrt(1 + Eccentricity) * Math.Sin(E / 2.0),
                Math.Sqrt(1 - Eccentricity) * Math.Cos(E / 2.0)
            );

            // 3) Orbital radius r
            var r = SemiMajorAxis * (1 - Eccentricity * Math.Cos(E));

            // 4) (xOrb, yOrb) in the orbital plane
            var xOrb = r * Math.Cos(nu);
            var yOrb = r * Math.Sin(nu);

            // ----------- 3D ORBIT ORIENTATIONS -------------
            // a) Rotate by argument of perihelion ω around Z
            var cosw = Math.Cos(ArgumentOfPerihelion);
            var sinw = Math.Sin(ArgumentOfPerihelion);

            var xw = xOrb * cosw - yOrb * sinw;
            var yw = xOrb * sinw + yOrb * cosw;
            var zw = 0.0;

            // b) Inclination i around X
            var cosi = Math.Cos(Inclination);
            var sini = Math.Sin(Inclination);

            var xi = xw;
            var yi = yw * cosi;
            var zi = yw * sini;  // tilt out of the X–Z plane

            // c) Longitude of ascending node Ω around Z
            var cosO = Math.Cos(LongitudeOfAscendingNode);
            var sinO = Math.Sin(LongitudeOfAscendingNode);

            var xEcl = xi * cosO - yi * sinO;
            var yEcl = xi * sinO + yi * cosO;
            var zEcl = zi;

            // ----- Remap final coords so Y is 'up' and orbit is in X–Z if i=0 -----
            // xEcl -> X
            // yEcl -> Z
            // zEcl -> Y
            var final = new Vector3(
                (float)xEcl,
                (float)zEcl,
                (float)yEcl
            ) * (AU * OrbitScale);

            // Optional extra rotation about +Y for your scene
            if (Math.Abs(OrbitOffset) > 1e-9)
            {

                var orbitNormal = new Vector3(
                    (float)(Math.Sin(Inclination) * Math.Sin(LongitudeOfAscendingNode)),
                    (float)(Math.Cos(Inclination)),
                    (float)(-Math.Sin(Inclination) * Math.Cos(LongitudeOfAscendingNode))
                );

                final = final.Transform(
                    Quaternion.CreateFromAxisAngle(orbitNormal, OrbitOffset)
                );
            }

            return final;
        }

        // Keep angles in the range [0..2π)
        static double NormalizeAngle(double angleRadians)
        {
            return angleRadians - (2.0 * Math.PI) * Math.Floor(angleRadians / (2.0 * Math.PI));
        }

        // Solve M = E - e*sin(E) for E in radians, via simple Newton iteration
        static double SolveKeplerEquation(double M, double e)
        {
            var E = M; // initial guess
            double delta;
            do
            {
                delta = E - e * Math.Sin(E) - M;
                E -= delta / (1 - e * Math.Cos(E));
            }
            while (Math.Abs(delta) > 1e-6);

            return E; // in radians
        }

        // Example: Earth’s orbital parameters in radians
        public static Orbit Earth()
        {
            return new Orbit
            {
                Eccentricity = 0.0167,
                SemiMajorAxis = 1.0,
                MeanAnomalyAtEpoch = DegreesToRadians(357.529),
                MeanMotion = DegreesToRadians(0.985608),
                ArgumentOfPerihelion = DegreesToRadians(102.937),
                OrbitScale = new Vector3(1, 1, -1)
            };
        }

        public static Orbit Moon()
        {
            return new Orbit
            {
                Eccentricity = 0.0549,
                SemiMajorAxis = Unit(384400f * UniverseOrbitScale) / AU,
                MeanAnomalyAtEpoch = DegreesToRadians(115.3654),
                MeanMotion = DegreesToRadians(13.176358),
                ArgumentOfPerihelion = DegreesToRadians(318.15),
                Inclination = DegreesToRadians(5.145),
                LongitudeOfAscendingNode = DegreesToRadians(125.08),
                OrbitScale = new Vector3(1, 1, -1),
                OrbitOffset = (float)DegreesToRadians(0),
            };
        }

        public double LongitudeOfAscendingNode { get; set; }

        public double Inclination { get; set; }

        public double Eccentricity { get; set; }

        public double SemiMajorAxis { get; set; }


        public double MeanAnomalyAtEpoch { get; set; }


        public double MeanMotion { get; set; }


        public double ArgumentOfPerihelion { get; set; }

        public Vector3 OrbitScale { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float OrbitOffset { get; set; }

    }
}