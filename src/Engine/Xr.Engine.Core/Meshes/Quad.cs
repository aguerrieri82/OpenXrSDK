namespace OpenXr.Engine
{
    public class Quad : Geometry3D
    {
        public Quad()
        {
            Vertices = VertexData.FromPosNormalUV(
            [   
                -1f, 0f, 1f, -0f, 1f, -0f, 0f, 0f,
                1f, 0f, 1f, -0f, 1f, -0f, 1f, 0f,
                1f, 0f, -1f, -0f, 1f, -0f, 1f, 1f,
                -1f, 0f, -1f, -0f, 1f, -0f, 0f, 1f,

             ]);

            Indices =
            [
                0,1,2,
                0,2,3,
            ];
        }


        public static readonly Quad Instance = new Quad();
    }
}
