#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif



namespace Xr.Engine.OpenGL
{
    public partial class GlPbrProgram : GlSimpleProgram
    {
        static GlBuffer<PbrMaterial.CameraUniforms>? _cameraBuffer;
        static GlBuffer<PbrMaterial.IBLUniforms>? _ibrBuffer;
        static GlBuffer<PbrMaterial.MaterialUniforms>? _materialBuffer;
        static GlBuffer<PbrMaterial.LightsUniform>? _lightsBuffer;

        public GlPbrProgram(GL gl, string vSource, string fSource, Func<string, string> resolver)
            : base(gl, vSource, fSource, resolver)
        {
            _cameraBuffer ??= new GlBuffer<PbrMaterial.CameraUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _ibrBuffer ??= new GlBuffer<PbrMaterial.IBLUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _materialBuffer ??= new GlBuffer<PbrMaterial.MaterialUniforms>(_gl, BufferTargetARB.UniformBuffer);
            _lightsBuffer ??= new GlBuffer<PbrMaterial.LightsUniform>(_gl, BufferTargetARB.UniformBuffer);
        }

        protected override GlBuffer<T> GetBuffer<T>(string name)
        {
            if (typeof(T) == typeof(PbrMaterial.CameraUniforms))
                return (GlBuffer<T>)(object)_cameraBuffer!;

            if (typeof(T) == typeof(PbrMaterial.IBLUniforms))
                return (GlBuffer<T>)(object)_ibrBuffer!;

            if (typeof(T) == typeof(PbrMaterial.MaterialUniforms))
                return (GlBuffer<T>)(object)_materialBuffer!;

            if (typeof(T) == typeof(PbrMaterial.LightsUniform))
                return (GlBuffer<T>)(object)_lightsBuffer!;

            return base.GetBuffer<T>(name);
        }

    }
}
