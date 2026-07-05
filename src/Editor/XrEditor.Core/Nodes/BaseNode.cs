
using System.Xml.Linq;
using XrEngine;

namespace XrEditor.Nodes
{
    public abstract class BaseNode<T> : IEditableNode where T : notnull
    {
        protected INode? _parent;
        protected T _value;
        protected string[]? _types;

        public BaseNode(T value)
        {
            _value = value;
        }


        protected string GetPresetStorePath()
        {
            var compName = _value.GetType().FullName!.Replace(".", "_");

            var path = Path.Combine("Presets", compName);

            return path;
        }

        public virtual void SavePreset(string name)
        {
            if (_value is not IStateManager sm)
                return;

            var root = new JsonStateContainer();
            sm.GetState(root);

            var compName = _value.GetType().FullName!.Replace(".", "_");

            var path = GetPresetStorePath();

            Directory.CreateDirectory(path);

            File.WriteAllText(Path.Combine(path, name + ".json"), root.AsJson());

        }

        public virtual IList<ComponentPreset> ListPresets()
        {
            var path = GetPresetStorePath();

            if (!Directory.Exists(path))
                return [];

            var result = new List<ComponentPreset>();

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                var json = File.ReadAllText(file);
                var preset = new ComponentPreset()
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    State = new JsonStateContainer(json)
                };

                result.Add(preset);
            }

            return result;
        }

        public virtual void DeletePreset(ComponentPreset preset)
        {
            var path = GetPresetStorePath();
            
            var fileName = Path.Combine(path, preset.Name + ".json");

            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        public virtual void LoadPreset(ComponentPreset preset)
        {
            if (_value is not IStateManager sm)
                return;

            sm.SetState(preset.State!);
        }

        protected virtual string[] ComputeType(object value)
        {
            var result = new List<string>();

            var curType = value.GetType()!;

            while (curType != typeof(object))
            {
                result.Add(curType!.Name);
                curType = curType.BaseType;
            }

            return [.. result];
        }

        public void SetParent(INode? parent)
        {
            _parent = parent;
        }

        public virtual bool IsLeaf => false;

        public virtual IEnumerable<INode> Children => [];

        public virtual IEnumerable<INode> Components => [];

        public ICollection<string> Types
        {
            get
            {
                _types ??= ComputeType(_value);
                return _types;
            }
        }

        public T Value => _value;

        object INode.Value => _value;

        public INode? Parent => _parent;

        protected DispatcherSwitch UiThread => Context.Require<IMainDispatcher>().Switch;
    }
}
