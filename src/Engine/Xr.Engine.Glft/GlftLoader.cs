using OpenXr.Engine;

namespace Xr.Engine.Glft
{
    public class GlftLoader
    {
        GlftLoader()
        {

        }

        public EngineObject Load(string filePath)
        {
            var model = glTFLoader.Interface.LoadModel(filePath);
            var buffer = glTFLoader.Interface.LoadBinaryBuffer(model, 0, filePath);

            var output = new DracoDecoder.VertexData[100000];
            var mesh = DracoDecoder.DecodeBuffer(buffer);

            var result = new Mesh()
            {
                Geometry = new Geometry3D
                {
                    Indices = mesh.Indices,
                    Vertices = mesh.Vertices.Select(a => new VertexData
                    {
                        Normal = a.Normal,
                        Pos = a.Position,
                        UV = a.UV,
                    }).ToArray()
                }
            };
            result.Materials.Add(new StandardMaterial() { Color= Color.White });

            return result;
        }

        public static readonly GlftLoader Instance = new();
    }
}
