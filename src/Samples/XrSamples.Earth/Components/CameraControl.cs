using OpenXr.Framework;
using System.ComponentModel;
using System.Numerics;
using XrEngine;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public enum AnchorPoint
    {
        Free,
        Earth,
        Moon,
        Sun
    }


    public class CameraControl : Behavior<EarthScene>, INotifyPropertyChanged
    {
        DateTime _dateTime;
        bool _posDirty;
        private readonly bool _sunAtOrigin;

        public CameraControl()
        {
            _dateTime = DateTime.Now;
            Speed = 1000;
            Latitude = 12.1518378f;
            Longitude = 43.570749f;
            Altitude = 2;
            Heading = 0;
            LockCamera = true;
            Origin = AnchorPoint.Earth;
            Target = AnchorPoint.Free;
            Zoom = 1;

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
            Vector3 localUp = Vector3.Normalize(cameraPosition);

            Vector3 worldNorthAxis = new Vector3(0, 1, 0)
                .Transform(_host!.Earth.Transform.Orientation)
                .Normalize();

            Vector3 localEast = Vector3.Cross(worldNorthAxis, localUp).Normalize();
            Vector3 localNorth = Vector3.Cross(localUp, localEast).Normalize();

            float cosE = MathF.Cos(elevation);
            float sinE = MathF.Sin(elevation);
            float cosA = MathF.Cos(azimuth);
            float sinA = MathF.Sin(azimuth);

            Vector3 dirHoriz = cosE * (cosA * localNorth + sinA * localEast);

            Vector3 cameraDir = Vector3.Normalize(dirHoriz + sinE * localUp);
            Vector3 cameraRight = Vector3.Cross(cameraDir, localUp).Normalize();
            Vector3 cameraUp = Vector3.Cross(cameraRight, cameraDir).Normalize();

            return (cameraDir, cameraUp);
        }

        Vector3 ComputePosition(Vector2 latLng, float altitude)
        {
            float lonRad = (latLng.X * MathF.PI / 180.0f);
            float latRad = (latLng.Y * MathF.PI / 180.0f);

            float radius = (_host!.Earth.SphereRadius + Unit(altitude));


            // Compute Cartesian coordinates
            float x = radius * MathF.Cos(latRad) * MathF.Cos(lonRad);
            float y = radius * MathF.Sin(latRad);
            float z = radius * MathF.Cos(latRad) * MathF.Sin(lonRad);


            return new Vector3(x, y, -z).Transform(_host.Earth.WorldMatrix);
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

            if (Zoom < 1)
                Zoom = 1;

            if (_posDirty || true)
            {
                Vector3 earthPos = _host.Earth.Orbit.GetPosition(_dateTime);

                _host.Moon.Transform.Position = _host.Moon.Orbit!.GetPosition(_dateTime);

                EarthPosAngle = MathF.Atan2(earthPos.X, earthPos.Z);

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EarthPosAngle)));

                _host.Earth.Rotation = _host.Earth.RotationAngle(_dateTime);

                _host.Moon.Rotation = _host.Moon.RotationAngle(_dateTime);

                if (_sunAtOrigin)
                    _host.Earth.Transform.Position = earthPos;

                foreach (Object3D item in _host.Children)
                {
                    if (item == _host.Earth ||
                        item is Camera ||
                        item is StarDome ||
                        item is Moon ||
                        item.Name == "Orbit Moon")
                        continue;

                    if (_sunAtOrigin)
                        item.Transform.Position = Vector3.Zero;
                    else
                        item.Transform.Position = -earthPos;
                }

                _posDirty = false;
            }

            PerspectiveCamera camera = (PerspectiveCamera)_host.ActiveCamera!;

            if (LockCamera)
            {
                camera.FovDegree = 45f * (1f / Zoom);
                camera.UpdateProjection();

                Vector3 cameraPos = ComputePosition(new Vector2(Latitude, Longitude), Altitude);

                Vector3 normal = cameraPos.Normalize();

                Vector3 cameraDir, cameraUp;

                if (Target == AnchorPoint.Free)
                {
                    (cameraDir, cameraUp) = ComputeCameraDirectionAndUp(cameraPos, Heading, Elevation);
                }
                else if (Target == AnchorPoint.Moon)
                {
                    cameraDir = (_host.Moon.Transform.Position - _host.ActiveCamera!.Transform.Position).Normalize();
                    cameraUp = normal;

                }
                else if (Target == AnchorPoint.Sun)
                {
                    cameraDir = (_host.Sun.Transform.Position - _host.ActiveCamera!.Transform.Position).Normalize();
                    cameraUp = normal;
                }
                else
                    throw new NotImplementedException();

                Vector3 cameraTarget = cameraPos + cameraDir * Unit(10);

                if (XrApp.Current != null && XrApp.Current.IsStarted)
                    XrApp.Current.ReferenceFrame = camera.GetWorldPose();
                else
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

        [Range(0, 1000, 0.5f)]
        public float Altitude { get; set; }

        public float Speed { get; set; }

        public bool Animate { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float EarthPosAngle { get; set; }

        public AnchorPoint Target { get; set; }

        public AnchorPoint Origin { get; set; }


        [Range(1, 100, 1)]
        public float Zoom { get; set; }

    }
}
