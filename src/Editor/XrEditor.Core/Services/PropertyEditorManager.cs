using System.Numerics;
using XrMath;

namespace XrEditor.Services
{
    public class PropertyEditorManager
    {
        readonly IList<IPropertyEditorFactory> _factories = [];


        public PropertyEditorManager()
        {
            Register(new TypedPropertyEditorFactory<Vector3, Vector3Editor>());
            Register(new TypedPropertyEditorFactory<Color, ColorEditor>());
            Register(new TypedPropertyEditorFactory<bool, BoolEditor>());
            Register(new TypedPropertyEditorFactory<string, TextEditor>());
            Register(new FloatEditorFactory());
            Register(new EnumEditorFactory());
            Register(new EngineObjectEditorFactory());
        }

        public IPropertyEditor? CreateEditor(Type type, IEnumerable<Attribute> attributes)
        {
            var factory = _factories.FirstOrDefault(a => a.CanHandle(type));
            return factory?.CreateEditor(type, attributes);
        }

        public void Register(IPropertyEditorFactory factory)
        {
            _factories.Add(factory);
        }

    }
}
