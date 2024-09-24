using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class DepthCopyEffect : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static DepthCopyEffect()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "copy_depth.frag",
                VertexSourceName = "Utils/fullscreen.vert",
                Resolver = str => Embedded.GetString(str),       
                IsLit = false,
                Priority = -1
            };
        }


        public DepthCopyEffect()
            : base()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Opaque;
            UseDepth = false;
            WriteDepth = false;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<DepthCopyEffect>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject(this);
        }
    }
}
