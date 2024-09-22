using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class DepthClearMaterial : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static DepthClearMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "clear.frag",
                VertexSourceName = "Utils/fullscreen.vert",
                Resolver = str => Embedded.GetString(str),       
                IsLit = false,
                Priority = -1
            };
        }


        public DepthClearMaterial()
            : base()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Opaque;
            UseDepth = false;
            WriteDepth = true;
            WriteColor = false; 
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<DepthClearMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject(this);
        }
    }
}
