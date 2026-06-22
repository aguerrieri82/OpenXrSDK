using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public interface ISelectionHandler
    {
        void OnSelected(Object3D obj, bool isSelected);
    }
}
