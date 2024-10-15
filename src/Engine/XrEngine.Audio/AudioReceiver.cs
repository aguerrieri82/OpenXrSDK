using OpenAl.Framework;
using System.Numerics;

namespace XrEngine.Audio
{
    public class AudioReceiver : Behavior<Object3D>
    {
        private AlListener? _listener;

        protected override void Start(RenderContext ctx)
        {
            var system = _host!.Scene!.Component<AudioSystem>();

            _listener = new AlListener(system.Device.Al);
        }

        protected override void Update(RenderContext ctx)
        {
            if (_listener == null)
                return;

            _listener.Position = _host!.WorldPosition;
            _listener.Orientation = new AudioOrientation
            {
                Forward = _host!.Forward,
                Up = Vector3.UnitY
            };
        }
    }
}
