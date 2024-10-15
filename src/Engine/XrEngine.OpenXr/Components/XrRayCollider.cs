using OpenXr.Framework;
using System.Diagnostics;
using XrEngine.Interaction;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrRayCollider : Behavior<Scene3D>
    {
        protected XrPoseInput? _input;
        protected IRayTarget[] _sceneTargets = [];
        protected readonly RayView _rayView;
        protected readonly HitTargetView _hitView;

        public XrRayCollider()
        {
            _rayView = new RayView();
            _hitView = new HitTargetView();

            ShowHit = true; 
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

            _host.AddChild(_rayView);
            _host.AddChild(_hitView);

            _sceneTargets = _host.Scene.Components<IComponent>().OfType<IRayTarget>().ToArray();
        }

        protected override void Update(RenderContext ctx)
        {
            Debug.Assert(_input != null);
            Debug.Assert(_host != null);

            if (_input.IsChanged && _input.IsActive)
            {
                _rayView.WorldPosition = _input.Value.Position;
                _rayView.WorldOrientation = _input.Value.Orientation;

                var ray = new Ray3
                {
                    Origin = _rayView.WorldPosition,
                    Direction = _rayView.Forward,
                };

                Collision? result = null;

                var results = _host.RayCollisions(ray).ToArray();
                if (results.Length > 0)
                {
                    var minDistance = results.Min(a => a.Distance);
                    result = results.Where(a => a.Distance == minDistance).FirstOrDefault();
                }
                if (result != null)
                {
                    _rayView.UpdateColor(new Color(0, 1, 0));

                    _rayView.Length = result.Distance;

                    var rayTarget = result.Object?.Feature<IRayTarget>();

                    rayTarget?.NotifyCollision(ctx, result);

                    foreach (var target in _sceneTargets)
                        target.NotifyCollision(ctx, result);

                    if (ShowHit)
                    {
                        _hitView.WorldPosition = result.Point;

                        if (result.Normal != null)
                            _hitView.Forward = result.Normal.Value.ToDirection(result.Object!.WorldMatrix);

                        _hitView.IsVisible = true;

                    }
                }
                else
                {
                    _hitView.IsVisible = false;
                    _rayView.Length = 3;
                    _rayView.UpdateColor(new Color(1, 1, 1));
                }
            }
        }

        public string? InputName { get; set; }

        public bool ShowHit { get; set; }

        public RayView RayView => _rayView;


    }
}
