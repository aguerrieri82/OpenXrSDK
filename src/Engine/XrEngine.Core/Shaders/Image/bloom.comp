
layout (local_size_x = 16, local_size_y = 16, local_size_z=1) in;

#ifdef IS_ARRAY

layout (binding = 0, rgba16f) uniform highp image2DArray image;

#else

layout (binding = 0, rgba16f) uniform highp image2D image;

#endif

const float kernel[9] = float[9](
    0.028532, 0.067234, 0.124946, 0.179044, 0.200488, 
    0.179044, 0.124946, 0.067234, 0.028532
);

uniform float uScale; 

void main() {

    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);

    #ifdef IS_ARRAY
        vec4 color = imageLoad(image, ivec3(gl_GlobalInvocationID));
    #else
        vec4 color = imageLoad(image, ivec2(gl_GlobalInvocationID.xy));
    #endif

	float brightness = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));

    if (length(color.rgb) >= 2.0) {
        
        color = vec4(0.0);

        for (int i = -4; i <= 4; ++i) {
            #ifdef IS_ARRAY
                #if MODE == 0
                    ivec3 curCoords = ivec3(coords + ivec2(0, i), gl_GlobalInvocationID.z); 
                #else
                    ivec3 curCoords = ivec3(coords + ivec2(i, 0), gl_GlobalInvocationID.z); 
                #endif
            #else
                #if MODE == 0
                    ivec2 curCoords = ivec2(coords + ivec2(0, i));
                #else
                    ivec2 curCoords = ivec2(coords + ivec2(i, 0));
                #endif
            #endif

                       
            vec4 curColor = imageLoad(image, curCoords);

            color += kernel[i + 4] * curColor;
        }

        color*= uScale;

       
        #ifdef IS_ARRAY
            imageStore(image, ivec3(gl_GlobalInvocationID), color) ;
        #else
            imageStore(image, ivec2(gl_GlobalInvocationID.xy), color);
        #endif

       //imageStore(image, ivec2(gl_GlobalInvocationID.xy), vec4(1.0,0.0,0.0,1.0));
    }

}