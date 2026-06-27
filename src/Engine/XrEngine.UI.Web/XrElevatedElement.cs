using System;
using System.Collections.Generic;
using System.Text;
using XrMath;

namespace XrEngine.UI.Web
{
    public class XrElevatedElement
    {
        public string? Id { get; set; }

        public string? Tag { get; set; }

        public Rect2 TextureRect { get; set; }

        public float Elevation { get; set; }

        public string Background { get; set; }

        public float Opacity { get; set; }

        public XrElevatedElement()
        {
            Background = "#000000";
            Opacity = 1f;
        }
    }
}
