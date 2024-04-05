using System.Numerics;
using XrEditor.Services;
using XrMath;

namespace XrEditor
{
    public class PropertyEditorManager
    {
        readonly IList<IPropertyEditorFactory> _factories = [];


        public PropertyEditorManager()
        {
            Register(new TypedPropertyEditorFactory<float, FloatEditor>());
            Register(new TypedPropertyEditorFactory<Vector3, Vector3Editor>());
            Register(new TypedPropertyEditorFactory<Color, ColorEditor>());
            Register(new TypedPropertyEditorFactory<bool, BoolEditor>());
            Register(new EnumEditorFactory());
            Register(new EngineObjectEditorFactory());
        }

        public IPropertyEditor? CreateEditor(Type type)
        {
            var factory = _factories.FirstOrDefault(a => a.CanHandle(type));
            return factory?.CreateEditor(type);
        }

        public void Register(IPropertyEditorFactory factory)
        {
            _factories.Add(factory);
        }

    }
}
