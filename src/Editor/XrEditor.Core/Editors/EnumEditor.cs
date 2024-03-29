using Newtonsoft.Json.Linq;
using System.Numerics;
using UI.Binding;
using XrEngine.OpenGL;

namespace XrEditor
{
    public interface IEnumEditor
    {

    }

    public class EnumEditor<T> : BaseEditor<T, T>, IEnumEditor where T: struct, Enum
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
}
