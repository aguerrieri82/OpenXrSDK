using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public interface IUiPointer
    {
        int Id { get; }

        void Capture(UiElement element);

        void Release();
    }
}
