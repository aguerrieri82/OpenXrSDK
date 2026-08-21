using System.Numerics;

namespace XrEngine
{
    public class DepthClipEffect : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static DepthClipEffect()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "empty.frag",
                VertexSourceName = "depth_clip.vert",
                Resolver = str => Embedded.GetString(str),
            };
        }

        public DepthClipEffect()
            : base()
        {
            _shader = SHADER;
            UseDepth = false;
            WriteDepth = true;
            WriteColor = false;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddExtension("GL_OVR_multiview2");

            bld.ExecuteAction((ctx, up) =>
            {
                var clips = ctx.ClipRegions;

                if (clips == null)
                    return;

                var size = ctx.PassCamera!.ViewSize;

                for (var j = 0; j < clips.Length; j++)
                {
                    var clip = clips[j];

                    var minX = 2f * clip.X / size.Width - 1f;
                    var minY = 2f * clip.Y / size.Height - 1f;
                    var maxX = 2f * (clip.X + clip.Width) / size.Width - 1f;
                    var maxY = 2f * (clip.Y + clip.Height) / size.Height - 1f;

                    up.SetUniform($"uViewClip[{j}]", new Vector4(minX, minY, maxX, maxY));
                }
            });
        }

    }
}
