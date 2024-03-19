using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Materials;

namespace XrEngine
{
    public class EnvironmentView : TriangleMesh
    {
        static readonly Geometry3D CubeGeometry = new()
        {
            Indices = [
                1, 2, 0,
                2, 3, 0,
                6, 2, 1,
                1, 5, 6,
                6, 5, 4,
                4, 7, 6,
                6, 3, 2,
                7, 3, 6,
                3, 7, 0,
                7, 4, 0,
                5, 1, 0,
                4, 5, 0
            ],
            Vertices = VertexData.FromPos(
            [
                -1, -1, -1,
                 1, -1, -1,
                 1,  1, -1,
                -1,  1, -1,
                -1, -1,  1,
                 1, -1,  1,
                 1,  1,  1,
                -1,  1,  1
            ]),
            ActiveComponents = VertexComponent.Position
        };

        public EnvironmentView()
        {
            Geometry = CubeGeometry;
            Materials.Add(new CubeMapMaterial() { });
        }

        public void LoadPanorama(string hdrFileName)
        {
            using (var stream = File.OpenRead(hdrFileName))
                LoadPanorama(stream);
        }

        public void LoadPanorama(Stream hdrStream)
        {
            LoadPanorama(HdrReader.Instance.Read(hdrStream)[0]);
            hdrStream.Dispose();
        }

        public void LoadPanorama(TextureData data)
        {
            var processor = _scene?.App?.Renderer as IIBLPanoramaProcessor;
            
            if (processor == null)
                throw new NotSupportedException();

            var textures = processor.ProcessPanoramaIBL(data, PanoramaProcessorOptions.Default());

            var map = ((CubeMapMaterial)Materials[0]);

            map.Texture = textures.LambertianEnv!;
            map.MipCount = (int)textures.MipCount;

            //textures.GGXLUT!.Data = null;
            //textures.CharlieLUT!.Data = null;
        }
    }
}
