layout(location = 0) in vec2 aCorner; // (-1,-1), (1,-1), (1,1), (-1,1)

struct SplatData
{
    vec3 Position; // xyz = world position
    vec4 AxisX;
    vec4 AxisY;
    vec4 Color;
};

layout(std430, binding = 18) readonly buffer SplatBuffer
{
    SplatData uSplats[];
};

uniform mat4 uViewProj;
uniform float uSplatRadius;
uniform float uSplatDepthBias;

#ifdef DISTANCE_SCALE
uniform mat4 uView;
uniform float uSplatDistanceScale;
uniform float uSplatMinRadius;
uniform float uSplatMaxRadius;
#endif

#ifdef CAMERA_FACING
uniform vec3 uCameraRight;
uniform vec3 uCameraUp;
#endif

out vec2 vLocal;
out vec4 vColor;

void main()
{
    SplatData s = uSplats[gl_InstanceID];

#ifdef CAMERA_FACING
    vec3 axisX = uCameraRight;
    vec3 axisY = uCameraUp;
#else
    vec3 axisX = s.AxisX.xyz;
    vec3 axisY = s.AxisY.xyz;
#endif

    float radius = uSplatRadius;

#ifdef DISTANCE_SCALE
    vec4 viewCenter = uView * vec4(s.Position.xyz, 1.0);
    float dist = -viewCenter.z;

    radius = clamp(
        uSplatRadius + dist * uSplatDistanceScale,
        uSplatMinRadius,
        uSplatMaxRadius
    );
#endif

    vec3 worldPos =
        s.Position.xyz +
        axisX * aCorner.x * radius +
        axisY * aCorner.y * radius;

    vLocal = aCorner;
    vColor = s.Color;

    vec4 clip = uViewProj * vec4(worldPos, 1.0);

     clip.z -= clip.w * uSplatDepthBias;

    gl_Position = clip;
}