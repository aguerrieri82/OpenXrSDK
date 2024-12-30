
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

        public Vector3 GetPosition(double angleRadians)
        {
            // 1) Solve Kepler’s Equation for the Eccentric Anomaly, E (in radians)
            double E = SolveKeplerEquation(angleRadians, Eccentricity);

            // 2) True Anomaly (ν), also in radians
            double trueAnomaly = 2.0 * Math.Atan2(
                Math.Sqrt(1 + Eccentricity) * Math.Sin(E / 2.0),
                Math.Sqrt(1 - Eccentricity) * Math.Cos(E / 2.0)
            );

            // 3) Orbital Radius (r)
            double r = SemiMajorAxis * (1 - Eccentricity * Math.Cos(E));

            // 4) Position in orbital plane (xOrbital, yOrbital)
            double xOrbital = r * Math.Cos(trueAnomaly);
            double yOrbital = r * Math.Sin(trueAnomaly);

            // 5) Rotate by ArgumentOfPerihelion (in radians) to get final coords
            double x = xOrbital * Math.Cos(ArgumentOfPerihelion)
                     - yOrbital * Math.Sin(ArgumentOfPerihelion);
            double y = xOrbital * Math.Sin(ArgumentOfPerihelion)
                     + yOrbital * Math.Cos(ArgumentOfPerihelion);

            // Return position in the XZ plane (with Y=0)
            // multiplied by AU to convert from "astronomical units" to your engine’s scale.
            return (new Vector3((float)x, 0f, (float)y) * OrbitScale * AU).Transform(Quaternion.CreateFromAxisAngle(Vector3.UnitY, RotationOffset));
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
                OrbitScale = new Vector3(1, 1,-1)
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
                ArgumentOfPerihelion = DegreesToRadians(318.15) ,
                RotationOffset = (float)DegreesToRadians(58)
            };
        }


        public double Eccentricity { get; set; }
        
        public double SemiMajorAxis { get; set; }


        public double MeanAnomalyAtEpoch { get; set; }


        public double MeanMotion { get; set; }


        public double ArgumentOfPerihelion { get; set; }

        public Vector3 OrbitScale { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float RotationOffset { get; set; }

    }
}