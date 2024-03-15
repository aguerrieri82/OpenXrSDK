﻿using System.Numerics;
using XrEngine;
using XrEngine.Components;
using XrEngine.Interaction;
using XrMath;

namespace XrEditor
{
    public class PickTool : BaseMouseTool, IDrawGizmos
    {
        protected Object3D? _currentPick;
        protected Color? _oldColor;
        protected Collision? _lastCollision;
        protected object _lock = new object();

        public override void NotifySceneChanged()
        {
            var debug = _sceneView?.Scene?.Components<DebugGizmos>().FirstOrDefault();

            if (debug != null)
                debug.Debuggers.Add(this);
        }

        protected override void OnMouseMove(PointerEvent ev)
        {

            if (_sceneView?.Scene == null)
                return;

            var ray = ToRay(ev);

            lock (_lock)
                _lastCollision = _sceneView.Scene.RayCollisions(ray).FirstOrDefault();

            var newPick = _lastCollision?.Object;

            if (newPick != null && !CanPick(newPick))
                newPick = null;

            if (_currentPick == newPick)
                return;

            if (_currentPick != null)
                OnLeave(_currentPick);

            _currentPick = newPick;

            if (_currentPick != null)
                OnEnter(_currentPick);
        }

        protected virtual bool CanPick(Object3D obj)
        {
            return true;
        }

        protected virtual void OnLeave(Object3D obj)
        {
            if (obj is TriangleMesh mesh && mesh.Materials.Count > 0 && mesh.Materials[0] is StandardMaterial mat && _oldColor != null)
            {
                mat.Color = _oldColor.Value;
                mat.NotifyChanged();
            }

        }

        protected virtual void OnEnter(Object3D obj)
        {
            if (obj is TriangleMesh mesh && mesh.Materials.Count > 0 && mesh.Materials[0] is StandardMaterial mat)
            {
                _oldColor = mat.Color;
                mat.Color = new Color(0, 1, 0, 1);
                mat.NotifyChanged();
            }
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            lock (_lock)
            {
                if (_lastCollision?.Normal == null)
                    return;

                canvas.Save();
                canvas.State.Color = new Color(0, 1, 0, 1);

                var normalMatrix = Matrix4x4.Transpose(_lastCollision.Object!.WorldMatrixInverse);

                canvas.DrawLine(_lastCollision.Point, _lastCollision.Point + _lastCollision.Normal.Value.Transform(normalMatrix).Normalize());
                canvas.Restore();
            }

        }
    }
}
