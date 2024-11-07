layout (local_size_x = 16, local_size_y = 16, local_size_z=1) in;

layout (binding = 0, rgba8) restrict readonly uniform highp image2D srcImage;

layout (binding = 1, rgba8) restrict writeonly uniform highp image2D destImage;

uniform int uSize; 

void main() {

    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);

    imageStore(destImage, coords, vec4(0.0));
      
    vec4 color = imageLoad(srcImage, coords);

    if (color == vec4(0.0)) 
        return;

    bool found = false; 
    
    for (int x = -uSize; x <= uSize; ++x) 
    {
        for (int y = -uSize; y <= uSize; ++y) 
        {
            ivec2 curCoords = ivec2(coords + ivec2(x, y));
            vec4 curColor = imageLoad(srcImage, curCoords);
            if (curColor == vec4(0.0))
            {
                found = true;   
                break;
            }
        }

        if (found)
            break;
    }
    
    if (!found)
        return;

    for (int x = -uSize; x <= uSize; ++x) 
    {
        for (int y = -uSize; y <= uSize; ++y) 
        {
            ivec2 curCoords = ivec2(coords + ivec2(x, y));
            vec4 curColor = imageLoad(srcImage, curCoords);

            if (curColor == vec4(0.0)) 
                imageStore(destImage, curCoords, color);
        }
    }
}