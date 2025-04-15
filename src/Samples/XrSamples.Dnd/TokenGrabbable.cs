using System;
using System.Collections.Generic;
using System.Text;
using XrEngine.OpenXr;

namespace XrSamples.Dnd
{
    public class TokenGrabbable : BoundsGrabbable
    {

        public override void Release()
        {
            ((Token)_host!).SendPosition();
            base.Release();
        }
    }
}
