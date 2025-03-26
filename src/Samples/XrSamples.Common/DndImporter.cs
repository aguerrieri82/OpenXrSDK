﻿using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using XrEngine;
using XrMath;


#pragma warning disable 8618

namespace XrSamples
{
    public class DndImporter
    {
        Dictionary<string, ShaderMaterial> _materials = [];
        Dictionary<string, Texture2D> _textures = [];
        Dictionary<string, Geometry3D> _geos = [];
        private string _basePath = ".";
        private HashSet<string> _unusedTex = [];
        private List<string> _psNames = [];

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

        ShaderMaterial ProcessMaterial(string matId)
        {
            if (!_materials.TryGetValue(matId, out var mat))
            {
                var impMat = Read<ImpMaterial>($"mat_{matId}.json");

                _psNames.Add(impMat.ps.name);


                var pbr = (PbrV2Material)MaterialFactory.CreatePbr(Color.White);
                //pbr.Alpha = AlphaMode.Blend;

                if (impMat.ps.name == "glTF/PbrMetallicRoughness")
                {

                    pbr.ColorMap = (Texture2D)ProcessTexture(impMat.textures[0].resId)!;
                    pbr.MetallicRoughnessMap = (Texture2D)ProcessTexture(impMat.textures[1].resId)!;
                    pbr.NormalMap = (Texture2D)ProcessTexture(impMat.textures[2].resId)!;
                }
                else if (impMat.ps.name == "Custom Image Shader")
                {
                    pbr.ColorMap = (Texture2D)ProcessTexture(impMat.textures[1].resId)!;
                }
                else
                {
                    foreach (var impTex in impMat.textures)
                    {
                        var tex = ProcessTexture(impTex.resId);
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
                        }
                        else if (isSpec)
                        {
                            pbr.SpecularMap = (Texture2D)tex;
                            pbr.Roughness = 0.5f;
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

                    

                mat = (ShaderMaterial)pbr;

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

        unsafe TriangleMesh ProcessMesh(string meshId)
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

                    //Flip indices
                    for (int i = 0; i < geo.Indices.Length; i += 3)
                    {
                        var tmp = geo.Indices[i + 1];
                        geo.Indices[i + 1] = geo.Indices[i + 2];
                        geo.Indices[i + 2] = tmp;
                    }
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
                            Unpack<Vector2H>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].UV = v.ToVector2());
                        else
                            Unpack<Vector2>(buffer, attr.byteOffset, attr.byteStride, data.Length, (v, i) => data[i].UV = v);
                        geo.ActiveComponents |= VertexComponent.UV1;
                    }
                }
                geo.Vertices = data;
                _geos[meshId] = geo;

                //Flip normals
                for (var i = 0; i < data.Length; i++)
                    geo.Vertices[i].Normal.Z *= -1;
            }

            mesh.Geometry = geo;

            return mesh;

        }

        Texture? ProcessTexture(string texId)
        {
            if (!_textures.TryGetValue(texId, out var text))
            {
                var fileName = Path.Combine(_basePath, $"{texId}.dds");

                try
                {
                    text = AssetLoader.Instance.Load<Texture2D>(fileName);
                    text.WrapS = WrapMode.Repeat;
                    text.WrapT = WrapMode.Repeat;
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

        Object3D ProcessDraw(ImpDraw draw)
        {


            var mesh = ProcessMesh(draw.meshId);
            var mat = ProcessMaterial(draw.matId);

            var word = MathUtils.CreateMatrix(draw.world);
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
            for (var i = main.Children.Count-1; i>= 0; i--)
            {
                if (main.Children[i] is not TriangleMesh item)
                    continue;

                var bounds = item.LocalBounds;
                if (bounds.Size.IsSimilar(new Vector3(1, 1.8f, 0.14f), 0.1f))
                    walls.AddChild(item, true);
            }

            main.AddChild(walls);
        }

        public Group3D Import(string path)
        {
            _basePath = path;

            var draws = Read<ImpDraw[]>("draws.json");
            var res = new Group3D();
            foreach (var draw in draws!)
                res.AddChild(ProcessDraw(draw));

            GroupDraws(res);

            return res;
        }
    }
}
