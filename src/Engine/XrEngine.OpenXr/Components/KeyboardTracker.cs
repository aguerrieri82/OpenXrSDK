using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class KeyboardTracker : AsyncBehavior<Object3D>, IDisposable, IDrawGizmos
    {
        private XrDynamicObjectTracker? _dynTracker;
        private XrApp? _app;
        private XrOculusPlugin? _oculus;
        private Space _keyboardSpace;
        private Bounds3 _bounds;
        private Pose3 _pose;
        private double _lastBoundsTime;
        private TriangleMesh? _hole;

        public KeyboardTracker()
        {
            CreateHole = true;
        }

        protected override async Task UpdateAsync(RenderContext ctx)
        {
            if (!XrDevice.IsMetaQuest)
                return;

            if (_app == null && XrApp.Current != null)
                _app = XrApp.Current;

            if (_app != null && _app.IsStarted && _dynTracker == null)
            {
                _dynTracker = new XrDynamicObjectTracker(_app);

                await _dynTracker.CreateAsync();
                await _dynTracker.SetTrackedClassesAsync(DynamicObjectClassMETA.KeyboardMeta);
            }

            if (_dynTracker != null && _keyboardSpace.Handle == 0)
            {
                _oculus ??= _app!.Plugin<XrOculusPlugin>();

                var result = await _oculus.QueryAllSpacesAsync(
                    storageLocation: SpaceStorageLocationFB.LocalFB,
                    component: METADynamicObjectTracker.SpaceComponentTypeDynamicObjectDataMeta);

                if (result.Length > 0)
                {
                    _keyboardSpace = result[0].Space;
                    _app!.SpacesTracker.Add(_keyboardSpace, TimeSpan.FromSeconds(0));

                    if (!_oculus.GetSpaceComponentEnabled(_keyboardSpace, SpaceComponentTypeFB.LocatableFB))
                        await _oculus.SetSpaceComponentStatusAsync(_keyboardSpace, SpaceComponentTypeFB.LocatableFB, true);

                    _bounds = _oculus.GetSpaceBoundingBox3D(_keyboardSpace).ToBounds3();
                    _lastBoundsTime = ctx.Time;
                }
                else
                {
                    await Task.Delay(1);
                }
            }

            if (_keyboardSpace.Handle == 0)
                return;

            if (ctx.Time - _lastBoundsTime > 2)
            {
                _bounds = _oculus!.GetSpaceBoundingBox3D(_keyboardSpace).ToBounds3();
                _lastBoundsTime = ctx.Time;
            }

            var loc = _app!.SpacesTracker.GetLastLocation(_keyboardSpace);

            if (loc != null && loc.IsValid)
            {
                _pose = loc.Pose;
                UpdateHole();
            }
        }

        private void UpdateHole()
        {
            if (!CreateHole)
            {
                _hole?.IsVisible = false;
                return;
            }

            if (_hole == null)
            {
                var material = new ColorMaterial(Color.Transparent)
                {
                    WriteDepth = true,
                    UseDepth = false
                };

                _hole = new TriangleMesh(new Cube3D(Vector3.One), material)
                {
                    Name = "Keyboard Hole"
                };

                _host!.Scene!.AddChild(_hole);
            }

            _hole.IsVisible = true;

            _hole.Transform.Position = _pose.Position + Vector3.Transform(_bounds.Center, _pose.Orientation);
            _hole.Transform.Orientation = _pose.Orientation;
            _hole.Transform.Scale = _bounds.Size;
        }

        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            if (_keyboardSpace.Handle == 0)
                return;

            canvas.Save();

            canvas.State.Color = "#00ff00";
            canvas.State.Transform = _pose.ToMatrix();

            canvas.DrawBounds(_bounds);

            canvas.Restore();
        }

        public void Dispose()
        {
            if (_dynTracker != null)
            {
                _dynTracker.Dispose();
                _dynTracker = null;
            }

            GC.SuppressFinalize(this);
        }

        public bool CreateHole { get; set; }

        public Pose3 Pose => _pose;

        public Bounds3 Bounds => _bounds;
    }
}