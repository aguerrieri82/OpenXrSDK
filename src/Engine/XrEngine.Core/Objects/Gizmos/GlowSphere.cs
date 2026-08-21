namespace XrEngine
{
    public class GlowSphere : TriangleMesh
    {
        public GlowSphere()
        {
            Geometry = Quad3D.Default;
            Materials.Add(new GlowSphereMaterial() { Attenuation = GlowAttType.Point });
        }

        public override void Update(RenderContext ctx)
        {
            Forward = ctx.Camera!.Forward;
            base.Update(ctx);
        }
    }
}
