layout(std140, binding = 0) uniform SprayProjectionBlock
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
    float uDistanceFalloff;
    float uRadialFalloff;
};