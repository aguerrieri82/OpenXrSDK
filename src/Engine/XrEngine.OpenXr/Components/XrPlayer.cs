using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.OpenXr
{
    public class XrPlayer : Behavior<Object3D>
    {

        protected override void Update(RenderContext ctx)
        {
            if (XrApp.Current == null)
                return;
            XrApp.Current.ReferenceFrame = _host!.GetWorldPose();
        }

    }
}
