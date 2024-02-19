#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Engine;
using OpenXr.Engine.OpenGL;



namespace Xr.Engine.OpenGL
{


    public class GlPbrProgram : GlProgram
    {
        readonly PbrMaterial _mat;

        public GlPbrProgram(GL gl, PbrMaterial material, GlRenderOptions renderOptions)
            : base(gl, renderOptions)
        {
            _mat = material;

        }


        public IEnumerable<string> Defines()
        {
            yield return "";
            /*
            if (_mat.HasClearcoat && _extensions.Contains(GlExtensions.KHR_materials_clearcoat))
            {
                yield return "MATERIAL_CLEARCOAT 1";
            }
            if (_mat.HasSheen && _extensions.Contains(GlExtensions.KHR_materials_sheen))
            {
                yield return "MATERIAL_SHEEN 1";
            }
            if (_mat.HasTransmission && _extensions.Contains(GlExtensions.KHR_materials_transmission))
            {
                yield return "MATERIAL_TRANSMISSION 1";
            }
            if (_mat.HasVolume && _extensions.Contains(GlExtensions.KHR_materials_volume))
            {
                yield return "MATERIAL_VOLUME 1";
            }
            if (_mat.HasIOR && _extensions.Contains(GlExtensions.KHR_materials_ior))
            {
                yield return "MATERIAL_IOR 1";
            }
            if (_mat.HasSpecular && _extensions.Contains(GlExtensions.KHR_materials_specular))
            {
                yield return "MATERIAL_SPECULAR 1";
            }
            if (_mat.HasIridescence && _extensions.Contains(GlExtensions.KHR_materials_iridescence))
            {
                yield return "MATERIAL_IRIDESCENCE 1";
            }
            if (_mat.HasEmissiveStrength && _extensions.Contains(GlExtensions.KHR_materials_emissive_strength))
            {
                yield return "MATERIAL_EMISSIVE_STRENGTH 1";
            }
            if (_mat.HasAnisotropy && _extensions.Contains(GlExtensions.KHR_materials_anisotropy))
            {
                yield return "MATERIAL_ANISOTROPY 1";
            }
            */
        }
    }
}
