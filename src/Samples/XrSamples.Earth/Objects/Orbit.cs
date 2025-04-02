
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
            double julianDate = ToJulianDate(dateTime);

            // Days since J2000 epoch (2000-01-01 12:00 UT)
            double daysSinceJ2000 = julianDate - 2451545.0;

            // M (mean anomaly) in radians
            double M = MeanAnomalyAtEpoch + MeanMotion * daysSinceJ2000;
            M = NormalizeAngle(M); // keep in [0..2π)

            // Pass the mean anomaly (in radians) to our other GetPosition
            return GetPosition(M);
        }

        public Vector3 GetPosition(double M)
        {
            double E = SolveKeplerEquation(M, Eccentricity);

            // 2) True anomaly ν
            double nu = 2.0 * Math.Atan2(
                Math.Sqrt(1 + Eccentricity) * Math.Sin(E / 2.0),
                Math.Sqrt(1 - Eccentricity) * Math.Cos(E / 2.0)
            );

            // 3) Orbital radius r
            double r = SemiMajorAxis * (1 - Eccentricity * Math.Cos(E));

            // 4) (xOrb, yOrb) in the orbital plane
            double xOrb = r * Math.Cos(nu);
            double yOrb = r * Math.Sin(nu);

            // ----------- 3D ORBIT ORIENTATIONS -------------
            // a) Rotate by argument of perihelion ω around Z
            double cosw = Math.Cos(ArgumentOfPerihelion);
            double sinw = Math.Sin(ArgumentOfPerihelion);

            double xw = xOrb * cosw - yOrb * sinw;
            double yw = xOrb * sinw + yOrb * cosw;
            double zw = 0.0;

            // b) Inclination i around X
            double cosi = Math.Cos(Inclination);
            double sini = Math.Sin(Inclination);

            double xi = xw;
            double yi = yw * cosi;
            double zi = yw * sini;  // tilt out of the X–Z plane

            // c) Longitude of ascending node Ω around Z
            double cosO = Math.Cos(LongitudeOfAscendingNode);
            double sinO = Math.Sin(LongitudeOfAscendingNode);

            double xEcl = xi * cosO - yi * sinO;
            double yEcl = xi * sinO + yi * cosO;
            double zEcl = zi;

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
            double E = M; // initial guess
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