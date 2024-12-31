
layout(binding=0) uniform sampler2D srcImage;
uniform int uSize;

out vec4 fragColor;

void main() {
   
   ivec2 coords = ivec2(gl_FragCoord.xy);
   vec4 color = texelFetch(srcImage, coords, 0);

    if (color == vec4(0.0)) 
    {
        for (int x = -uSize; x <= uSize; ++x) 
        {
            for (int y = -uSize; y <= uSize; ++y) 
            {
                ivec2 offsetCoords = coords + ivec2(x, y);
                vec4 neighborColor = texelFetch(srcImage, offsetCoords, 0);

                if (neighborColor != vec4(0.0)) 
                {
                    fragColor = neighborColor;
                    return;
                }
            }
         }
    }

    discard;  
}