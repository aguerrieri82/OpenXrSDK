using OpenXr.Framework;
using System.Collections.Concurrent;
using System.Numerics;
using XrEngine;
using XrInteraction;
using XrMath;

namespace XrEditor
{
    public abstract class PickTool : BasePointerTool, IDrawGizmos, IRayPointer
    {
        protected Object3D? _currentPick;
        protected Color? _oldColor;
        protected Collision? _lastCollision;
        protected RayPointerStatus _lastRay;
        protected bool _isPicking;
        protected IViewHitTest? _hitTest;
        protected readonly ConcurrentBag<Collision> _collisions = [];

        public PickTool()
        {

        }

        public override void NotifySceneChanged()
        {
            var debug = _sceneView?.Scene?.Components<DebugGizmos>().FirstOrDefault();

            if (debug != null)
                debug.Debuggers.Add(this);
        }

        RayPointerStatus IRayPointer.GetPointerStatus()
        {
            var result = _lastRay;
            result.IsActive = XrApp.Current == null || !XrApp.Current.IsStarted;
            return result;
        }

        protected void UpdateRay(Pointer2Event ev)
        {
            _lastRay.Ray = ToRay(ev);
            _lastRay.Buttons = ev.Buttons;
            _lastRay.IsActive = true;
        }

        protected override void OnPointerDown(Pointer2Event ev)
        {
            UpdateRay(ev);
            base.OnPointerDown(ev);
        }

        protected override void OnPointerUp(Pointer2Event ev)
        {
            UpdateRay(ev);
            base.OnPointerUp(ev);
        }

        protected override async void OnPointerMove(Pointer2Event ev)
        {
            if (_sceneView?.Scene == null)
                return;

            UpdateRay(ev);

            if (_isPicking)
                return;

            Context.TryRequire(out _hitTest);

            _isPicking = true;

            Object3D? newPick = null;   

            await AppDispatcher.ExecuteAsync(() =>
            {
                if (_hitTest != null)
                {
                    var result = _hitTest.HitTest((uint)ev.Position.X, (uint)ev.Position.Y);
                    if (result.Object == null)
                        _lastCollision = null;
                    else
                    {
                        _lastCollision = new Collision
                        {
                            Object = result.Object,
                            Normal = result.Normal,
                            Point = result.Pos,
                            LocalPoint = result.Object!.ToLocal(result.Pos),
                        };
                    }
  
                }
                else
                {
                    _sceneView.Scene.RayCollisions(_lastRay.Ray, _collisions);

                    _lastCollision = _collisions.OrderBy(a => a.Distance)
                                                .FirstOrDefault();
                }
                
                newPick = _lastCollision?.Object;

                _isPicking = false;

            }).ConfigureAwait(false);


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
            if (obj is TriangleMesh mesh && mesh.Materials.Count > 0 && mesh.Materials[0] is BasicMaterial mat && _oldColor != null)
            {
                mat.Color = _oldColor.Value;
                mat.NotifyChanged(ObjectChangeType.Render);
            }
        }

        protected virtual void OnEnter(Object3D obj)
        {
            if (obj is TriangleMesh mesh && mesh.Materials.Count > 0 && mesh.Materials[0] is BasicMaterial mat)
            {
                _oldColor = mat.Color;
                mat.Color = new Color(0, 1, 0, 1);
                mat.NotifyChanged(ObjectChangeType.Render);
            }
        }

        public virtual void DrawGizmos(Canvas3D canvas)
        {

        }

        public void CapturePointer()
        {
            if (_sceneView != null)
                _sceneView.ActiveTool = this;
        }

        public void ReleasePointer()
        {
            if (_sceneView?.ActiveTool == this)
                _sceneView.ActiveTool = null;
        }

        public bool IsCaptured => _sceneView?.ActiveTool == this;

        int IRayPointer.PointerId => -100;

        string IRayPointer.Name => "Mouse";

        bool IDrawGizmos.IsEnabled => _isActive;
    }
}
