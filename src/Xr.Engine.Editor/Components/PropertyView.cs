using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Editor
{
    public class PropertyView : BaseView
    {

        public PropertyView()
        {

        }

        public string? Label { get; set; }   

        public IPropertyEditor? Editor { get; set; } 
    }
}
