namespace XrEngine
{
    public class UpdateAction
    {
        public Action? Execute { get; set; }

        public Action? Rollback { get; set; }

        public string? Name { get; set; }
    }

    public class SceneAddAction : UpdateAction
    {
        public Scene3D? Scene { get; set; }

        public Object3D? Object { get; set; }
    }

    public class SceneRemoveAction : UpdateAction
    {
        public Scene3D? Scene { get; set; }

        public Object3D? Object { get; set; }
    }

    public class EntityChangedAction : UpdateAction
    {
        public Scene3D? Scene { get; set; }

        public EngineObject? Object { get; set; }

        public IStateContainer? NewState { get; set; }

        public IStateContainer? OldState { get; set; }
    }

    public class UpdateHistory : IObjectChangeListener
    {
        protected int _index;
        protected List<UpdateAction> _actions = [];
        protected Scene3D _scene;
        private int _suspend;

        public UpdateHistory(Scene3D scene)
        {
            _scene = scene;
            _scene.ChangeListeners.Add(this);
        }

        public void Suspend()
        {
            _suspend++;
        }

        public void Resume()
        {
            _suspend--;
        }

        public void Add(UpdateAction action)
        {
            while (_actions.Count > _index)
                _actions.RemoveAt(_actions.Count - 1);

            _actions.Add(action);

            _index++;
        }

        public void NotifyChanged(Object3D object3D, ObjectChange change)
        {
            if (_suspend > 0)
                return;

            if (change.IsAny(ObjectChangeType.SceneAdd))
                Add(new SceneAddAction
                {
                    Scene = _scene,
                    Name = "Add Object",
                    Object = object3D,
                });

            else if (change.IsAny(ObjectChangeType.SceneAdd))
            {
                Add(new SceneRemoveAction
                {
                    Scene = _scene,
                    Name = "Remove Object",
                    Object = object3D,
                });
            }
            else if (change.IsAny(ObjectChangeType.Render))
            {
                Add(new EntityChangedAction
                {
                    Scene = _scene,
                    Name = "Changed",
                    Object = change.Target,
                });
            }

        }

        public IReadOnlyList<UpdateAction> Actions => _actions;

    }
}
