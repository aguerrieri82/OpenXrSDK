using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI.Objects
{
    public struct UiDefaultPointer : IUiPointer
    {
        public UiDefaultPointer(int id)
        {
            Id = id;
        }

        public void Capture(UiElement element)
        {
            UiManager.SetPointerCapture(Id, element);   
        }

        public void Release()
        {
            UiManager.SetPointerCapture(Id, null);
        }

        public int Id { get; }

    }
}
