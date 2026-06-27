
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using XrEngine.Objects;
using XrMath;

namespace XrEngine
{
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct QuadStyle
    {
        public Color BackColor;
        public float Opacity;
    }

    public enum TextureCutMode
    {
        Main = 0,
        Layers = 1
    }

    public class TextureCutMaterial : TextureMaterial
    {
        static readonly Shader CUT_SHADER;

        static TextureCutMaterial()
        {
            CUT_SHADER = new StandardVertexShader
            {
                VertexSourceName = "texture_cut.vert",
                FragmentSourceName = "texture_cut.frag",
                IsLit = false,
            };
        }

        public TextureCutMaterial()
        {
            Shader = CUT_SHADER;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature($"MODE {(int)Mode}u");

            if (!WriteColor)
                bld.AddFeature("DEPTH_ONLY");

            bld.LoadBufferArray(ctx=>
            {
                if (_contentVersion == ctx.CurrentBuffer!.Version)
                    return null;

                ctx.CurrentBuffer!.Version = _contentVersion;

                return Styles;
            }, 10, BufferStore.Model, false);

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uCount", Styles?.Length ?? 0);
            });

            base.UpdateShaderMaterial(bld);
        }

        public QuadStyle[]? Styles { get; set; }

        public TextureCutMode Mode { get; set; }
    }
}
