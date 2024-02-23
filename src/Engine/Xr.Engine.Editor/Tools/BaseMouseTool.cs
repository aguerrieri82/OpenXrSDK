using OpenXr.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Xr.Engine.Editor
{
    [Flags]
    public enum MouseButton
    {
        Left = 0x1,
        Middle= 0x2,
        Right = 0x4
    }



    public class BaseMouseTool : IEditorTool
    {
        protected SceneView? _sceneView;

        public virtual void Attach(SceneView view)
        {
            _sceneView = view;
            _sceneView.RenderHost.PointerDown += OnMouseDown;
            _sceneView.RenderHost.PointerUp += OnMouseUp;
            _sceneView.RenderHost.PointerMove += OnMouseMove;
            _sceneView.RenderHost.WheelMove += OnWheelMove; ;
        }

        protected virtual void OnWheelMove(PointerEvent ev)
        {

        }

        protected Vector3 ToView(PointerEvent ev, float z = -1f)
        {
            var width = (float)_sceneView!.RenderHost.PixelSize.Width;
            var height = (float)_sceneView!.RenderHost.PixelSize.Height;

            return  new Vector3(
                2.0f * ev.X / (float)width - 1.0f,
                1.0f - 2.0f * ev.Y / (float)height,
                z
            );
        }

        protected Vector3 ToWorld(PointerEvent ev, float z = -1f)
        {
            var normPoint = ToView(ev, z);
            var dirEye = Vector4.Transform(new Vector4(normPoint, 1.0f), _sceneView!.Camera!.ProjectionInverse);
            dirEye /= dirEye.W;
            var pos4 = Vector4.Transform(dirEye, _sceneView!.Camera.WorldMatrix);
            return new Vector3(pos4.X, pos4.Y, pos4.Z);
        }

        protected Ray3 ToRay(PointerEvent ev)
        {
            var normPoint = ToView(ev);

            var dirEye = Vector4.Transform(new Vector4(normPoint, 1.0f), _sceneView!.Camera!.ProjectionInverse);
            dirEye.W = 0;

            var dirWorld = Vector4.Transform(dirEye, _sceneView!.Camera.WorldMatrix);

            return new Ray3
            {
                Origin = _sceneView!.Camera.WorldPosition,
                Direction = new Vector3(dirWorld.X, dirWorld.Y, dirWorld.Z).Normalize()
            };
        }

        protected virtual void OnMouseDown(PointerEvent ev)
        {

        }

        protected virtual void OnMouseUp(PointerEvent ev)
        {

        }

        protected virtual void OnMouseMove(PointerEvent ev)
        {

        }
    }
}
