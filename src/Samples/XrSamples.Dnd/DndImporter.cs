using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using XrEngine;
using XrMath;


#pragma warning disable 8618

namespace XrSamples.Dnd
{
    public class DndImporter
    {
        readonly Dictionary<string, ShaderMaterial> _materials = [];
        readonly Dictionary<string, Texture2D> _textures = [];
        readonly Dictionary<string, Geometry3D> _geos = [];
        private string _basePath = ".";
        private readonly HashSet<string> _unusedTex = [];
        private readonly List<string> _psNames = [];

        #region STRUCTS

        public struct Vector2H
        {
            public Half X;
            public Half Y;

            public Vector2 ToVector2()
            {
                return new Vector2((float)X, (float)Y);
            }
        }

        public struct Vector3H
        {
            public Half X;
            public Half Y;
            public Half Z;


            public Vector3 ToVecto3()
            {
                return new Vector3((float)X, (float)Y, (float)Z);
            }
        }
        public struct Vector4H
        {
            public Half X;
            public Half Y;
            public Half Z;
            public Half W;

            public Vector4 ToVector4()
            {
                return new Vector4((float)X, (float)Y, (float)Z, (float)W);
            }
        }

        #endregion

        #region Data

        public class ImpConst
        {
            public string name { get; set; }

            public float[][] values { get; set; }
        }

        public class ImpMaterial
        {
            public ImpPixelShader ps { get; set; }

            public ImpTexture[] textures { get; set; }

            public string id { get; set; }

            public ImpConst[] cbs { get; set; }

        }

        public class ImpPixelShader
        {
            public string entry { get; set; }
            public string resId { get; set; }
            public string name { get; set; }
            public int topology { get; set; }
        }

        public class ImpTexture
        {
            public int slot { get; set; }
            public int type { get; set; }
            public string resId { get; set; }
            public string name { get; set; }
        }


        public class ImpDraw
        {
            public int id { get; set; }
            public string meshId { get; set; }
            public string matId { get; set; }
            public string psId { get; set; }
            public float[] world { get; set; }
        }


        public class ImpMesh
        {
            public string id { get; set; }
            public string ixResId { get; set; }
            public int ixByteOffset { get; set; }
            public int ixByteStride { get; set; }
            public int ixOffset { get; set; }
            public int ixCount { get; set; }
            public string name { get; set; }
            public ImpAttribute[] attributes { get; set; }
        }

        public class ImpAttribute
        {
            public ImpFormat format { get; set; }
            public string name { get; set; }
            public string resId { get; set; }
            public int byteStride { get; set; }
            public int byteOffset { get; set; }
        }

        public class ImpFormat
        {
            public int type { get; set; }
            public int count { get; set; }
            public int byteWidth { get; set; }
        }

        #endregion

        ShaderMaterial ProcessMaterialV2(string matId)
        {
            if (!_materials.TryGetValue(matId, out var mat))
            {
                var impMat = Read<ImpMaterial>($"mat_{matId}.json");

                _psNames.Add(impMat.ps.name);

                var basMat = new BasicMaterial();

                if (impMat.ps.name == "glTF/PbrMetallicRoughness")
                {
                    basMat.DiffuseTexture = (Texture2D)ProcessTexture(impMat.textures[0])!;

                }
                else if (impMat.ps.name == "Custom Image Shader")
                {
                    basMat.DiffuseTexture = (Texture2D)ProcessTexture(impMat.textures[1])!;
                }
                else
                {
                    foreach (var impTex in impMat.textures)
                    {
                        var tex = ProcessTexture(impTex);
                        if (tex == null)
                            continue;
                        var name = impTex.name.ToLower();
                        var isDif = name.EndsWith("dif") ||
                                    name.EndsWith("diff") ||
                                    name.Contains("albedo") ||
                                    name.Contains("diffuse") ||
                                    name.Contains("basecolor") ||
                                    name.Contains("_diff");
                        if (isDif)
                        {
                            basMat.DiffuseTexture = (Texture2D)tex;
                            break;
                        }

                    }
                }

                basMat.SetProp("ps_name", impMat.ps.name);

                _materials[matId] = basMat;

                mat = basMat;
            }

            return mat;
        }

