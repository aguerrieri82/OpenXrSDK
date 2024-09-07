using OpenXr.Framework;
using XrEngine.Interaction;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrRayCollider : Behavior<Scene3D>
    {
        protected XrPoseInput? _input;
        protected readonly RayView _rayView;

        public XrRayCollider()
        {
            _rayView = new RayView();
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
            _input = (XrPoseInput)XrApp.Current!.Inputs[InputName!];

            _host!.AddChild(_rayView);
        }

        protected override void Update(RenderContext ctx)
        {
            if (_input!.IsChanged && _input.IsActive)
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
                    _rayView.UpdateColor(new Color(0, 1, 0));

                    _rayView.Length = result.Distance;

                    var rayTarget = result.Object!.Components<IRayTarget>().FirstOrDefault();
                    rayTarget?.NotifyCollision(ctx, result);
                }
                else
                {
                    _rayView.Length = 3;
                    _rayView.UpdateColor(new Color(1, 1, 1));
                }
            }

            base.Update(ctx);
        }

        public string? InputName { get; set; }

        public RayView RayView => _rayView;
    }
}
