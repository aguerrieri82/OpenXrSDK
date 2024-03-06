using OpenXr.Framework;

namespace Xr.Engine.OpenXr
{
    public class RayCollider : Behavior<Scene>
    {
        readonly XrInput<XrPose> _input;
        readonly RayView _rayView;


        public RayCollider(XrInput<XrPose> input)
        {
            _input = input;
            _rayView = new RayView();
        }

        protected override void Start(RenderContext ctx)
        {
            _host!.AddChild(_rayView);
        }

        protected override void Update(RenderContext ctx)
        {
            if (_input.IsChanged && _input.IsActive && _input.Value != null)
            {
                _rayView.Transform.Position = _input.Value.Position;
                _rayView.Transform.Orientation = _input.Value.Orientation;

                var ray = new Ray3
                {
                    Origin = _rayView.WorldPosition,
                    Direction = _rayView.Forward,
                };

                var result = _host!.RayCollisions(ray).FirstOrDefault();
                if (result != null)
                {
                    _rayView.Materials[0].Color = new Color(0, 1, 0);
                    _rayView.Length = result.Distance;

                    var rayTarget = result.Object!.Components<IRayTarget>().FirstOrDefault();
                    rayTarget?.NotifyCollision(ctx, result);
                }
                else
                {
                    _rayView.Length = 3;
                    _rayView.Materials[0].Color = new Color(1, 1, 1);
                }
            }

            base.Update(ctx);
        }

        public RayView RayView => _rayView;
    }
}
