using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace CanvasUI
{
    public interface IUiWindow
    {
        public void Close();

        public UiElement? Content { get; set; }

        public Size2 Size { get; set; }
        
        public Vector3 Position { get; set; }
    }
}
