namespace Xr.Engine
{
    public class Quad3D : Geometry3D
    {
        public Quad3D()
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

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0;
        }


        public static readonly Quad3D Instance = new Quad3D();
    }
}
