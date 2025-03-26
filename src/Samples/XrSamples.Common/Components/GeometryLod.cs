using MeshOptimizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Tensorflow;
using XrEngine;
using XrMath;

namespace XrSamples.Components
{
    public class GeometryLod : Behavior<TriangleMesh>
    {
        Geometry3D? _lowGeo;
        Geometry3D? _curGeo;

        public GeometryLod()
        {
            MinVertices = 500;
            Factor = 0.3f;
            CutSize = 100;
        }

        protected override void Start(RenderContext ctx)
        {
            if (_host?.Geometry == null || _host.Geometry.Indices.Length < MinVertices)
                return;

            _curGeo = _host.Geometry;  
            _lowGeo = _host.Geometry.Clone();

            _host.Flags &= ~EngineObjectFlags.NotifyChanged;

            XrEngine.MeshOptimizer.Simplify(_lowGeo, Factor, 0.1f);

            if (_lowGeo.Indices.Length / (float)_curGeo.Indices.Length > 0.8f)
                _lowGeo = null;

            if (_host.Materials.Count == 1 && _host.Materials[0] is IPbrMaterial pbr)
            {
                var newMat = (IPbrMaterial)pbr.Clone();
                newMat.NormalMap = null;
                newMat.OcclusionMap = null;
                _host.Materials.Add((Material)newMat);
            }

            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            if (_lowGeo == null)
                return;

            var sb = new List<Vector2>();

            foreach (var p in _host!.LocalBounds.Points)
            {
                var t = p.Transform(_host!.WorldMatrix);
                var s = ctx.Camera!.WorldToScreen(t);
                sb.Add(s);
            }

            var bounds = sb.Bounds();

            if (bounds.Size.X < CutSize && bounds.Size.Y < CutSize)
            {
                if (_host.Geometry == _lowGeo)
                    return;
                _host.Geometry = _lowGeo;
                _host.Materials[0].IsEnabled = false;
                _host.Materials[1].IsEnabled = true;
                //Log.Debug(this, "Low res switch: {0} - {1}:{2}", _host.Name, _curGeo!.Indices.Length, _lowGeo.Indices.Length);
            }
            /*
            else
            {
                if (_host.Geometry == _curGeo)
                    return; 
                _host.Geometry = _curGeo;
                _host.Materials[0].IsEnabled = true;
                _host.Materials[1].IsEnabled = false;

            }
            */

            base.Update(ctx);
        }

        public float CutSize { get; set; }

        public float Factor { get; set; }   

        public int MinVertices { get; set; }
    }
}
