using Silk.NET.Vulkan;
using System.Numerics;
using XrEditor.Services;
using XrEngine;
using XrEngine.Interaction;
using XrMath;


namespace XrEditor
{


    public abstract class BasePointerTool : IEditorTool
    {
        protected SceneView? _sceneView;
        protected bool _isActive;
        protected readonly IMainDispatcher _main;

        public BasePointerTool()
        {
            _isActive = true;
            _main = Context.Require<IMainDispatcher>();
        }

        public virtual void Attach(SceneView view)
        {
            _sceneView = view;
            _sceneView.RenderSurface.PointerDown += OnPointerDown;
            _sceneView.RenderSurface.PointerUp += OnPointerUp;
            _sceneView.RenderSurface.PointerMove += OnPointerMove;
            _sceneView.RenderSurface.WheelMove += OnWheelMove; ;
        }


        protected Vector3 ToView(Pointer2Event ev, float z = -1f)
        {
            var width = _sceneView!.RenderSurface.Size.X;
            var height = _sceneView!.RenderSurface.Size.Y;

            return new Vector3(
                2.0f * ev.Position.X / (float)width - 1.0f,
                1.0f - 2.0f * ev.Position.Y / (float)height,
                z
            );
        }

        protected Vector3 ToWorld(Pointer2Event ev, float z = -1f)
        {
            var normPoint = ToView(ev, z);
            return _sceneView!.Camera!.Unproject(normPoint);
        }

        protected Ray3 ToRay(Pointer2Event ev)
        {
            if (_sceneView?.Camera == null)
                return new Ray3();

            var normPoint = ToView(ev);

            var dirEye = Vector4.Transform(new Vector4(normPoint, 1.0f), _sceneView.Camera.ProjectionInverse);
            dirEye.W = 0;

            var dirWorld = Vector4.Transform(dirEye, _sceneView.Camera.WorldMatrix);

            return new Ray3
            {
                Origin = _sceneView.Camera.WorldPosition,
                Direction = new Vector3(dirWorld.X, dirWorld.Y, dirWorld.Z).Normalize()
            };
        }

        protected virtual void OnWheelMove(Pointer2Event ev)
        {

        }
        protected virtual void OnPointerDown(Pointer2Event ev)
        {

        }

        protected virtual void OnPointerUp(Pointer2Event ev)
        {

        }

        protected virtual void OnPointerMove(Pointer2Event ev)
        {

        }

        public virtual void NotifySceneChanged()
        {

        }

        public bool IsActive => _isActive;

    }
}