        ShaderMaterial ProcessMaterial(string matId)
        {
            if (!_materials.TryGetValue(matId, out var mat))
            {
                var impMat = Read<ImpMaterial>($"mat_{matId}.json");

                _psNames.Add(impMat.ps.name);

                var pbr = (PbrV2Material)MaterialFactory.CreatePbr(Color.White);
                pbr.Simplified = SimpleMaterials;

                if (impMat.ps.name == "glTF/PbrMetallicRoughness")
                {

                    pbr.ColorMap = (Texture2D)ProcessTexture(impMat.textures[0])!;
                    pbr.MetallicRoughnessMap = (Texture2D)ProcessTexture(impMat.textures[1])!;
                    pbr.NormalMap = (Texture2D)ProcessTexture(impMat.textures[2])!;
                    pbr.OcclusionMap = (Texture2D)ProcessTexture(impMat.textures[3])!;
                    pbr.NormalScale = impMat.cbs[0].values[10][3];
                    pbr.Color = new Color(impMat.cbs[0].values[4][0], impMat.cbs[0].values[4][1], impMat.cbs[0].values[4][1], 1);
                    pbr.Metalness = impMat.cbs[0].values[15][3];
                    pbr.Roughness = impMat.cbs[0].values[16][1];
                    pbr.OcclusionStrength = impMat.cbs[0].values[18][3];
                }

                if (matId == "f54acfc201032560348210eaa944d71c___")
                {
                    var height = AssetLoader.Instance.Load<Texture2D>("res://asset/Untitled material_Height.png");
                    pbr.HeightMap = new HeightMapSettings
                    {
                        Texture = height,
                        ScaleFactor = 0.01f,
                        TargetTriSize = 10,
                        DebugTessellation = false,
                        NormalStrength = new Vector3(0.2f, 0.2f, 1),
                        NormalMode = HeightNormalMode.Geometry
                    };
                }

                if (impMat.ps.name == "Custom Image Shader")
                {
                    pbr.ColorMap = (Texture2D)ProcessTexture(impMat.textures[1])!;
                }
                else
                {
                    foreach (var impTex in impMat.textures)
                    {
                        var tex = ProcessTexture(impTex);
                        if (tex == null)
                            continue;
                        var name = impTex.name.ToLower();
                        var isDif = name.EndsWith("dif") ||
                                    name.EndsWith("diff") ||
                                    name.Contains("albedo") ||
                                    name.Contains("diffuse") ||
                                    name.Contains("basecolor") ||
                                    name.Contains("_diff");

                        var isNormal = name.EndsWith("nrm") ||
                           name.Contains("normal") ||
                           name.EndsWith("-nml") ||
                           name.EndsWith("_n");


                        var isSpec = name.EndsWith("smt") ||
                                      name.EndsWith("smooth") ||
                                      name.Contains("specular");


                        var isAO = name.EndsWith("ao") ||
                                   name.Contains("occlusion");

                        var isRough = name.Contains("roughness") ||
                                      name.EndsWith("-r") ||
                                      name.EndsWith("_rgh");

                        var isMetal = name.EndsWith("_mtl") ||
                          name.EndsWith("-m");


                        if (isDif)
                            pbr.ColorMap = (Texture2D)tex;

                        else if (isNormal)
                        {
                            if (pbr.NormalMap != null)
                                continue;
                            pbr.NormalMap = (Texture2D)tex;
                            pbr.NormalMapFormat = NormalMapFormat.UnityBc3;
                            pbr.NormalScale = 1.0f;
                            if (impMat.ps.name == "Standard")
                            {
                                pbr.NormalScale = impMat.cbs[0].values[8][0];
                            }
                            else if (impMat.ps.name == "Dungeon Alchemist/Standard Shader")
                            {
                                pbr.NormalScale = impMat.cbs[0].values[5][1];
                            }
                            else if (impMat.ps.name == "Dungeon Alchemist/Floor Tile Standard Shader")
                            {
                                pbr.NormalScale = impMat.cbs[0].values[4][0];
                            }
                            else
                                Debugger.Break();
                        }
                        else if (isSpec)
                        {
                            if (impMat.ps.name == "Dungeon Alchemist/Standard Shader")
                            {
                                pbr.Roughness = impMat.cbs[0].values[6][0];
                            }
                            else
                                pbr.Roughness = 1.0f;

                            pbr.SpecularMap = (Texture2D)tex;
                            pbr.Metalness = 0f;
                        }
                        else if (isRough)
                        {
                            pbr.MetallicRoughnessMap = (Texture2D)tex;
                            pbr.Roughness = 0.5f;
                            pbr.Metalness = 0f;
                        }
                        else if (isAO)
                        {
                            pbr.OcclusionMap = (Texture2D)tex;
                        }
                        else
                        {
                            _unusedTex.Add(impTex.name);
                        }
                    }
                }


                if (impMat.ps.name == "Dungeon Alchemist/likeCharlie/TreeLeaves")
                {
                    pbr.AlphaCutoff = 0.5f;
                    pbr.Alpha = AlphaMode.Mask;
                    pbr.DoubleSided = true;
                    pbr.Roughness = 1f;
                }
                else if (impMat.ps.name == "Standard")
                {
                    var alphaCut = impMat.cbs[0].values[5][0];
                    var alphaMul = impMat.cbs[0].values[4][3];
                    if (alphaCut > 0 && alphaCut < 1)
                    {
                        pbr.AlphaCutoff = alphaCut;
                        pbr.Alpha = AlphaMode.Mask;
                        //pbr.Roughness = 1f;
                        //pbr.DoubleSided = true;
                        Debug.WriteLine($"####### Alpha {alphaCut} {pbr.ColorMap?.Name}");
                    }
                }


                mat = pbr;
                mat.SetProp("ps_name", impMat.ps.name);

                _materials[matId] = mat;
            }

            return mat;

        }

