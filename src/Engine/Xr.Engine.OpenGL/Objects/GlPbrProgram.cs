#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using System.Numerics;


namespace Xr.Engine.OpenGL
{
    public class GlPbrProgram : GlProgram
    {
        public enum DebugFlags
        {
            DEBUG_NORMAL_SHADING,
            DEBUG_NORMAL_TEXTURE,
            DEBUG_NORMAL_GEOMETRY,
            DEBUG_TANGENT,
            DEBUG_BITANGENT,
            DEBUG_ALPHA,
            DEBUG_UV_0,
            DEBUG_UV_1,
            DEBUG_OCCLUSION,
            DEBUG_EMISSIVE,
            DEBUG_METALLIC_ROUGHNESS,
            DEBUG_BASE_COLOR,
            DEBUG_ROUGHNESS,
            DEBUG_METALLIC,
            DEBUG_CLEARCOAT,
            DEBUG_CLEARCOAT_FACTOR,
            DEBUG_CLEARCOAT_ROUGHNESS,
            DEBUG_CLEARCOAT_NORMAL,
            DEBUG_SHEEN,
            DEBUG_SHEEN_COLOR,
            DEBUG_SHEEN_ROUGHNESS,
            DEBUG_SPECULAR,
            DEBUG_SPECULAR_FACTOR,
            DEBUG_SPECULAR_COLOR,
            DEBUG_TRANSMISSION_VOLUME,
            DEBUG_TRANSMISSION_FACTOR,
            DEBUG_VOLUME_THICKNESS,
            DEBUG_IRIDESCENCE,
            DEBUG_IRIDESCENCE_FACTOR,
            DEBUG_IRIDESCENCE_THICKNESS,
            DEBUG_ANISOTROPIC_STRENGTH,
            DEBUG_ANISOTROPIC_DIRECTION,
            DEBUG_NONE
        }


        readonly List<PbrLightUniform> _lights = [];
        private Matrix4x4 _model;

        public GlPbrProgram(GL gl, Func<string, string> includeResolver, GlRenderOptions renderOptions)
            : base(gl, includeResolver, renderOptions)
        {
            _programId = "pbr+primitive";
            Debug = DebugFlags.DEBUG_NONE;
        }

        protected override void Build()
        {
            //var vertSrc = ShaderPreprocessor.ParseShader(PatchShader(_resolver("pbr/primitive.vert"), ShaderType.VertexShader));
            //var fragSrc = ShaderPreprocessor.ParseShader(PatchShader(_resolver("pbr/pbr.frag"), ShaderType.FragmentShader));
            var vertSrc = (PatchShader(_resolver("pbr/primitive.vert"), ShaderType.VertexShader));
            var fragSrc = (PatchShader(_resolver("pbr/pbr.frag"), ShaderType.FragmentShader));
            var vert = new GlShader(_gl, ShaderType.VertexShader, vertSrc);
            var frag = new GlShader(_gl, ShaderType.FragmentShader, fragSrc);
            Create(vert, frag); 
        }

        public override void SetModel(Matrix4x4 model)
        {
            _model = model;
            SetUniform("u_ModelMatrix", model);
        }

        public override void AddLight(PointLight point)
        {
            _lights.Add(new PbrLightUniform
            {
                type = PbrLightUniform.Point,
                color = (Vector3)point.Color,
                position = point.WorldPosition,
                intensity = point.Intensity * 10,
                innerConeCos = 0,
                outerConeCos = MathF.Cos(MathF.PI / 4f),
                range = point.Range,
            });
        }

        public override void AddLight(DirectionalLight directional)
        {
            _lights.Add(new PbrLightUniform
            {
                type = PbrLightUniform.Directional,
                color = (Vector3)directional.Color,
                position = directional.WorldPosition,
                direction =  directional.Forward,
                intensity = directional.Intensity,
                innerConeCos = 0,
                outerConeCos = MathF.Cos(MathF.PI / 4f),
                range = -1
                
            });
        }

        public override void AddLight(SpotLight spot)
        {
            _lights.Add(new PbrLightUniform
            {
                type = PbrLightUniform.Spot,
                color = (Vector3)spot.Color,
                position = spot.WorldPosition,
                direction = spot.Forward,
                intensity = spot.Intensity,
                range = spot.Range,
                innerConeCos = MathF.Cos(spot.InnerConeAngle),
                outerConeCos = MathF.Cos(spot.OuterConeAngle)
            });
        }

        public override void ConfigureLights()
        {
            if (_lights.Count > 0)
            {
                SetUniformStructArray("u_Lights", _lights);
                //SetUniform("u_LightsCount", _lights.Count);
            }
        }

        public override void Commit()
        {
            if (_lights.Count > 0)
                _features.Add("USE_PUNCTUAL 1");

            if (_lights.Count > MaxLights)
                MaxLights = (uint)_lights.Count + 16;

            _features.Add($"LIGHT_COUNT {_lights.Count}");

            //TODO check this
             _features.Add("LINEAR_OUTPUT");

            foreach (var flag in Enum.GetValues<DebugFlags>())
                _features.Add($"{flag} {(int)flag}");

            _features.Add("ALPHAMODE_OPAQUE 0");
            _features.Add("ALPHAMODE_MASK 1");
            _features.Add("ALPHAMODE_BLEND 2");

            _features.Add($"DEBUG {Debug}");

            base.Commit();
        }

        public override void SetLayout(GlVertexLayout layout)
        {
            foreach (var item in layout.Attributes!)
            {
                if (item.Component == VertexComponent.Normal)
                    _features.Add("HAS_NORMAL_VEC3");
                else if (item.Component == VertexComponent.Tangent)
                    _features.Add("HAS_TANGENT_VEC4");
                else if (item.Component == VertexComponent.UV0)
                    _features.Add("HAS_TEXCOORD_0_VEC2");
                else if (item.Component == VertexComponent.UV1)
                    _features.Add("HAS_TEXCOORD_1_VEC2");
                else if (item.Component == VertexComponent.Color3)
                    _features.Add("HAS_COLOR_0_VEC3");
                else if (item.Component == VertexComponent.Color4)
                    _features.Add("HAS_COLOR_0_VEC4");
            }
        }

        public override void SetAmbient(AmbientLight ambient)
        {

        }

        public override void SetCamera(Camera camera)
        {
            SetUniform("u_Exposure", camera.Exposure);
            SetUniform("u_Camera", camera.Transform.Position);
            SetUniform("u_ViewProjectionMatrix", camera.Transform.Matrix * camera.Projection);
            SetUniform("u_NormalMatrix", Matrix4x4.Identity);
        }

        public override void BeginEdit()
        {
            _lights.Clear();
            base.BeginEdit();
        }

        public DebugFlags Debug { get; set; }

        public static uint MaxLights { get; set; } = 10;
    }
}
