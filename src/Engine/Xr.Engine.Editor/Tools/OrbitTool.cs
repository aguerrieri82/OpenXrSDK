using OpenXr.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Xr.Engine.Editor.Tools
{
    public class OrbitTool : BaseMouseTool
    {
        public OrbitTool()
        {

        }
        public Vector3 Target { get; set; }

    }
}
