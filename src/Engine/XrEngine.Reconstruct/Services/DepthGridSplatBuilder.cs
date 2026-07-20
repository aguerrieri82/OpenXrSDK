using Common.Interop;
using System.Numerics;

namespace XrEngine.Reconstruct
{
    public unsafe static class DepthGridSplatBuilder
    {
        private static readonly IMemoryBuffer<byte> _tempRgba = MemoryBuffer.Create<byte>(16);

        public static void CreateSplats(
             List<SplatData> result,
             Geometry3D geometry,
             IMemoryBuffer<byte> colorRgba,
             int colorWidth,
             int colorHeight,
             int blurRadius = 1,
             float alpha = 1.0f)
        {
            var byteSize = colorWidth * colorHeight * 4;

            if (_tempRgba.Size < byteSize)
                _tempRgba.Allocate((uint)byteSize);

            var vertices = geometry.Vertices;
            var indices = geometry.Indices;

            var used = new bool[vertices.Length];

            for (var i = 0; i < indices.Length; i++)
                used[(int)indices[i]] = true;

            if (blurRadius > 0)
                BlurHorizontal(colorRgba, _tempRgba, colorWidth, colorHeight, blurRadius);

            using var colorLock = colorRgba.MemoryLock();
            using var tempLock = _tempRgba.MemoryLock();

            var pColor = colorLock.Data;
            var pTemp = tempLock.Data;

            for (var i = 0; i < vertices.Length; i++)
            {
                if (!used[i])
                    continue;

                var v = vertices[i];

                var color = blurRadius <= 0
                    ? SampleColorNearest(
                        pColor,
                        colorWidth,
                        colorHeight,
                        v.UV
                    )
                    : SampleColorNearestVerticalBlur(
                        pTemp,
                        colorWidth,
                        colorHeight,
                        v.UV,
                        blurRadius
                    );

                color.W *= alpha;

                var normal = Vector3.Normalize(v.Normal);

                var refAxis = MathF.Abs(normal.Z) < 0.95f
                    ? Vector3.UnitZ
                    : Vector3.UnitX;

                var axisX = Vector3.Normalize(Vector3.Cross(refAxis, normal));
                var axisY = Vector3.Cross(normal, axisX);

                if (!float.IsFinite(axisX.X) || !float.IsFinite(axisX.Y) || !float.IsFinite(axisX.Z) ||
                    !float.IsFinite(axisY.X) || !float.IsFinite(axisY.Y) || !float.IsFinite(axisY.Z))
                    continue;

                result.Add(new SplatData
                {
                    Position = v.Pos,
                    AxisX = new Vector4(axisX, 0.0f),
                    AxisY = new Vector4(axisY, 0.0f),
                    Color = color
                });
            }
        }

        private static Vector4 SampleColorNearest(
            byte* rgba,
            int width,
            int height,
            Vector2 uv)
        {
            var x = (int)MathF.Round(uv.X * (width - 1));
            var y = (int)MathF.Round(uv.Y * (height - 1));

            var p = (y * width + x) * 4;

            return new Vector4(
                rgba[p + 0] / 255.0f,
                rgba[p + 1] / 255.0f,
                rgba[p + 2] / 255.0f,
                rgba[p + 3] / 255.0f
            );
        }

        private static Vector4 SampleColorNearestVerticalBlur(
            byte* rgba,
            int width,
            int height,
            Vector2 uv,
            int radius)
        {
            var x = (int)MathF.Round(uv.X * (width - 1));
            var y = (int)MathF.Round(uv.Y * (height - 1));

            var diameter = radius * 2 + 1;

            var r = 0;
            var g = 0;
            var b = 0;
            var a = 0;

            for (var k = -radius; k <= radius; k++)
            {
                var sy = Math.Clamp(y + k, 0, height - 1);
                var p = (sy * width + x) * 4;

                r += rgba[p + 0];
                g += rgba[p + 1];
                b += rgba[p + 2];
                a += rgba[p + 3];
            }

            return new Vector4(
                r / (diameter * 255.0f),
                g / (diameter * 255.0f),
                b / (diameter * 255.0f),
                a / (diameter * 255.0f)
            );
        }

        private static void BlurHorizontal(
            IMemoryBuffer<byte> src,
            IMemoryBuffer<byte> dst,
            int width,
            int height,
            int radius)
        {
            using var srcLock = src.MemoryLock();
            using var dstLock = dst.MemoryLock();

            var pSrc = srcLock.Data;
            var pDst = dstLock.Data;

            var diameter = radius * 2 + 1;

            for (var y = 0; y < height; y++)
            {
                var row = y * width * 4;

                for (var x = 0; x < width; x++)
                {
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    var a = 0;

                    for (var k = -radius; k <= radius; k++)
                    {
                        var sx = Math.Clamp(x + k, 0, width - 1);
                        var p = row + sx * 4;

                        r += pSrc[p + 0];
                        g += pSrc[p + 1];
                        b += pSrc[p + 2];
                        a += pSrc[p + 3];
                    }

                    var d = row + x * 4;

                    pDst[d + 0] = (byte)(r / diameter);
                    pDst[d + 1] = (byte)(g / diameter);
                    pDst[d + 2] = (byte)(b / diameter);
                    pDst[d + 3] = (byte)(a / diameter);
                }
            }
        }
    }
}