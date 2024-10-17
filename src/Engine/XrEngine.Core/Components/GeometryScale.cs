using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public class GeometryScale : BaseComponent<Object3D>, IDrawGizmos, IScaleHandler
    {
        private Dictionary<TriangleMesh, Geometry3D> _geometries = [];
        private Bounds3 _bounds;
        private Vector3 _size;

        public GeometryScale()
        {
            Size = Vector3.One;
            UseGizmos = true;
        }

        protected override void OnAttach()
        {
            _host!.UpdateBounds(true);

            _bounds = _host!.WorldBounds;

            _size = _bounds.Size;

            foreach (var item in _host!.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                _geometries[item] = item.Geometry!.Clone();
                item.Geometry.Flags = EngineObjectFlags.None;
            }
  
            base.OnAttach();
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            if (!UseGizmos)
                return;

            canvas.Save();

            //canvas.State.Transform = _host!.WorldMatrix;

            var planes = _bounds.Faces().ToArray();

            planes[0].Pose.Position.Z = Min.Z;
            planes[1].Pose.Position.Z = Max.Z;
            planes[2].Pose.Position.Y = Min.Y;
            planes[3].Pose.Position.Y = Max.Y;
            planes[4].Pose.Position.X = Min.X;
            planes[5].Pose.Position.X = Max.X;

            canvas.State.Color = new Color(0, 0, 0.5f, 1);
            canvas.DrawQuad(planes[0], false);
            canvas.State.Color = new Color(0, 0, 1f, 1);
            canvas.DrawQuad(planes[1], false);

            canvas.State.Color = new Color(0, 0.5f, 0, 1);
            canvas.DrawQuad(planes[2], false);
            canvas.State.Color = new Color(0, 1, 0, 1);
            canvas.DrawQuad(planes[3], false);

            canvas.State.Color = new Color(0.5f, 0, 0, 1);
            canvas.DrawQuad(planes[4], false);
            canvas.State.Color = new Color(1, 0, 0, 1);
            canvas.DrawQuad(planes[5], false);

            canvas.Restore();
        }

        protected void Resize()
        {
            if (_host == null)
                return;

            var delta = (Size - _bounds.Size) / 2f;

            var curTransform = _host.Transform.Matrix;

            foreach (var item in _geometries)
            {
                var mesh = item.Key;   
                var geometry = item.Value; 

                var curVer = mesh.Geometry!.Vertices;
                var startVer = geometry!.Vertices;

                for (var i = 0; i < curVer.Length; i++)
                {
                    var starVerPosWorld = startVer[i].Pos.Transform(mesh.WorldMatrix);

                    var verDelta = Vector3.Zero;

                    if (starVerPosWorld.X > Max.X)
                        verDelta.X = delta.X;
                    else if (starVerPosWorld.X < Min.X)
                        verDelta.X = -delta.X;

                    if (starVerPosWorld.Y > Max.Y)
                        verDelta.Y = delta.Y;
                    else if (starVerPosWorld.Y < Min.Y)
                        verDelta.Y = -delta.Y;

                    if (starVerPosWorld.Z > Max.Z)
                        verDelta.Z = delta.Z;
                    else if (starVerPosWorld.Z < Min.Z)
                        verDelta.Z = -delta.Z;

                    curVer[i].Pos = (starVerPosWorld + verDelta).Transform(mesh.WorldMatrixInverse);
                }

                mesh.Geometry.NotifyChanged(ObjectChangeType.Geometry);
            }

            _host.Transform.SetMatrix(curTransform);

            if (_host is Group3D grp)
                grp.UpdateBounds(true);
        }

        [Action]
        public void ResetSize()
        {
            Size = _bounds.Size;
        }

        public Vector3 Size
        {
            get => _size;
            set
            {
                if (_size == value)
                    return;
                _size = value;
                Resize();
            }
        }

        public bool UseGizmos { get; set; }

        public Vector3 Min { get; set; }

        public Vector3 Max { get; set; }
    }
}
