using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public interface IEditorProperties
    {
        IList<PropertyView> EditorProperties();
    }
}
