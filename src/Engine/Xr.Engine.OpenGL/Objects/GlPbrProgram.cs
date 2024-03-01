#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Security.Cryptography;
using System.Text;
using System.Diagnostics.CodeAnalysis;


namespace Xr.Engine.OpenGL
{
    public partial class GlPbrProgram : GlSimpleProgram
    {
        static GlBuffer<PbrCameraUniforms>? _cameraBuffer;
        static GlBuffer<PbrIBLUniforms>? _ibrBuffer;
        static GlBuffer<PbrMaterialUniforms>? _materialBuffer;
        static GlBuffer<PbrLightsUniform>? _lightsBuffer;

        public GlPbrProgram(GL gl, string vSource, string fSource, Func<string, string> resolver)
            : base(gl, vSource, fSource, resolver)
        {
            _cameraBuffer ??= new GlBuffer<PbrCameraUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _ibrBuffer ??= new GlBuffer<PbrIBLUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _materialBuffer ??= new GlBuffer<PbrMaterialUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _lightsBuffer ??= new GlBuffer<PbrLightsUniform>(_gl, BufferTargetARB.UniformBuffer);
        }

        protected override GlBuffer<T> GetBuffer<T>(string name)
        {
            if (typeof(T) == typeof(PbrCameraUniforms))
                return (GlBuffer<T>)(object)_cameraBuffer!;
            
            if (typeof(T) == typeof(PbrIBLUniforms))
                return (GlBuffer<T>)(object)_ibrBuffer!;

            if (typeof(T) == typeof(PbrMaterialUniforms))
                return (GlBuffer<T>)(object)_materialBuffer!;

            if (typeof(T) == typeof(PbrLightsUniform))
                return (GlBuffer<T>)(object)_lightsBuffer!;

            return base.GetBuffer<T>(name);
        }

    }
}
