const float EPS = 1e-6;

layout(std140, binding = 13) uniform SprayProjectionBlock
{
    mat4 uHostLocalToWorld;
    mat4 uCanvasWorldToLocal;
    mat4 uCanvasLocalToWorld;

    vec3 uSprayCenterLocal;
    vec3 uSprayDirectionLocal;

    float uSprayRadius;
    float uSpreadAngle;
    vec2 uCanvasSize;

    float uDensityScale;
    float _uDistanceFalloff;
    float uRadialFalloff;

    vec3 uPrevPosition;
    vec4 uPrevRotation;

    vec3 uCurPosition;
    vec4 uCurRotation;

    int uStepCount;

    vec3 uHostScale;
};

layout(std430, binding = 12) buffer PaintStateBuffer
{
    uint HasSprayFragments;

    uint SprayMinX;
    uint SprayMinY;
    uint SprayMaxX;
    uint SprayMaxY;
};
