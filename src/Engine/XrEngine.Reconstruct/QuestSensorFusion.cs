using System.Numerics;
using XrEngine.Devices;

namespace XrEngine.Reconstruct
{
    public class QuestSensorFusion
    {

        private readonly float _fx, _fy, _cx, _cy;

        private Matrix4x4 _headToRgbPose;

        public QuestSensorFusion(CameraParams camera)
        {
            _fx = camera.Intrinsic![0];
            _fy = camera.Intrinsic![1];
            _cx = camera.Intrinsic![2];
            _cy = camera.Intrinsic![3];

            _headToRgbPose = Matrix4x4.CreateFromQuaternion(camera.Rotation!.Value) *
                             Matrix4x4.CreateTranslation(camera.Position!.Value);

        }

        public unsafe byte[] AlignColorToDepth(
            Span<Vector3> depthPixels, int depthW, int depthH,
            Matrix4x4 depthView, Matrix4x4 depthProj,
            Span<byte> colorPixels, int colorW, int colorH,
            Matrix4x4 leftView,
            Matrix4x4 headPose,
            float fovScale,
            Vector2 centerOfs)
        {
            var result = new byte[depthW * depthH * 3];



            Matrix4x4.Invert(_headToRgbPose * headPose, out var rgbView);


            fixed (Vector3* pDepth = depthPixels)
            fixed (byte* pColor = colorPixels)
            fixed (byte* pResult = result)
            {
                var dstIndex = 0;

                var wInv = 1.0f / depthW;
                var hInv = 1.0f / depthH;

                for (var y = 0; y < depthH; y++)
                {
                    for (var x = 0; x < depthW; x++)
                    {
                        var worldPos = pDepth[dstIndex];

                        var ptColor = Vector3.Transform(worldPos, rgbView);

                        if (ptColor.Z <= 0.05f)
                        {
                            dstIndex++;

                            continue;
                        }

                        var invZ = 1.0f / ptColor.Z;

                        var c_u = (ptColor.X * invZ) * _fx * fovScale + _cx + centerOfs.X;
                        var c_v = (ptColor.Y * invZ) * _fy * fovScale + _cy + centerOfs.Y;
                        var cX = (int)c_u;
                        var cY = (int)c_v;

                        var dstIdx = dstIndex * 3;

                        if (cX >= 0 && cX < colorW && cY >= 0 && cY < colorH)
                        {
                            var srcIdx = (cY * colorW + cX) * 3;
                            pResult[dstIdx] = pColor[srcIdx];
                            pResult[dstIdx + 1] = pColor[srcIdx + 1];
                            pResult[dstIdx + 2] = pColor[srcIdx + 2];
                        }

                        dstIndex++;

                    }

                }
            }
            return result;
        }
    }

}