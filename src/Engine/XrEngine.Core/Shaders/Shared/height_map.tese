#ifdef USE_HEIGHT_MAP

layout(triangles) in;

in vec2 tcTexCoords[]; // Input texture coordinates from tessellation control shader
out vec2 fUv;  // Pass to fragment shader
out vec3 fNormal;     // Pass the computed normal to the fragment shader

layout(binding=8) uniform sampler2D heightMap; // Height map texture

uniform float uHeightScale;   // Scale factor for height
uniform vec2 uHeightTexSize;      // Size of one texel in texture space

void main() {

    vec2 texelSize = 1.0 / uHeightTexSize;

    // Interpolate texture coordinates
    fUv = mix(
        mix(tcTexCoords[0], tcTexCoords[1], gl_TessCoord.x),
        mix(tcTexCoords[3], tcTexCoords[2], gl_TessCoord.x),
        gl_TessCoord.y
    );


    // Sample height map to get displacement
    float height = texture(heightMap, fUv).r * uHeightScale;

    // Interpolate world position
    vec3 worldPos = mix(
        mix(gl_in[0].gl_Position.xyz, gl_in[1].gl_Position.xyz, gl_TessCoord.x),
        mix(gl_in[3].gl_Position.xyz, gl_in[2].gl_Position.xyz, gl_TessCoord.x),
        gl_TessCoord.y
    );

    // Apply displacement
    worldPos.y += height;

    // Compute normals using finite differences
    float hL = texture(heightMap, fUv - vec2(texelSize.x, 0.0)).r * uHeightScale; // Left
    float hR = texture(heightMap, fUv + vec2(texelSize.x, 0.0)).r * uHeightScale; // Right
    float hD = texture(heightMap, fUv - vec2(0.0, texelSize.y)).r * uHeightScale; // Down
    float hU = texture(heightMap, fUv + vec2(0.0, texelSize.y)).r * uHeightScale; // Up

    fNormal = normalize(vec3(
        hL - hR,          // x-component (gradient along x-axis)
        2.0 * texelSize.x, // y-component (scale by texel size)
        hD - hU           // z-component (gradient along z-axis)
    ));


    gl_Position = vec4(worldPos, 1.0);
}

#else

layout(triangles) in;

void main() {
    gl_Position = gl_in[0].gl_Position;
}

#endif