using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using XrMath;

namespace XrEngine
{
    public class ObjReader : BaseAssetLoader
    {
        #region Private Structs

        private readonly struct ObjVertexKey
        {
            public ObjVertexKey(int position, int uv, int normal)
            {
                Position = position;
                UV = uv;
                Normal = normal;
            }

            public readonly int Position;

            public readonly int UV;

            public readonly int Normal;
        }

        #endregion

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

            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 1024,
                FileOptions.SequentialScan);

            using var reader = new StreamReader(stream, bufferSize: 1024 * 1024);

            var estimatedVertices = Math.Max(1024, (int)(stream.Length / 128));
            var estimatedIndices = estimatedVertices * 6;

            var positions = new List<Vector3>(estimatedVertices);
            var normals = new List<Vector3>(estimatedIndices);
            var uvs = new List<Vector2>(estimatedIndices);

            var vertices = new List<VertexData>(estimatedIndices);
            var indices = new List<uint>(estimatedIndices);

            void AddVertex(ObjVertexKey key)
            {
                var vertex = new VertexData
                {
                    Pos = positions[key.Position]
                };

                if (key.UV >= 0)
                    vertex.UV = uvs[key.UV];

                if (key.Normal >= 0)
                    vertex.Normal = normals[key.Normal];

                var index = (uint)vertices.Count;

                vertices.Add(vertex);
                indices.Add(index);
            }

            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                var span = line.AsSpan();

                if (span.Length == 0 || span[0] == '#')
                    continue;

                if (!ReadToken(ref span, out var tag))
                    continue;

                if (tag.Length == 1 && tag[0] == 'v')
                {
                    if (!ReadToken(ref span, out var x) ||
                        !ReadToken(ref span, out var y) ||
                        !ReadToken(ref span, out var z))
                        continue;

                    positions.Add(new Vector3(
                        ParseFloat(x),
                        ParseFloat(y),
                        ParseFloat(z)));

                    continue;
                }

                if (tag.Length == 2 && tag[0] == 'v' && tag[1] == 'n')
                {
                    if (!ReadToken(ref span, out var x) ||
                        !ReadToken(ref span, out var y) ||
                        !ReadToken(ref span, out var z))
                        continue;

                    normals.Add(new Vector3(
                        ParseFloat(x),
                        ParseFloat(y),
                        ParseFloat(z)).Normalize());

                    continue;
                }

                if (tag.Length == 2 && tag[0] == 'v' && tag[1] == 't')
                {
                    if (!ReadToken(ref span, out var x) ||
                        !ReadToken(ref span, out var y))
                        continue;

                    uvs.Add(new Vector2(
                        ParseFloat(x),
                        ParseFloat(y)));

                    continue;
                }

                if (tag.Length == 1 && tag[0] == 'f')
                {
                    if (!ReadToken(ref span, out var firstToken) ||
                        !ReadToken(ref span, out var prevToken) ||
                        !ReadToken(ref span, out var curToken))
                        continue;

                    var first = ParseFaceVertexKey(firstToken, positions.Count, uvs.Count, normals.Count);
                    var prev = ParseFaceVertexKey(prevToken, positions.Count, uvs.Count, normals.Count);
                    var cur = ParseFaceVertexKey(curToken, positions.Count, uvs.Count, normals.Count);

                    AddVertex(first);
                    AddVertex(prev);
                    AddVertex(cur);

                    prev = cur;

                    while (ReadToken(ref span, out curToken))
                    {
                        cur = ParseFaceVertexKey(curToken, positions.Count, uvs.Count, normals.Count);

                        AddVertex(first);
                        AddVertex(prev);
                        AddVertex(cur);

                        prev = cur;
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

        private static bool ReadToken(ref ReadOnlySpan<char> span, out ReadOnlySpan<char> token)
        {
            span = span.TrimStart();

            if (span.Length == 0)
            {
                token = default;
                return false;
            }

            var end = 0;

            while (end < span.Length && !char.IsWhiteSpace(span[end]))
                end++;

            token = span[..end];
            span = span[end..];

            return true;
        }

        private static ObjVertexKey ParseFaceVertexKey(
            ReadOnlySpan<char> token,
            int positionCount,
            int uvCount,
            int normalCount)
        {
            var slash0 = token.IndexOf('/');

            if (slash0 < 0)
                return new ObjVertexKey(ParseObjIndex(token, positionCount), -1, -1);

            var positionToken = token[..slash0];
            var rest = token[(slash0 + 1)..];

            var slash1 = rest.IndexOf('/');

            if (slash1 < 0)
            {
                return new ObjVertexKey(
                    ParseObjIndex(positionToken, positionCount),
                    rest.Length > 0 ? ParseObjIndex(rest, uvCount) : -1,
                    -1);
            }

            var uvToken = rest[..slash1];
            var normalToken = rest[(slash1 + 1)..];

            return new ObjVertexKey(
                ParseObjIndex(positionToken, positionCount),
                uvToken.Length > 0 ? ParseObjIndex(uvToken, uvCount) : -1,
                normalToken.Length > 0 ? ParseObjIndex(normalToken, normalCount) : -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ParseFloat(ReadOnlySpan<char> token)
        {
            return float.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ParseObjIndex(ReadOnlySpan<char> token, int count)
        {
            var index = int.Parse(token, NumberStyles.Integer, CultureInfo.InvariantCulture);

            if (index > 0)
                return index - 1;

            if (index < 0)
                return count + index;

            throw new FormatException("OBJ index 0 is invalid.");
        }

        public static readonly ObjReader Instance = new();
    }
}