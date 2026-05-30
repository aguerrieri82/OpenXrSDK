using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class PaintLayer
    {
        public float TexelSize;

        public float Dryness;

        //Density / seconds
        public float DryTime;

        public Texture2D? Texture;

        public Quad3D? Quad;    
    }

    public class PaintCanvas : Group3D
    {
        readonly IList<PaintLayer> _layers = [];

        public PaintCanvas(Quad3 quad, float texelSize = 1, int layers = 3)
        {

        }

        public IList<PaintLayer> Layers => _layers; 
    }
}
