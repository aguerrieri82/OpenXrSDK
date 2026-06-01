
using System;
using System.Collections.Generic;
using System.Numerics;

using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples.Graffiti
{
    public class SprayBrush : TriangleMesh
    {

        public SprayBrush(int radialSubs, int innerSubs)
        {
     
            var builder = new MeshBuilder();

            float radius = 0.5f;

            float iStep = 1f / (innerSubs) * radius;

            float jStep = 2 * MathF.PI / radialSubs;

            Vector2 ToUv(Vector3 pos) => new Vector2(pos.X + 0.5f, pos.Y + 0.5f);

            for (int i = 0; i < innerSubs; i++)
            {
                float r1 = radius - (iStep * i);
                float r2 = radius - (iStep * (i + 1));

                for (int j = 0; j < radialSubs; j++)
                {
                    float a1 = jStep * j;
                    float a2 = jStep * (j + 1);

                    var p1 = new Vector3(r1 * MathF.Cos(a1), r1 * MathF.Sin(a1), 0);
                    var p2 = new Vector3(r1 * MathF.Cos(a2), r1 * MathF.Sin(a2), 0);
       

                    if (i == innerSubs - 1)
                        builder.AddTriangle(p1, p2, Vector3.Zero, ToUv(p1), ToUv(p2), ToUv(Vector3.Zero));
                    else
                    {
                        var p3 = new Vector3(r2 * MathF.Cos(a1), r2 * MathF.Sin(a1), 0);
                        var p4 = new Vector3(r2 * MathF.Cos(a2), r2 * MathF.Sin(a2), 0);
                        builder.AddFace(p1, p2, p4, p3, ToUv(p1), ToUv(p2), ToUv(p4), ToUv(p3));
                    }
                 }
            }

            var texture = Context.Require<IAssetStore>().GetPath("check.png");


            Geometry = builder.ToGeometry();
            Materials.Add(new WireframeMaterial() { Color = new Color(0,1,0), DoubleSided = true });
            // Materials.Add(new TextureMaterial(Texture2D.FromImage(texture)));

            IsVisible = XrPlatform.IsEditor;
        }

    }
}
