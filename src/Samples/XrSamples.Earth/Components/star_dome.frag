in vec2 fUv;

layout(location=0) out vec4 FragColor;

layout(binding=0) uniform sampler2D stars;
layout(binding=1) uniform sampler2D grid;
layout(binding=2) uniform sampler2D constellations;

uniform float uExposure;
uniform float uOffset;
uniform float uTransparency;

void main()
{
    vec2 uv = fUv;
   
    FragColor = uOffset + texture(stars, uv) * uExposure;
    
    #ifdef SHOW_GRID
        FragColor += texture(grid, uv) * uTransparency;
    #endif

    #ifdef SHOW_CONST
        FragColor += texture(constellations, uv) * uTransparency;
    #endif

}