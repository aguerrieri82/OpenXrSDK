namespace OpenXr.Engine
{
    public class Quad : Geometry3D
    {
        public Quad()
        {
            Vertices = VertexData.FromPosNormalUV(
            [
               //X    Y      Z      
                 0.5f,  0.5f, 0.0f, 0f, 0f, -1f, 1f, 0f,
                 0.5f, -0.5f, 0.0f, 0f, 0f, -1f, 1f, 1f,
                -0.5f, -0.5f, 0.0f, 0f, 0f, -1f, 0f, 1f,
                -0.5f,  0.5f, 0.0f, 0f, 0f, -1f, 0f, 0f,
             ]);

            Indices =
            [
                0, 1, 3,
                1, 2, 3
            ];

            this.UpdateBounds();
        }


        public static readonly Quad Instance = new Quad();
    }
}
