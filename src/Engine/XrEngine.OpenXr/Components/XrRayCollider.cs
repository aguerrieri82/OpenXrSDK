using OpenXr.Framework;
using System.Collections.Concurrent;
using System.Diagnostics;
using XrEngine.Interaction;
using XrMath;

namespace XrEngine.OpenXr
{
    public interface IRayColliderHandler
    {

        bool UpdateRayView(XrRayCollider collider, Collision? collision);

        IEnumerable<ICollider3D>? GetColliders();

        bool IsActive { get; }
    }

    public class XrRayCollider : Behavior<Scene3D>
    {
        protected XrPoseInput? _input;
        protected IRayTarget[] _sceneTargets = [];
        protected Collision? _lastCollision;
        protected readonly RayView _rayView;
        protected readonly HitTargetView _hitView;
        protected readonly ConcurrentBag<Collision> _collisions = [];

        public XrRayCollider()
        {
            _rayView = new RayView();
            _hitView = new HitTargetView();
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
            container.Write(nameof(InputName), InputName);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            InputName = container.Read<string>(nameof(InputName));
        }

        protected override void Start(RenderContext ctx)
        {
            Debug.Assert(InputName != null);
            Debug.Assert(_host?.Scene != null);

            _input = (XrPoseInput?)XrApp.Current?.Inputs[InputName];
            _sceneTargets = _host.Scene.Components<IComponent>().OfType<IRayTarget>().ToArray();
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_input != null);
            Debug.Assert(_host != null);

            if (!_input.IsChanged || !_input.IsActive)
                return;

            _rayView.WorldPosition = _input.Value.Position;
            _rayView.WorldOrientation = _input.Value.Orientation;

            var ray = new Ray3
            {
                Origin = _rayView.WorldPosition,
                Direction = _rayView.Forward,
            };

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

        public string? InputName { get; set; }

        public bool ShowHit { get; set; }

        public RayView RayView => _rayView;

        public HitTargetView HitTargetView => _hitView;
    }
}
