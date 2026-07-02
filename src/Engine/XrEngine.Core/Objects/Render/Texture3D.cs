using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine
{
    public class Texture3D : Texture2D
    {
        protected override void InitSampler()
        {
            if (WrapR == 0)
                WrapR = WrapMode.ClampToEdge;

            base.InitSampler();
        }

        public WrapMode WrapR { get; set; }
    }
}
