layout(location = 0) out vec2 DepthMoments;

void main()
{    
    float depth = gl_FragCoord.z;
    
    float dx = dFdx(depth);
    float dy = dFdy(depth);

    DepthMoments = vec2(depth, depth * depth + 0.25 * (dx * dx + dy * dy));
}