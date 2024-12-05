using Silk.NET.OpenXR;
using System.Numerics;
using System.Text.Json;
using XrMath;
using Action = Silk.NET.OpenXR.Action;

namespace OpenXr.Framework
{
  
    public abstract class XrInput<TValue> : IXrInput
    {
        protected bool _isChanged;
        protected TValue _value;
        protected bool _isActive;
        protected DateTime _lastChangeTime;
        protected Action _action;
        protected ulong _subPath;
        protected readonly XrApp _app;
        protected readonly ActionType _actionType;
        protected readonly string _path;
        protected readonly string _name;

        protected XrInput(XrApp app, string path, ActionType actionType, string name)
        {
            _app = app;
            _actionType = actionType;
            _path = path;
            _name = name;
            _value = default!;
        }

        public void Destroy()
        {
            if (_action.Handle != 0)
            {
                _app.CheckResult(_app.Xr.DestroyAction(_action), "DestroyAction");
                _action.Handle = 0;
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }


        public virtual ActionSuggestedBinding Initialize()
        {
            var result = new ActionSuggestedBinding
            {
                Binding = _app.StringToPath(_path),
                Action = _action.Handle == 0 ? _app.CreateAction(_name, _name, _actionType) : _action
            };
            _action = result.Action;
            return result;
        }

        public static XrInput<TValue> Create(XrApp app, string path, string name)
        {
            if (typeof(TValue) == typeof(float))
                return (new XrFloatInput(app, path, name) as XrInput<TValue>)!;

            if (typeof(TValue) == typeof(bool))
                return (new XrBoolInput(app, path, name) as XrInput<TValue>)!;

            if (typeof(TValue) == typeof(Vector2))
                return (new XrVector2Input(app, path, name) as XrInput<TValue>)!;

            if (typeof(TValue) == typeof(Pose3))
                return (new XrPoseInput(app, path, name) as XrInput<TValue>)!;

            throw new NotSupportedException();

        }

        public abstract void Update(Space refSpace, long predictTime);

        public virtual void SetState(XrInputState state)
        {
            _isActive = state.IsActive;
            _isChanged = state.IsChanged;
            if (state.Value is JsonElement je)
                state.Value = je.Deserialize<TValue>(new JsonSerializerOptions { IncludeFields = true })!;
            _value = (TValue)state.Value;
        }

        public virtual XrInputState GetState()
        {
            return new XrInputState
            {
                Value = _value!,
                IsChanged = _isChanged,
                IsActive = _isActive,
            };
        }

        public DateTime LastChangeTime => _lastChangeTime;

        public bool IsActive => _isActive;

        public bool IsChanged => _isChanged;

        public TValue Value => _value;

        public string Name => _name;

        public Action Action => _action;

        public string Path => _path;

        object IXrInput.Value => _value!;
    }


    public class XrFloatInput : XrInput<float>
    {
        public XrFloatInput(XrApp app, string path, string name)
            : base(app, path, ActionType.FloatInput, name)
        {
        }

        public override void Update(Space refSpace, long predictTime)
        {
            var state = _app.GetActionStateFloat(_action, _subPath);
            _isActive = state.IsActive != 0;
            _isChanged = state.ChangedSinceLastSync != 0;
            _lastChangeTime = DateTime.UnixEpoch + TimeSpan.FromTicks(state.LastChangeTime);
            _value = state.CurrentState;
        }
    }

    public class XrBoolInput : XrInput<bool>
    {
        public XrBoolInput(XrApp app, string path, string name)
            : base(app, path, ActionType.BooleanInput, name)
        {
        }

        public override void Update(Space refSpace, long predictTime)
        {
            if (_action.Handle == 0)
                return;

            var state = _app.GetActionStateBoolean(_action, _subPath);
            _isActive = state.IsActive != 0;
            _isChanged = state.ChangedSinceLastSync != 0;
            _lastChangeTime = DateTime.UnixEpoch + TimeSpan.FromTicks(state.LastChangeTime);
            _value = state.CurrentState != 0;
        }
    }

    public class XrVector2Input : XrInput<Vector2>
    {
        public XrVector2Input(XrApp app, string path, string name)
            : base(app, path, ActionType.Vector2fInput, name)
        {
        }

        public override void Update(Space refSpace, long predictTime)
        {
            var state = _app.GetActionStateVector2(_action, _subPath);
            _isActive = state.IsActive != 0;
            _isChanged = state.ChangedSinceLastSync != 0;
            _lastChangeTime = DateTime.UnixEpoch + TimeSpan.FromTicks(state.LastChangeTime);
            _value = new Vector2(state.CurrentState.X, state.CurrentState.Y);
        }
    }

    public class XrPoseInput : XrInput<Pose3>
    {
        protected Space _space;
        protected Vector3? _linearVelocity;
        protected Vector3? _angularVelocity;

        public XrPoseInput(XrApp app, string path, string name)
            : base(app, path, ActionType.PoseInput, name)
        {
        }

        public override ActionSuggestedBinding Initialize()
        {
            var result = base.Initialize();
            _space = _app.CreateActionSpace(_action, _subPath);
            _app.SpacesTracker.Add(_space, TimeSpan.Zero);
            return result;
        }

        public override void Update(Space refSpace, long predictTime)
        {
            if (_action.Handle == 0)
                return;

            _isActive = _app.GetActionPoseIsActive(_action, _subPath);
            _isChanged = true;
            _lastChangeTime = DateTime.UtcNow;

            var spaceInfo = _app.SpacesTracker.GetLastLocation(_space);

            if (spaceInfo != null && spaceInfo.IsValid)
            {
                _value = spaceInfo.Pose;

                if ((spaceInfo.VelocityFlags & SpaceVelocityFlags.LinearValidBit) != 0)
                    _linearVelocity = spaceInfo.LinearVelocity;
                else
                    _linearVelocity = null;

                if ((spaceInfo.VelocityFlags & SpaceVelocityFlags.AngularValidBit) != 0)
                    _angularVelocity = spaceInfo.AngularVelocity;
                else
                    _angularVelocity = null;
            }
        }

        public override void SetState(XrInputState state)
        {
            base.SetState(state);
            _angularVelocity = state.AngularVelocity;
            _linearVelocity = state.LinearVelocity;
        }

        public override XrInputState GetState()
        {
            var res = base.GetState();
            res.AngularVelocity = _angularVelocity;
            res.LinearVelocity = _linearVelocity;
            return res;
        }

        public Vector3? LinearVelocity => _linearVelocity;

        public Vector3? AngularVelocity => _angularVelocity;

        public Space Space => _space;
    }
}
