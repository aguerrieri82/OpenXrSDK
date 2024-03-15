using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Interaction;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrInputPointerDevice : Behavior<Object3D>, IPointerEventSource
    {
        public void CapturePointer()
        {
            throw new NotImplementedException();
        }

        public Ray3 GetRay()
        {
            throw new NotImplementedException();
        }

        public void ReleasePointer()
        {
            throw new NotImplementedException();
        }

        public XrPoseInput? PoseInput { get; set; }

        public XrBoolInput? LeftButton { get; set; }

        public XrBoolInput? RightButton { get; set; }

        public event PointerEventDelegate? PointerDown;
        
        public event PointerEventDelegate? PointerUp;
        
        public event PointerEventDelegate? PointerMove;

        public event PointerEventDelegate? WheelMove;

    }
}
