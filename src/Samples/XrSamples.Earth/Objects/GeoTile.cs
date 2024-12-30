
using Common.Interop;
using DotSpatial.Projections;

using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Tensorflow;
using XrEngine;
using XrEngine.Tiff;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class GeoTile : TriangleMesh, IDrawGizmos
    {
        public GeoTile()
        {
            SphereRadius = 6378137f;
            OffsetX = 0;
        }

        Vector3 ToCartesian(Vector2 latLng)
        {
            var lonRad = (latLng.X * MathF.PI / 180.0f) + OffsetX;
            var latRad = (latLng.Y * MathF.PI / 180.0f) + OffsetY;

            var x = SphereRadius * MathF.Cos(latRad) * MathF.Sin(lonRad);
            var y = SphereRadius * MathF.Sin(latRad);                    
            var z = SphereRadius * MathF.Cos(latRad) * MathF.Cos(lonRad);

            return new Vector3(x, y, z);
        }

        Vector2 ToUv(Vector2 latLng)
        {
            var normal = ToCartesian(latLng).Normalize();

            var u = (0.25f + (float)(Math.Atan2(normal.Z, normal.X) / (2 * Math.PI))) % 1;
            var v = (0.5f - (float)(Math.Asin(normal.Y) / Math.PI)) % 1;

            return new Vector2(1 - u, v);
        }

        public void LoadAlbedoSlice(Texture2D tex)
        {
            Geometry!.ActiveComponents |= VertexComponent.UV1;

            var mat = (IPbrMaterial)Materials[0];

            mat.ColorMapUVSet = 1;
            mat.ColorMap = tex;

            var p0 = ToUv(NorthEast);
            var p1 = ToUv(NorthWest);
            var p2 = ToUv(SouthEast);
            var p3 = ToUv(SouthWest);

            var len = Geometry!.Vertices.Length;


            var trans = Matrix3x2.CreateScale(p1.X - p0.X, p2.Y - p0.Y) * 
                        Matrix3x2.CreateTranslation(p0.X, p0.Y);           


            for (var i = 0; i < len; i++)
            {
                var uv0 = Geometry!.Vertices[i].UV;

                Geometry!.Vertices[i].UV1 = Vector2.Transform(uv0, trans);
            }

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
            pbr.Roughness = 1f;

            if (Roughness != null)
                pbr.MetallicRoughnessMap = ImageUtils.MergeMetalRaugh(Roughness);

            pbr.ColorMap = Color;   

            if (pbr is IHeightMaterial hm)
            {
                hm.HeightMap = new HeightMapSettings()
                {
                    Texture = HeightMap,
                    ScaleFactor = Unit(0.001f),
                    TargetTriSize = 5,
                    NormalStrength = new Vector3(1000, -1000, -0.3f),
                    NormalMode = HeightNormalMode.Fast,
                    MaskValue = -9999,
                    SphereRadius = SphereRadius,
                    SphereWorldCenter = SphereWorldCenter
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

        public override void Update(RenderContext ctx)
        {
            if (Materials.Count > 0 && Materials[0] is IHeightMaterial hm)
            {
                hm.HeightMap!.SphereWorldCenter = SphereWorldCenter;
                hm.HeightMap!.SphereRadius = SphereRadius;
            }   

            base.Update(ctx);
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            canvas.Save();

            var p0 = ToCartesian(NorthEast);
            var p1 = ToCartesian(NorthWest);
            var p2 = ToCartesian(SouthEast);
            var p3 = ToCartesian(SouthWest);


            canvas.State.Color = "#ffff00";
            canvas.State.Transform = Parent!.WorldMatrix;

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

            canvas.Restore();
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

        public Texture2D? Color { get; set; }

        public float SphereRadius { get; set; }

        public Vector3 SphereWorldCenter { get; set; }

        public bool DebugGizmos { get; set; }

        bool IDrawGizmos.IsEnabled => DebugGizmos;
    }
}
