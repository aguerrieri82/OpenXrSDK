using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Components;
using XrMath;

namespace XrEngine.UI
{
    public class UiButton : UiContentView
    {


        protected override void OnPointerUp(UiPointerEvent ev)
        {
            Click?.Invoke(this, EventArgs.Empty);
            base.OnPointerUp(ev);
        }


        public event EventHandler? Click;
    }
}
