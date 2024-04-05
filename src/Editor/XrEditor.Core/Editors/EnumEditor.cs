using UI.Binding;

namespace XrEditor
{
    public interface IEnumEditor
    {

    }

    public class EnumEditor<T> : BaseEditor<T, T>, IEnumEditor where T : struct, Enum
    {
        public EnumEditor()
        {
        }

        public EnumEditor(IProperty<T> binding)
        {
            Binding = binding;
        }


        public T[] Values => Enum.GetValues<T>();
    }

    public struct EnumEditorFactory : IPropertyEditorFactory
    {
        public bool CanHandle(Type type)
        {
            return type.IsEnum;
        }

        public IPropertyEditor CreateEditor(Type type)
        {
            return (IPropertyEditor)Activator.CreateInstance(typeof(EnumEditor<>).MakeGenericType(type))!;
        }
    }
}
