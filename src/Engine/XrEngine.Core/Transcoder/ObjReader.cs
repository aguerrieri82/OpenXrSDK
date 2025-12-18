using System.Globalization;
using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class ObjReader : BaseAssetLoader
    {

        public ObjReader()
        {

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        protected override bool CanHandleExtension(string extension, out Type resType)
        {
            if (extension == ".obj")
            {
                resType = typeof(TriangleMesh);
                return true;
            }
            return base.CanHandleExtension(extension, out resType);
        }

        public override EngineObject LoadAsset(Uri uri, Type resType, EngineObject? destObj, IAssetLoaderOptions? options = null)
        {
            string path = GetFilePath(uri);
            using FileStream stream = File.OpenRead(path);
            using StreamReader reader = new StreamReader(stream);

            string? line;

            List<VertexData> data = new List<VertexData>();
            List<uint> indexes = new List<uint>();
            int vni = 0;

            float Parse(string num)
            {
                return float.Parse(num, CultureInfo.InvariantCulture);
            }

            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(' ');
                if (parts.Length < 2)
                    continue;
                if (parts[0] == "v")
                {
                    data.Add(new VertexData
                    {
                        Pos = new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3]))
                    });
                }
                else if (parts[0] == "vn")
                {
                    data[vni] = new VertexData
                    {
                        Normal = new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])).Normalize(),
                        Pos = data[vni].Pos
                    };
                    vni++;
                }
                else if (parts[0] == "f")
                {
                    uint ix0 = uint.Parse(parts[1].Split('/')[0]);
                    uint ix1 = uint.Parse(parts[2].Split('/')[0]);
                    uint ix2 = uint.Parse(parts[3].Split('/')[0]);

                    indexes.Add(ix0 - 1);
                    indexes.Add(ix1 - 1);
                    indexes.Add(ix2 - 1);
                }
            }

            Geometry3D geo = new Geometry3D()
            {
                Indices = indexes.ToArray(),
                Vertices = data.ToArray(),
                ActiveComponents = VertexComponent.Position | VertexComponent.Normal
            };

            TriangleMesh mesh = new TriangleMesh(geo);
            return mesh;
        }

        public static readonly ObjReader Instance = new();
    }
}
