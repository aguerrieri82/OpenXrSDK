using Common.Interop;
using System.Numerics;
using System.Runtime.CompilerServices;
using XrEngine;

namespace XrSamples.Graffiti.Objects
{
    public class BrickGeometry : Geometry3D, IGeneratedContent
    {
        public BrickGeometry()
        {
            WallSize = new Vector2(2.0f, 2.0f);
            Offset = Vector2.Zero;

            BrickSize = new Vector2(0.25f, 0.065f);
            OddRowOffset = BrickSize.X * 0.5f;

            MortarSize = new Vector2(0.002f, 0.002f);

            Depth = 0.003f;
            MortarSkew = 0.003f;

            Build();
        }

        private bool IsValid()
        {
            if (WallSize.X <= 0.0f || WallSize.Y <= 0.0f)
                return false;

            if (BrickSize.X <= 0.0f || BrickSize.Y <= 0.0f)
                return false;

            if (MortarSize.X < 0.0f || MortarSize.Y < 0.0f)
                return false;

            if (BrickSize.X + MortarSize.X <= 0.0f)
                return false;

            if (BrickSize.Y + MortarSize.Y <= 0.0f)
                return false;

            if (Depth < 0.0f)
                return false;

            return true;
        }

        public unsafe TextureData BuildDensityTexture(
             float texelSize,
             float densityToHeightScale)
        {
            if (texelSize <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(texelSize));
            /*
            if (densityToHeightScale <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(densityToHeightScale));
            */
            if (!IsValid())
                return new TextureData();

            var pitch = BrickSize + MortarSize;
            var tileSize = new Vector2(pitch.X, pitch.Y * 2.0f);

            var width = Math.Max(
                1u,
                (uint)MathF.Ceiling(tileSize.X / texelSize));

            var height = Math.Max(
                1u,
                (uint)MathF.Ceiling(tileSize.Y / texelSize));

            var byteSize = width * height * sizeof(float);
            var buffer = MemoryBuffer.Create<byte>(byteSize);

            var mortarDensity = -Depth / densityToHeightScale;

            var brickW = BrickSize.X;
            var brickH = BrickSize.Y;

            var skew = MathF.Abs(MortarSkew);
            skew = MathF.Min(skew, MathF.Min(brickW, brickH) * 0.49f);

            using (var mem = buffer.MemoryLock())
            {
                var dst = (float*)(byte*)mem;

                for (var y = 0; y < height; y++)
                {
                    var py = (y + 0.5f) * texelSize;

                    var row = (int)MathF.Floor(py / pitch.Y);
                    var localY = py - row * pitch.Y;

                    var rowOffset = (row & 1) != 0
                        ? OddRowOffset
                        : 0.0f;

                    var rowPtr = dst + y * width;

                    for (var x = 0; x < width; x++)
                    {
                        var px = (x + 0.5f) * texelSize;

                        var localX = px - rowOffset;

                        localX %= pitch.X;
                        if (localX < 0.0f)
                            localX += pitch.X;

                        var density = mortarDensity;

                        if (localX >= 0.0f &&
                            localY >= 0.0f &&
                            localX < brickW &&
                            localY < brickH)
                        {
                            density = 0.0f;

                            if (skew > 0.0f)
                            {
                                var edgeDist = MathF.Min(
                                    MathF.Min(localX, brickW - localX),
                                    MathF.Min(localY, brickH - localY));

                                if (edgeDist < skew)
                                {
                                    var t = edgeDist / skew;

                                    // edgeDist = 0     -> mortarDensity
                                    // edgeDist = skew  -> 0
                                    density = mortarDensity * (1.0f - t);
                                }
                            }
                        }

                        rowPtr[x] = density;
                    }
                }
            }

            return new TextureData
            {
                Width = width,
                Height = height,
                Depth = 1,
                MipLevel = 0,
                Layer = 0,
                Format = TextureFormat.GrayFloat32,
                Compression = TextureCompressionFormat.Uncompressed,
                Data = buffer,
                BlockSize = 0
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float PositiveMod(float x, float m)
        {
            var r = x % m;
            return r < 0.0f ? r + m : r;
        }

        public void Build()
        {
            var builder = new MeshBuilder();

            if (!IsValid())
            {
                Vertices = [];
                Indices = [];
                return;
            }

            var wallMin = -WallSize * 0.5f;
            var wallMax = WallSize * 0.5f;
            var pitch = BrickSize + MortarSize;

            Vector2 ToUv(Vector2 p)
            {
                return new Vector2(
                    (p.X - wallMin.X) / WallSize.X,
                    (p.Y - wallMin.Y) / WallSize.Y);
            }

            void AddMortarPlane()
            {
                var p00 = new Vector2(wallMin.X, wallMin.Y);
                var p10 = new Vector2(wallMax.X, wallMin.Y);
                var p11 = new Vector2(wallMax.X, wallMax.Y);
                var p01 = new Vector2(wallMin.X, wallMax.Y);

                builder.AddFace(
                    new Vector3(p00.X, p00.Y, 0.0f),
                    new Vector3(p10.X, p10.Y, 0.0f),
                    new Vector3(p11.X, p11.Y, 0.0f),
                    new Vector3(p01.X, p01.Y, 0.0f),
                    ToUv(p00),
                    ToUv(p10),
                    ToUv(p11),
                    ToUv(p01));
            }

            void AddBrick(Vector2 min, Vector2 max)
            {
                var x0 = min.X;
                var y0 = min.Y;
                var x1 = max.X;
                var y1 = max.Y;

                var width = x1 - x0;
                var height = y1 - y0;

                if (width <= 0.0f || height <= 0.0f)
                    return;

                if (Depth <= 0.0f)
                    return;

                var skew = MathF.Abs(MortarSkew);
                skew = MathF.Min(skew, MathF.Min(width, height) * 0.49f);

                var z0 = 0.0f;
                var z1 = Depth;

                var b00 = new Vector3(x0, y0, z0);
                var b10 = new Vector3(x1, y0, z0);
                var b11 = new Vector3(x1, y1, z0);
                var b01 = new Vector3(x0, y1, z0);

                var t00 = new Vector3(x0 + skew, y0 + skew, z1);
                var t10 = new Vector3(x1 - skew, y0 + skew, z1);
                var t11 = new Vector3(x1 - skew, y1 - skew, z1);
                var t01 = new Vector3(x0 + skew, y1 - skew, z1);

                var uvB00 = ToUv(new Vector2(b00.X, b00.Y));
                var uvB10 = ToUv(new Vector2(b10.X, b10.Y));
                var uvB11 = ToUv(new Vector2(b11.X, b11.Y));
                var uvB01 = ToUv(new Vector2(b01.X, b01.Y));

                var uvT00 = ToUv(new Vector2(t00.X, t00.Y));
                var uvT10 = ToUv(new Vector2(t10.X, t10.Y));
                var uvT11 = ToUv(new Vector2(t11.X, t11.Y));
                var uvT01 = ToUv(new Vector2(t01.X, t01.Y));

                builder.AddFace(
                    t00, t10, t11, t01,
                    uvT00, uvT10, uvT11, uvT01);

                builder.AddFace(
                    b00, b10, t10, t00,
                    uvB00, uvB10, uvT10, uvT00);

                builder.AddFace(
                    b10, b11, t11, t10,
                    uvB10, uvB11, uvT11, uvT10);

                builder.AddFace(
                    b11, b01, t01, t11,
                    uvB11, uvB01, uvT01, uvT11);

                builder.AddFace(
                    b01, b00, t00, t01,
                    uvB01, uvB00, uvT00, uvT01);
            }

            AddMortarPlane();

            var firstRow = (int)MathF.Floor((wallMin.Y - Offset.Y - BrickSize.Y) / pitch.Y) - 1;
            var lastRow = (int)MathF.Ceiling((wallMax.Y - Offset.Y) / pitch.Y) + 1;

            for (var row = firstRow; row <= lastRow; row++)
            {
                var y0 = Offset.Y + row * pitch.Y;
                var y1 = y0 + BrickSize.Y;

                if (y1 <= wallMin.Y || y0 >= wallMax.Y)
                    continue;

                var cy0 = MathF.Max(y0, wallMin.Y);
                var cy1 = MathF.Min(y1, wallMax.Y);

                var rowOffset = (row & 1) != 0
                    ? OddRowOffset
                    : 0.0f;

                var xOffset = Offset.X + rowOffset;

                var firstCol = (int)MathF.Floor((wallMin.X - xOffset - BrickSize.X) / pitch.X) - 1;
                var lastCol = (int)MathF.Ceiling((wallMax.X - xOffset) / pitch.X) + 1;

                for (var col = firstCol; col <= lastCol; col++)
                {
                    var x0 = xOffset + col * pitch.X;
                    var x1 = x0 + BrickSize.X;

                    if (x1 <= wallMin.X || x0 >= wallMax.X)
                        continue;

                    var cx0 = MathF.Max(x0, wallMin.X);
                    var cx1 = MathF.Min(x1, wallMax.X);

                    AddBrick(
                        new Vector2(cx0, cy0),
                        new Vector2(cx1, cy1));
                }
            }

            builder.ToGeometry(this);

            for (var i = 0; i < Vertices.Length; i++)
                Vertices[i].UV.Y = 1 - Vertices[i].UV.Y;

            this.ComputeTangents();
        }

        [Range(0, 0.5f, 0.001f)]
        public float Depth { get; set; }

        [Range(0, 0.1f, 0.001f)]
        public float MortarSkew { get; set; }

        public Vector2 WallSize { get; set; }

        public Vector2 Offset { get; set; }

        public Vector2 BrickSize { get; set; }

        [Range(0, 1, 0.01f)]
        public float OddRowOffset { get; set; }

        public Vector2 MortarSize { get; set; }

        public Vector2 DensityTileSize => new(
            BrickSize.X + MortarSize.X,
            (BrickSize.Y + MortarSize.Y) * 2.0f);

    }
}
