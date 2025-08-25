using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
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
            var path = GetFilePath(uri);
            using var stream = File.OpenRead(path);   
            using var reader = new StreamReader(stream);
            
            string? line;

            var data = new List<VertexData>();
            var indexes = new List<uint>();
            var vni = 0;

            float Parse(string num)
            {
                return float.Parse(num, CultureInfo.InvariantCulture);
            }

            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(' ');
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
                    var ix0 = uint.Parse(parts[1].Split('/')[0]);
                    var ix1 = uint.Parse(parts[2].Split('/')[0]);
                    var ix2 = uint.Parse(parts[3].Split('/')[0]);

                    indexes.Add(ix0 - 1);
                    indexes.Add(ix1 - 1);
                    indexes.Add(ix2 - 1);
                }
            }

            var geo = new Geometry3D()
            {
                Indices = indexes.ToArray(),
                Vertices = data.ToArray(),
                ActiveComponents = VertexComponent.Position | VertexComponent.Normal
            };

            var mesh = new TriangleMesh(geo);
            return mesh;            
        }

        public static readonly ObjReader Instance = new();
    }
}
