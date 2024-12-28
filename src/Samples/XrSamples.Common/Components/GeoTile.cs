using Common.Interop;
using DotSpatial.Projections;

using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.Tiff;
using XrMath;

namespace XrSamples
{
    public class GeoTile : TriangleMesh, IDrawGizmos
    {
        public GeoTile()
        {
            EarthRadius = 6378137f;
            OffsetX = MathF.PI / 2;
        }

        Vector3 ToCartesian(Vector2 latLng)
        {
            var lonRad = (latLng.X * MathF.PI / 180.0f) + OffsetX;
            var latRad = (latLng.Y * MathF.PI / 180.0f) + OffsetY;

            // Calculate Cartesian coordinates
            var x = EarthRadius * MathF.Cos(latRad) * MathF.Sin(lonRad); // Horizontal left-right
            var y = EarthRadius * MathF.Sin(latRad);                    // Vertical (up-down)
            var z = EarthRadius * MathF.Cos(latRad) * MathF.Cos(lonRad); // Forward/backward

            return new Vector3(x, y, z);
        }

        public unsafe void LoadGeoTiff(string path)
        {
            var tiff = LibTiff.TIFFOpen(path, "r");

            var texData = tiff.Read();

            if (texData.Format == TextureFormat.GrayRawSInt16)
            {
                texData.Data = ImageUtils.ConvertShortToFloat(texData.Data!);    
                texData.Format = TextureFormat.GrayFloat32; 
            }

            var w = texData.Width;
            var h = texData.Height;

            HeightMap = Texture2D.FromData([texData]);

            var tie = tiff.GetDoubleArrayField(LibTiff.TiffTag.GeoTiePoints);
            var scale = tiff.GetDoubleArrayField(LibTiff.TiffTag.GeoPixelScale);
            var trans = tiff.GetDoubleArrayField(LibTiff.TiffTag.GeoTransMatrix);
            var noData = tiff.GetStringField(LibTiff.TiffTag.GdalNoData);
            var meta = tiff.GetStringField(LibTiff.TiffTag.GdalMetadata);

            var dir = LibTiff.GetGeoDirectory(tiff);

            tiff.TIFFClose();

            var model = (LibTiff.GeoModelType)(ushort)dir.First(a => a.Key == LibTiff.GeoTag.GTModelTypeGeoKey).Value!;

            if (model == LibTiff.GeoModelType.Geographic)
            {
                NorthEast = new Vector2()
                {
                    X = (float)tie[3],
                    Y = (float)tie[4]
                };

                NorthWest = new Vector2()
                {
                    X = NorthEast.X + (float)scale[0] * w,
                    Y = NorthEast.Y
                };

                SouthEast = new Vector2()
                {
                    X = NorthEast.X,
                    Y = NorthEast.Y - (float)scale[1] * h,
                };

                SouthWest = new Vector2
                {
                    X = NorthWest.X,
                    Y = SouthEast.Y,
                };

            }
            else
            {
                var code = (ushort)dir.First(a => a.Key == LibTiff.GeoTag.ProjectedCSTypeGeoKey).Value!;

                if (code != 4326)
                {
                    var source = ProjectionInfo.FromEpsgCode(code);
                    var target = ProjectionInfo.FromEpsgCode(4326);

                    double[] pt = [
                            tie[3], tie[4],
                            tie[3] + scale[0] * w, tie[4],
                            tie[3], tie[4] - scale[1] * h,
                            tie[3] + scale[0] * w, tie[4] - scale[1] * h,
                    ];

                    Reproject.ReprojectPoints(pt, [0, 0, 0, 0], source, target, 0, 4);

                    NorthEast = new((float)pt[0], (float)pt[1]);
                    NorthWest = new((float)pt[2], (float)pt[3]);
                    SouthEast = new((float)pt[4], (float)pt[5]);
                    SouthWest = new((float)pt[6], (float)pt[7]);
                }
            }

            var p0 = ToCartesian(NorthEast);
            var p1 = ToCartesian(NorthWest);
            var p2 = ToCartesian(SouthEast);
            var p3 = ToCartesian(SouthWest);

            var ww = (p1 - p0).Length();
            var hh = (p2 - p0).Length();

            var center = (p0 + p1 + p2 + p3) / 4f;
            var zAxis = center.Normalize();
            var xAxis = Vector3.Normalize(p1 - p0);
            var yAxis = Vector3.Cross(zAxis, xAxis);

            var rotationMatrix = new Matrix4x4(
                 xAxis.X, yAxis.X, zAxis.X, 0,
                 xAxis.Y, yAxis.Y, zAxis.Y, 0,
                 xAxis.Z, yAxis.Z, zAxis.Z, 0,
                 0, 0, 0, 1
             );

            var orientation = Quaternion.CreateFromRotationMatrix(rotationMatrix);

            Transform.Orientation = new Quaternion(orientation.X, orientation.Y, orientation.Z, -orientation.W);
            Transform.Position = center;

            Geometry = new QuadPatch3D(new Vector2(ww, hh), 100);
         
            var pbr = MaterialFactory.CreatePbr("#ffffff");

            if (Roughness != null)
                pbr.MetallicRoughnessMap = ImageUtils.MergeMetalRaugh(Roughness);

            
            if (pbr is IHeightMaterial hm)
            {
                hm.HeightMap = new HeightMapSettings()
                {
                    Texture = HeightMap,
                    ScaleFactor = 0.0001f,
                    TargetTriSize = 5,
                    NormalStrength = 1f,
                    NormalMode = HeightNormalMode.Sobel,
                    MaskValue = -9999,
                    SphereRadius = EarthRadius
                };

                HeightMap.WrapS = WrapMode.ClampToEdge;
                HeightMap.WrapT = WrapMode.ClampToEdge;
                HeightMap.MagFilter = ScaleFilter.Linear;
                HeightMap.MinFilter = ScaleFilter.Linear;

                if (texData.Format == TextureFormat.GrayRawSInt16)
                {
                    HeightMap.MagFilter = ScaleFilter.Nearest;
                    HeightMap.MinFilter = ScaleFilter.Nearest;
                }
          
            }

            Materials.Add((Material)pbr);
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            var p0 = ToCartesian(NorthEast);
            var p1 = ToCartesian(NorthWest);
            var p2 = ToCartesian(SouthEast);
            var p3 = ToCartesian(SouthWest);

            canvas.State.Color = "#ffff00";

            canvas.DrawLine(p0, p1);
            canvas.DrawLine(p2, p3);
            canvas.DrawLine(p0, p2);
            canvas.DrawLine(p1, p3);


            var center = (p0 + p1 + p2 + p3) / 4f;
            var zAxis = center.Normalize();
            var xAxis = Vector3.Normalize(p1 - p0);
            var yAxis = Vector3.Cross(zAxis, xAxis);

            canvas.State.Color = "#ff0000";
            canvas.DrawLine(center, center + xAxis * 0.1f);
            canvas.State.Color = "#00ff00";
            canvas.DrawLine(center, center + yAxis * 0.1f);
            canvas.State.Color = "#0000ff";
            canvas.DrawLine(center, center + zAxis * 0.1f);

        }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float OffsetX { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float OffsetY { get; set; }

        public Vector2 NorthEast { get; set; }

        public Vector2 SouthWest { get; set; }

        public Vector2 NorthWest { get; set; }

        public Vector2 SouthEast { get; set; }

        public Texture2D? HeightMap { get; set; }

        public Texture2D? Roughness { get; set; }

        public float EarthRadius { get; set; }

        bool IDrawGizmos.IsEnabled => true;
    }
}