        byte[] ReadBuffer(string resId)
        {
            var fileName = Path.Combine(_basePath, $"{resId}.bin");
            return File.ReadAllBytes(fileName);
        }

        unsafe void Unpack<T>(byte[] buffer, int offset, int stride, int count, Action<T, int> action)
        {
            fixed (byte* pBuf = buffer)
            {
                var curOfs = pBuf + offset;

                int i = 0;
                while (i < count)
                {
                    var data = *(T*)curOfs;
                    action(data, i);
                    i++;
                    curOfs += stride;
                }
            }
        }

        unsafe TriangleMesh ProcessMesh(string meshId, bool rebuildNormals = false)
        {

            var mesh = new TriangleMesh();


            if (!_geos.TryGetValue(meshId, out var geo))
            {
                var impMesh = Read<ImpMesh>($"mesh_{meshId}.json");

                var vData = new List<VertexData>();

                var buffer = ReadBuffer(impMesh.ixResId);

                geo = new Geometry3D();



                geo.Indices = new uint[impMesh.ixCount];

                fixed (byte* pBuf = buffer)
                {
                    if (impMesh.ixByteStride == 4)
                    {
                        var start = (uint*)(pBuf + impMesh.ixByteOffset + impMesh.ixOffset * impMesh.ixByteStride);
                        for (var i = 0; i < impMesh.ixCount; i++)
                            geo.Indices[i] = start[i];
                    }
                    else if (impMesh.ixByteStride == 2)
                    {
                        var start = (ushort*)(pBuf + impMesh.ixByteOffset + impMesh.ixOffset * impMesh.ixByteStride);
                        for (var i = 0; i < impMesh.ixCount; i++)
                            geo.Indices[i] = start[i];
                    }
                    else
                        throw new NotSupportedException();


                }


                var maxIdx = geo.Indices.Max();

                var data = new VertexData[maxIdx + 1];

                foreach (var attr in impMesh.attributes)
                {
                    buffer = ReadBuffer(attr.resId);

                    if (attr.name == "POSITION")
                    {
                        Unpack<Vector3>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].Pos = v);
                        geo.ActiveComponents |= VertexComponent.Position;
                    }

                    else if (attr.name == "NORMAL")
                    {
                        if (attr.format.byteWidth == 2)
                            Unpack<Vector3H>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].Normal = v.ToVecto3());
                        else
                            Unpack<Vector3>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].Normal = v);
                        geo.ActiveComponents |= VertexComponent.Normal;
                    }

                    else if (attr.name == "TANGENT")
                    {
                        if (attr.format.byteWidth == 2)
                            Unpack<Vector4H>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].Tangent = v.ToVector4());
                        else
                            Unpack<Vector4>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].Tangent = v);
                        geo.ActiveComponents |= VertexComponent.Tangent;
                    }

                    else if (attr.name == "TEXCOORD0")
                    {
                        if (attr.format.byteWidth == 2)
                            Unpack<Vector2H>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].UV = v.ToVector2());
                        else
                            Unpack<Vector2>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].UV = v);
                        geo.ActiveComponents |= VertexComponent.UV0;
                    }
                    else if (attr.name == "TEXCOORD1")
                    {
                        if (attr.format.byteWidth == 2)
                            Unpack<Vector2H>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].UV1 = v.ToVector2());
                        else
                            Unpack<Vector2>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].UV1 = v);
                        geo.ActiveComponents |= VertexComponent.UV1;
                    }
                }
                geo.Vertices = data;
                _geos[meshId] = geo;

                if (rebuildNormals)
                    geo.ComputeNormals();

                if (FlipZ)
                {
                    /*
                    //Flip normals
                    for (var i = 0; i < data.Length; i++)
                        geo.Vertices[i].Normal.Z *= -1;
                    */


                    //Flip indices
                    for (int i = 0; i < geo.Indices.Length; i += 3)
                    {
                        var tmp = geo.Indices[i + 1];
                        geo.Indices[i + 1] = geo.Indices[i + 2];
                        geo.Indices[i + 2] = tmp;
                    }
                }


            }

            mesh.Geometry = geo;

            return mesh;

        }
        Texture? ProcessTexture(ImpTexture tex)
        {
            return ProcessTexture(tex.resId, tex.name);
        }
        Texture? ProcessTexture(string texId, string name)
        {
            if (!_textures.TryGetValue(texId, out var text))
            {
                var fileName = Path.Combine(_basePath, $"{texId}.dds");

                try
                {
                    text = AssetLoader.Instance.Load<Texture2D>(fileName);
                    text.WrapS = WrapMode.Repeat;
                    text.WrapT = WrapMode.Repeat;
                    text.Name = name;
                    if (text.Data!.Count > 1)
                        text.MinFilter = ScaleFilter.LinearMipmapLinear;
                    _textures[texId] = text;
                }
                catch
                {
                    return null;
                }
            }

            return text;
        }

        Geometry3D? patch;

        Object3D ProcessDraw(ImpDraw draw)
        {
            var mat = ProcessMaterial(draw.matId);

            var name = mat.GetProp<string>("ps_name");
            bool rebuildNormals = name == "Dungeon Alchemist/likeCharlie/TreeLeaves";

            var mesh = ProcessMesh(draw.meshId, rebuildNormals);

            if (draw.matId == "f54acfc201032560348210eaa944d71c__")
            {
                if (patch == null)
                {
                    var size = mesh.Geometry!.Bounds.Size;
                    patch = new QuadPatch3D(new Vector2(size.X, size.Y));
                }

                mesh.Geometry = patch;
                //Debugger.Break();
            }

            var word = MathUtils.CreateMatrix(draw.world);
            if (FlipZ)
                word *= Matrix4x4.CreateScale(1, 1, -1);

            mesh.WorldMatrix = word;
            mesh.Materials.Add(mat);
            mesh.Name = "#" + draw.id;

            return mesh;
        }

        T Read<T>(string path)
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(_basePath, path)))!;
        }

        public void GroupDraws(Group3D main)
        {
            var walls = new Group3D();
            walls.Name = "Walls";

            var tiles = new Group3D();
            tiles.Name = "Tiles";

            for (var i = main.Children.Count - 1; i >= 0; i--)
            {
                if (main.Children[i] is not TriangleMesh item)
                    continue;

                var bounds = item.LocalBounds;
                if (bounds.Size.IsSimilar(new Vector3(1, 1.8f, 0.14f), 0.1f))
                {
                    item.Flags |= EngineObjectFlags.LargeOccluder;
                    walls.AddChild(item, true);
                }
                if (bounds.Size.IsSimilar(new Vector3(1, 0, 1f), 0.1f))
                {
                    tiles.AddChild(item, true);
                    MapY = MathF.Max(MapY, item.WorldPosition.Y);
                }
            }
           
            main.AddChild(walls);
            main.AddChild(tiles);
        }

        public Group3D Import(string path)
        {
            _basePath = path;

            var draws = Read<ImpDraw[]>("draws.json");

            var res = new Group3D
            {
                Name = "Map"
            };

            foreach (var draw in draws!)
                res.AddChild(ProcessDraw(draw));

            /*
            var totIdex = res.Children.OfType<TriangleMesh>().Sum(a => a.Geometry.Indices.Length);  


            var stats = res.Children.OfType<TriangleMesh>()
                .GroupBy(a => a.Geometry)
                .Select(a => new
                {
                    geo = a.Key,
                    count = a.Count(),
                    Mats = a.GroupBy(b => b.Materials[0])
                           .Select(b => 
                           new { 
                               mat = b.Key, 
                               count = b.Count() 
                           })
                           .OrderByDescending(a=> a.count)
                           .ToArray()
                })
                .OrderByDescending(a => a.count)
                .ToArray();
            */

            GroupDraws(res);

            return res;
        }

        public float MapY { get; set; }

        public IEnumerable<PbrV2Material> Materials => _materials.Values.OfType<PbrV2Material>();

        public bool FlipZ { get; set; } = true;

        public bool SimpleMaterials { get; set; }
    }
}
