using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor.Abstraction
{
    public interface IEditorUIElementContainer
    {
        IEditorUIElement? UIElement { get; set; }
    }
}
