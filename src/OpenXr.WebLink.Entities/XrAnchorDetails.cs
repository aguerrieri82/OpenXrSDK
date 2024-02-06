using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink.Entities
{
    public class XrAnchorDetails
    {
        public Guid Id { get; set; }

        public IList<string>? Labels { get; set; }

        public Rect2f? Bounds2D { get; set; }

        public Posef? Pose { get; set; }

        public Mesh? Mesh { get; set; }
    }
}
