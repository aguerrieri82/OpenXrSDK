using System.Collections.Concurrent;
using System.Diagnostics;
using XrEngine.Interaction;
using XrInteraction;
using XrMath;

namespace XrEngine.OpenXr
{
    public interface IRayColliderHandler
    {
        bool UpdateRayView(RayPointerCollider collider, Collision? collision);

        IEnumerable<ICollider3D>? GetColliders();

        bool IsActive { get; }
    }

    public class RayPointerCollider : Behavior<Scene3D>
    {
        protected IRayTarget[] _sceneTargets = [];
        protected Collision? _lastCollision;
        protected readonly RayView _rayView;
        protected readonly HitTargetView _hitView;
        protected readonly ConcurrentBag<Collision> _collisions = [];

        public RayPointerCollider()
        {
            _rayView = new RayView();
            _hitView = new HitTargetView();
            _hitView.Materials[0].IsEnabled = false;
            ShowHit = false;
        }

        protected override void OnAttach()
        {
            _host!.AddChild(_rayView);
            _host.AddChild(_hitView);
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(PointerName), PointerName);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            PointerName = container.Read<string>(nameof(PointerName));
        }

        protected override void Start(RenderContext ctx)
        {
            Debug.Assert(_host?.Scene != null);

            if (Pointer == null && !string.IsNullOrWhiteSpace(PointerName))
            {
                Pointer = _host!.Scene
                      .Components<IRayPointer>()
                      .Where(a => a.Name == PointerName)
                      .FirstOrDefault();
            }
    
            _sceneTargets = _host.Scene.Components<IRayTarget>().ToArray();
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_host != null);

            var status = Pointer?.GetPointerStatus();

            if (status == null || !status.Value.IsActive)
                return;

            var ray = status.Value.Ray;
            
            _rayView.SetGlobalPoseIfChanged(ray.ToPose());

            Collision? result = null;

            var colliders = Handler != null && Handler.IsActive ? Handler.GetColliders() : null;

            _host.RayCollisions(ray, _collisions, colliders);

            if (_collisions.Count > 0)
            {
                var minDistance = _collisions.Min(a => a.Distance);
                result = _collisions.Where(a => a.Distance == minDistance).FirstOrDefault();
            }

            if (result != null)
            {
                NotifyCollision(ctx, result);

                _rayView.UpdateColor(new Color(0, 1, 0));

                bool mustUpdate = true;

                if (Handler != null && Handler.IsActive)
                {
                    if (Handler.UpdateRayView(this, result))
                        mustUpdate = false;
                }

                if (mustUpdate)
                {
                    _rayView.Length = result.Distance;

                    if (result.Point != _hitView.WorldPosition)
                        _hitView.WorldPosition = result.Point;

                    if (result.Normal != null)
                        _hitView.Forward = result.Normal.Value.ToDirection(result.Object!.WorldMatrix);
                }
            }
            else
            {
                _rayView.Length = 3;
                _rayView.UpdateColor(new Color(1, 1, 1));

                if (_lastCollision != null)
                    NotifyCollision(ctx, null);
            }

            _lastCollision = result;

            _hitView.Materials[0].IsEnabled = ShowHit;
        }

        protected void NotifyCollision(RenderContext ctx, Collision? collision)
        {
            var rayTarget = collision?.Object?.Feature<IRayTarget>();

            rayTarget?.NotifyCollision(ctx, collision);

            foreach (var target in _sceneTargets)
                target.NotifyCollision(ctx, collision);

        }

        public IRayColliderHandler? Handler { get; set; }

        public IRayPointer? Pointer { get; set; }

        public string? PointerName { get; set; }    

        public bool ShowHit { get; set; }

        public RayView RayView => _rayView;

        public HitTargetView HitTargetView => _hitView;
    }
}
