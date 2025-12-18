using DotSpatial.Projections;

using System.Numerics;
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
            // Convert degrees to radians
            float latRad = (float)DegreesToRadians(latLng.Y);
            float lonRad = (float)DegreesToRadians(latLng.X);


            // Compute Cartesian coordinates
            float x = SphereRadius * MathF.Cos(latRad) * MathF.Cos(lonRad);
            float y = SphereRadius * MathF.Sin(latRad);
            float z = SphereRadius * MathF.Cos(latRad) * MathF.Sin(lonRad);

            return new Vector3(x, y, -z);
        }

        Vector2 ToUv(Vector2 latLng)
        {
            Vector3 normal = ToCartesian(latLng).Normalize();

            return NormalToUV(normal);
        }

        public void LoadAlbedoSlice(Texture2D tex)
        {
            Geometry!.ActiveComponents |= VertexComponent.UV1;

            IPbrMaterial mat = (IPbrMaterial)Materials[0];

            mat.ColorMapUVSet = 1;
            mat.ColorMap = tex;

            Vector2 p0 = ToUv(NorthEast);
            Vector2 p1 = ToUv(NorthWest);
            Vector2 p2 = ToUv(SouthEast);
            Vector2 p3 = ToUv(SouthWest);

            int len = Geometry!.Vertices.Length;


            Matrix3x2 trans = Matrix3x2.CreateScale(p1.X - p0.X, p2.Y - p0.Y) *
                        Matrix3x2.CreateTranslation(p0.X, p0.Y);


            for (int i = 0; i < len; i++)
            {
                Vector2 uv0 = Geometry!.Vertices[i].UV;

                Geometry!.Vertices[i].UV1 = Vector2.Transform(uv0, trans);
            }

        }

        public unsafe void LoadGeoTiff(string path)
        {
            LibTiff.Tiff tiff = LibTiff.TIFFOpen(path, "r");

            TextureData texData = tiff.Read();

            if (texData.Format == TextureFormat.GrayRawSInt16)
            {
                texData.Data = ImageUtils.ConvertShortToFloat(texData.Data!);
                texData.Format = TextureFormat.GrayFloat32;
            }

            uint w = texData.Width;
            uint h = texData.Height;

            HeightMap = Texture2D.FromData([texData]);

            double[] tie = tiff.GetDoubleArrayField(LibTiff.TiffTag.GeoTiePoints);
            double[] scale = tiff.GetDoubleArrayField(LibTiff.TiffTag.GeoPixelScale);
            double[] trans = tiff.GetDoubleArrayField(LibTiff.TiffTag.GeoTransMatrix);
            string noData = tiff.GetStringField(LibTiff.TiffTag.GdalNoData);
            string meta = tiff.GetStringField(LibTiff.TiffTag.GdalMetadata);

            LibTiff.GeoDirectoryEntry[] dir = LibTiff.GetGeoDirectory(tiff);

            tiff.TIFFClose();

            LibTiff.GeoModelType model = (LibTiff.GeoModelType)(ushort)dir.First(a => a.Key == LibTiff.GeoTag.GTModelTypeGeoKey).Value!;

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
                ushort code = (ushort)dir.First(a => a.Key == LibTiff.GeoTag.ProjectedCSTypeGeoKey).Value!;

                if (code != 4326)
                {
                    ProjectionInfo source = ProjectionInfo.FromEpsgCode(code);
                    ProjectionInfo target = ProjectionInfo.FromEpsgCode(4326);

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

            Vector3 p0 = ToCartesian(NorthEast);
            Vector3 p1 = ToCartesian(NorthWest);
            Vector3 p2 = ToCartesian(SouthEast);
            Vector3 p3 = ToCartesian(SouthWest);

            float ww = (p1 - p0).Length();
            float hh = (p2 - p0).Length();

            Vector3 center = (p0 + p1 + p2 + p3) / 4f;
            Vector3 zAxis = center.Normalize();
            Vector3 xAxis = Vector3.Normalize(p1 - p0);
            Vector3 yAxis = Vector3.Cross(zAxis, xAxis);

            Matrix4x4 rotationMatrix = new Matrix4x4(
                 xAxis.X, yAxis.X, zAxis.X, 0,
                 xAxis.Y, yAxis.Y, zAxis.Y, 0,
                 xAxis.Z, yAxis.Z, zAxis.Z, 0,
                 0, 0, 0, 1
             );

            Quaternion orientation = Quaternion.CreateFromRotationMatrix(rotationMatrix);

            Transform.Orientation = new Quaternion(orientation.X, orientation.Y, orientation.Z, -orientation.W);
            Transform.Position = center;

            Geometry = new QuadPatch3D(new Vector2(ww, hh), 100);

            IPbrMaterial pbr = MaterialFactory.CreatePbr("#ffffff");
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
                    NormalStrength = new Vector3(1000, -1000, 0.3f),
                    NormalMode = HeightNormalMode.Fast,
                    MaskValue = -9999,
                    SphereRadius = SphereRadius,
                    SphereWorldCenter = SphereWorldCenter
                };

                HeightMap.MipLevelCount = 20;
                HeightMap.WrapS = WrapMode.ClampToEdge;
                HeightMap.WrapT = WrapMode.ClampToEdge;
                HeightMap.MagFilter = ScaleFilter.LinearMipmapLinear;
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

            Vector3 p0 = ToCartesian(NorthEast);
            Vector3 p1 = ToCartesian(NorthWest);
            Vector3 p2 = ToCartesian(SouthEast);
            Vector3 p3 = ToCartesian(SouthWest);


            canvas.State.Color = "#ffff00";
            canvas.State.Transform = Parent!.WorldMatrix;

            canvas.DrawLine(p0, p1);
            canvas.DrawLine(p2, p3);
            canvas.DrawLine(p0, p2);
            canvas.DrawLine(p1, p3);


            Vector3 center = (p0 + p1 + p2 + p3) / 4f;
            Vector3 zAxis = center.Normalize();
            Vector3 xAxis = Vector3.Normalize(p1 - p0);
            Vector3 yAxis = Vector3.Cross(zAxis, xAxis);

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
