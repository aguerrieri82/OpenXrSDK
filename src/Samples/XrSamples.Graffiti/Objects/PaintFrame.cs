
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine;

namespace XrSamples.Graffiti
{
    public class PaintFrame : TriangleMesh
    {
        public PaintFrame(Vector2 size, Material material)
        {
            Build(size, new Vector2(0.02f, 0.01f));
            Materials.Add(material);
        }

        public void Build(Vector2 size, Vector2 profile)
        {
            var builder = new MeshBuilder();

            var width = size.X + profile.X * 2;
            var height = size.Y + profile.X * 2;

            var thickness = profile.X;
            var depth = profile.Y;

            var halfW = width * 0.5f;
            var halfH = height * 0.5f;
            var halfT = thickness * 0.5f;

            // top
            builder.AddCube(
                new Vector3(0, halfH - halfT, 0),
                new Vector3(width, thickness, depth));

            // bottom
            builder.AddCube(
                new Vector3(0, -halfH + halfT, 0),
                new Vector3(width, thickness, depth));

            // left
            builder.AddCube(
                new Vector3(-halfW + halfT, 0, 0),
                new Vector3(thickness, height, depth));

            // right
            builder.AddCube(
                new Vector3(halfW - halfT, 0, 0),
                new Vector3(thickness, height, depth));

            Geometry = builder.ToGeometry(Geometry);

            Size = size;
            Profile = profile;
        }


        [Action]
        public void Build()
        {
            Build(Size, Profile);
        }

        public Vector2 Size { get; set; }

        public Vector2 Profile { get; set; }
    }
}
