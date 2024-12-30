
using System.ComponentModel;

using System.Numerics;

using XrEngine;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class CameraControl : Behavior<EarthScene>, INotifyPropertyChanged
    {
        DateTime _dateTime;
        bool _posDirty;


        public CameraControl()
        {
            _dateTime = DateTime.Now;
            Speed = 10;
            Latitude = 12.1518378f;
            Longitude = 43.570749f;
            Altitude = 30;
            Heading = 0;
            LockCamera = true;
            Offset = (float)DegreesToRadians(162);

            Time = "17:00";
            Altitude = 10000;

        }

        protected override void Start(RenderContext ctx)
        {
            UpdatePos();

            base.Start(ctx);
        }

        (Vector3 direction, Vector3 up) ComputeCameraDirectionAndUp(Vector3 cameraPosition, float azimuth, float elevation)
        {
            var localUp = Vector3.Normalize(cameraPosition);

            var worldNorthAxis = new Vector3(0, 1, 0)
                .Transform(_host!.Earth.Transform.Orientation)
                .Normalize();

            var localEast = Vector3.Cross(worldNorthAxis, localUp).Normalize();
            var localNorth = Vector3.Cross(localUp, localEast).Normalize();

            var cosE = MathF.Cos(elevation);
            var sinE = MathF.Sin(elevation);
            var cosA = MathF.Cos(azimuth);
            var sinA = MathF.Sin(azimuth);

            var dirHoriz = cosE * (cosA * localNorth + sinA * localEast);

            var cameraDir = Vector3.Normalize(dirHoriz + sinE * localUp);
            var cameraRight = Vector3.Cross(cameraDir, localUp).Normalize();
            var cameraUp = Vector3.Cross(cameraRight, cameraDir).Normalize();

            return (cameraDir, cameraUp);
        }

        Vector3 ComputePosition(Vector2 latLng, float altitude)
        {
            var lonRad = (latLng.X * MathF.PI / 180.0f);
            var latRad = (latLng.Y * MathF.PI / 180.0f);

            var radius = (_host!.Earth.SphereRadius + Unit(altitude));

            var x = radius * MathF.Cos(latRad) * MathF.Sin(lonRad);
            var y = radius * MathF.Sin(latRad);
            var z = radius * MathF.Cos(latRad) * MathF.Cos(lonRad);

            return new Vector3(x, y, z).Transform(_host.Earth.WorldMatrix);
        }

        public static float ComputeEarthRotationAngleV2(DateTime utcTime, bool solarDay)
        {
            // Convert to Julian Date
            double jd = ToJulianDate(utcTime);
            double d = jd - 2451545.0; // days since J2000

            // GMST (radians)
            double gmstHours = 18.697374558 + 24.06570982441908 * d;
            gmstHours = gmstHours % 24.0;
            if (gmstHours < 0) gmstHours += 24.0;
            double gmst = gmstHours * (Math.PI / 12.0);

            // Sun’s approximate Right Ascension (radians)
            double M = (357.529 + 0.98560028 * d) % 360.0;         // mean anomaly (deg)
            double L0 = (280.459 + 0.98564736 * d) % 360.0;         // mean longitude (deg)
            double Mrad = M * (Math.PI / 180.0);
            double L0rad = L0 * (Math.PI / 180.0);
            double lambda = (L0
                             + 1.915 * Math.Sin(Mrad) * (180.0 / Math.PI)
                             + 0.020 * Math.Sin(2 * Mrad) * (180.0 / Math.PI)) % 360.0;
            double lambdaRad = lambda * (Math.PI / 180.0);
            double eps = 23.439 * (Math.PI / 180.0);
            double alpha = Math.Atan2(Math.Sin(lambdaRad) * Math.Cos(eps),
                                      Math.Cos(lambdaRad));
            alpha = (alpha < 0) ? alpha + 2.0 * Math.PI : alpha;

            // Earth rotation = GMST - SunRA, normalized
            double rotation = gmst - alpha;
            rotation = rotation % (2.0 * Math.PI);
            if (rotation < 0) rotation += 2.0 * Math.PI;
            return (float)rotation;
        }

        public static float ComputeEarthRotationAngle(DateTime utcTime)
        {
            double julianDate = ToJulianDate(utcTime);

            // Adjust Julian Date for UT1 if needed
            double dUT1 = julianDate - 2451545.0;

            // Calculate Earth Rotation Angle (ERA) in radians
            double eraRadians = 2.0 * Math.PI * (0.7790572732640 + 1.00273781191135448 * dUT1);
            eraRadians %= 2.0 * Math.PI; // Normalize to [0, 2π)

            if (eraRadians < 0)
                eraRadians += 2.0 * Math.PI;

            return (float)eraRadians;
        }

        protected override void Update(RenderContext ctx)
        {
            if (Animate)
            {
                _dateTime += TimeSpan.FromSeconds(ctx.DeltaTime * Speed);

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Time)));

                _posDirty = true;
            }


            UpdatePos();

            base.Update(ctx);
        }

        protected void UpdatePos()
        {
            if (_host?.Earth?.Orbit == null)
                return;

            if (_posDirty || true)
            {
                var earthPos = _host.Earth.Orbit.GetPosition(_dateTime);

                EarthPosAngle = MathF.Atan2(earthPos.X, earthPos.Z);

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EarthPosAngle)));

                foreach (var item in _host.Children)
                {
                    if (item == _host.Earth || item is Camera)
                        continue;

                    item.Transform.Position = -earthPos;
                }

                _host.Earth.Rotation = Offset + ComputeEarthRotationAngle(_dateTime);

                _posDirty = false;
            }

            if (LockCamera)
            {
                var cameraPos = ComputePosition(new Vector2(Latitude, Longitude), Altitude);

                var (cameraDir, cameraUp) = ComputeCameraDirectionAndUp(cameraPos, Heading, Elevation);

                var cameraTarget = cameraPos + cameraDir * Unit(10);

                _host.ActiveCamera!.LookAt(cameraPos, cameraTarget, cameraUp);
            }
        }

        public string Time
        {
            get => _dateTime.TimeOfDay.ToString("hh\\:mm");
            set
            {
                _dateTime = _dateTime.Date + TimeSpan.Parse(value);
                _posDirty = true;
            }
        }

        public string Date
        {
            get => _dateTime.Date.ToString("dd/MM/yyyy");
            set
            {
                _dateTime = DateTime.Parse(value).Date + _dateTime.TimeOfDay;
                _posDirty = true;
            }
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        public bool LockCamera { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float Heading { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float Elevation { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        [Range(0, 1000, 0.001f)]
        public float Altitude { get; set; } 

        public float Speed { get; set; }

        public bool Animate { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float Offset { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float EarthPosAngle { get; set; }

    }
}
