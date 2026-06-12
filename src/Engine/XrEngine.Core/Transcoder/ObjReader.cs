using System.Globalization;
using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class ObjReader : BaseAssetLoader
    {
        public ObjReader()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
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

        public override EngineObject LoadAsset(
            Uri uri,
            Type resType,
            EngineObject? destObj,
            IAssetLoaderOptions? options = null)
        {
            var path = GetFilePath(uri);

            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream);

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();

            var vertices = new List<VertexData>();
            var indices = new List<uint>();

            float Parse(string num)
            {
                return float.Parse(num, CultureInfo.InvariantCulture);
            }

            int ParseObjIndex(string num, int count)
            {
                var ix = int.Parse(num, CultureInfo.InvariantCulture);

                // OBJ indices are 1-based.
                if (ix > 0)
                    return ix - 1;

                // Negative indices are relative to current list end.
                if (ix < 0)
                    return count + ix;

                throw new FormatException("OBJ index 0 is invalid.");
            }

            VertexData ReadFaceVertex(string token)
            {
                // Supported:
                // v
                // v/vt
                // v//vn
                // v/vt/vn

                var p = token.Split('/');

                var vi = ParseObjIndex(p[0], positions.Count);

                var result = new VertexData
                {
                    Pos = positions[vi]
                };

                if (p.Length >= 2 && p[1].Length > 0)
                {
                    var ti = ParseObjIndex(p[1], uvs.Count);
                    result.UV = uvs[ti];
                }

                if (p.Length >= 3 && p[2].Length > 0)
                {
                    var ni = ParseObjIndex(p[2], normals.Count);
                    result.Normal = normals[ni];
                }

                return result;
            }

            uint AddVertex(VertexData vertex)
            {
                var ix = (uint)vertices.Count;
                vertices.Add(vertex);
                indices.Add(ix);
                return ix;
            }

            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0 || line[0] == '#')
                    continue;

                var parts = line.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    continue;

                if (parts[0] == "v")
                {
                    positions.Add(new Vector3(
                        Parse(parts[1]),
                        Parse(parts[2]),
                        Parse(parts[3])));
                }
                else if (parts[0] == "vn")
                {
                    normals.Add(new Vector3(
                        Parse(parts[1]),
                        Parse(parts[2]),
                        Parse(parts[3])).Normalize());
                }
                else if (parts[0] == "vt")
                {
                    uvs.Add(new Vector2(
                        Parse(parts[1]),
                        Parse(parts[2])));
                }
                else if (parts[0] == "f")
                {
                    if (parts.Length < 4)
                        continue;

                    // Read all polygon corners first.
                    var face = new VertexData[parts.Length - 1];

                    for (var i = 1; i < parts.Length; i++)
                        face[i - 1] = ReadFaceVertex(parts[i]);

                    // Triangulate as fan:
                    // 0,1,2
                    // 0,2,3
                    // 0,3,4
                    for (var i = 1; i < face.Length - 1; i++)
                    {
                        AddVertex(face[0]);
                        AddVertex(face[i]);
                        AddVertex(face[i + 1]);
                    }
                }
            }

            var active =
                VertexComponent.Position |
                VertexComponent.Normal;

            if (uvs.Count > 0)
                active |= VertexComponent.UV0;

            var geo = new Geometry3D
            {
                Indices = indices.ToArray(),
                Vertices = vertices.ToArray(),
                ActiveComponents = active
            };

            return new TriangleMesh(geo);
        }

        public static readonly ObjReader Instance = new();
    }
}