using OpenXr.Engine;
using OpenXr.Engine.Objects;
using OpenXr.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Engine
{
    public class ControllerCollider : Behavior<Scene>
    {
        XrInput<XrPose> _input;
        RayView _rayView;

        public ControllerCollider(XrInput<XrPose> input)
        {
            _input = input;
            _rayView = new RayView();
        }

        public override void Start(RenderContext ctx)
        {
            _host!.AddChild(_rayView);
        }

        protected override void Update(RenderContext ctx)
        {
            if (_input.IsChanged && _input.IsActive && _input.Value != null)
            {
                _rayView.Transform.Position = _input.Value.Position;
                _rayView.Transform.Orientation = _input.Value.Orientation;
                _rayView.UpdateWorldMatrix(false, false);
            }

            base.Update(ctx);
        }

        public RayView RayView => _rayView;
    }
}
