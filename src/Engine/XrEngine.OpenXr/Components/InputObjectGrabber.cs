using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public class InputObjectGrabber : BaseObjectGrabber<Scene3D>
    {
        public InputObjectGrabber()
        {
        }

        public InputObjectGrabber(XrPoseInput input, XrHaptic? vibrate, params XrFloatInput[] handlers)
            : base(vibrate, input.Name)
        {
            Input = input;
            Handlers = handlers;
            Vibrate = vibrate;
        }

        protected override ObjectGrab IsGrabbing()
        {
            return new ObjectGrab
            {
                Pose = Input!.Value,
                IsGrabbing = Handlers!.All(a => a.Value > 0.8),
                IsValid = Input.IsActive
            };
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Input), Input);
            if (Handlers != null)
            {
                var handlers = container.Enter(nameof(Handlers));
                for (var i = 0; i < Handlers.Count; i++)
                    handlers.Write(i.ToString(), Handlers[i]);
            }
        }


        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);

            Input = container.Read<XrPoseInput>(nameof(Input));
            Handlers = [];

            if (container.Contains(nameof(Handlers)))
            {
                var handlersState = container.Enter(nameof(Handlers));
                for (var i = 0; i < handlersState.Count; i++)
                    Handlers.Add(handlersState.Read<XrFloatInput>(i.ToString()));
            }
        }


        public XrPoseInput? Input { get; set; }

        public IList<XrFloatInput>? Handlers { get; set; }

    }
}
