using Newtonsoft.Json.Linq;
using System.Numerics;
using UI.Binding;
using XrEngine.OpenGL;

namespace XrEditor
{
    public class BoolEditor : BaseEditor<bool, bool>
    {
        public BoolEditor()
        {
        }

        public BoolEditor(IProperty<bool> binding)
        {
            Binding = binding;
        }

    }
}
