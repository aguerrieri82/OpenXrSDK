using System.Numerics;
using XrInteraction;

namespace XrEditor
{
    public class CameraMoveKeyTool : BaseKeyboardTool
    {
        protected readonly HashSet<KeyCode> _activeKeys = [];
        protected double _lastTime;

        public CameraMoveKeyTool()
        {
            MoveSpeed = 3f;
            RotateSpeed = 1.5f;
            _isActive = false;
        }

        public override void Attach(SceneView view)
        {
            base.Attach(view);
            _sceneView!.BeforeRender += OnBeforeRender;
        }

        private void OnBeforeRender(XrEngine.Scene3D scene)
        {
            var time = scene.App!.RenderContext.Time;
            var dt = _lastTime == 0 ? 0 : (float)(time - _lastTime);
            _lastTime = time;

            if (!_isActive || dt <= 0)
                return;

            var camera = _sceneView!.Camera;

            var forward = camera.Forward;
            forward.Y = 0;
            forward = Vector3.Normalize(forward);

            var right = Vector3.Cross(forward, Vector3.UnitY);
            var direction = Vector3.Zero;
            var yaw = 0f;
            var pitch = 0f;

            lock (_activeKeys)
            {
                if (_activeKeys.Contains(KeyCode.Up))
                    direction += forward;
                if (_activeKeys.Contains(KeyCode.Down))
                    direction -= forward;
                if (_activeKeys.Contains(KeyCode.Right))
                    direction += right;
                if (_activeKeys.Contains(KeyCode.Left))
                    direction -= right;

                if (_activeKeys.Contains(KeyCode.A))
                    yaw += RotateSpeed * dt;
                if (_activeKeys.Contains(KeyCode.D))
                    yaw -= RotateSpeed * dt;
                if (_activeKeys.Contains(KeyCode.W))
                    pitch += RotateSpeed * dt;
                if (_activeKeys.Contains(KeyCode.S))
                    pitch -= RotateSpeed * dt;
            }

            if (direction != Vector3.Zero)
            {
                var offset = Vector3.Normalize(direction) * MoveSpeed * dt;
                camera.WorldPosition += offset;
                camera.Target += offset;
            }

            if (yaw != 0 || pitch != 0)
            {
                var distance = Vector3.Distance(camera.WorldPosition, camera.Target);

                if (yaw != 0)
                {
                    var rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
                    camera.WorldOrientation = Quaternion.Normalize(rot * camera.WorldOrientation);
                }

                if (pitch != 0)
                {
                    var cameraRight = Vector3.Transform(Vector3.UnitX, camera.WorldOrientation);
                    var rot = Quaternion.CreateFromAxisAngle(cameraRight, pitch);
                    camera.WorldOrientation = Quaternion.Normalize(rot * camera.WorldOrientation);
                }

                camera.Target = camera.WorldPosition + camera.Forward * distance;
            }
        }

        protected override void OnKeyDown(KeyboardEvent ev)
        {
            lock (_activeKeys)
                _activeKeys.Add(ev.Key);

        }

        protected override void OnKeyUp(KeyboardEvent ev)
        {
            lock (_activeKeys)
                _activeKeys.Remove(ev.Key);

        }

        public float MoveSpeed { get; set; }

        public float RotateSpeed { get; set; }
    }
}
