// Define workgroup size
layout (local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

// Image bindings (removed 'restrict' as it's not a valid GLSL qualifier)
layout (binding = 0, rgba8) readonly uniform highp image2D srcImage;
layout (binding = 1, rgba8) writeonly uniform highp image2D destImage;

// Uniform parameters
uniform int uSize; // Neighborhood size

// Define maximum uSize to ensure shared memory bounds (e.g., uSize <= 4)
const int MAX_USIZE = 4;

// Calculate shared memory dimensions based on uSize
// Shared memory includes the workgroup's tile plus a border of uSize
const int TILE_SIZE_X = 16;
const int TILE_SIZE_Y = 16;
const int SHARED_SIZE_X = TILE_SIZE_X + 2 * MAX_USIZE;
const int SHARED_SIZE_Y = TILE_SIZE_Y + 2 * MAX_USIZE;

// Declare shared memory as a fixed-size 1D array
shared vec4 sharedSrcImage[SHARED_SIZE_X * SHARED_SIZE_Y];

void main() {
    // Get global and local invocation IDs
    ivec2 globalID = ivec2(gl_GlobalInvocationID.xy);
    ivec2 localID = ivec2(gl_LocalInvocationID.xy);

    // Get the dimensions of the source image
    ivec2 imgSize = imageSize(srcImage);

    // Clamp uSize to MAX_USIZE to prevent excessive shared memory usage
    int radius = clamp(uSize, 0, MAX_USIZE);

    // Calculate the starting global coordinates for the shared memory tile
    ivec2 sharedStart = globalID - ivec2(radius, radius);

    // Each thread loads multiple pixels to cover the shared memory tile
    for(int y = 0; y < 2 * radius + 1; y++) {
        for(int x = 0; x < 2 * radius + 1; x++) {
            ivec2 offset = ivec2(x, y);
            ivec2 srcCoord = sharedStart + offset;

            // Calculate shared memory index
            int sharedIndex = (localID.y + y) * SHARED_SIZE_X + (localID.x + x);

            // Perform bounds checking
            if(all(greaterThanEqual(srcCoord, ivec2(0))) && all(lessThan(srcCoord, imgSize))) {
                sharedSrcImage[sharedIndex] = imageLoad(srcImage, srcCoord);
            }
            else {
                // Assign a default value (e.g., transparent black) for out-of-bounds
                sharedSrcImage[sharedIndex] = vec4(0.0);
            }
        }
    }

    // Synchronize to ensure all shared memory loads are complete
    barrier();

    // Calculate the local coordinates within shared memory
    ivec2 localSharedCoord = localID + ivec2(radius, radius);

    // Load the current pixel color from shared memory
    vec4 currentColor = sharedSrcImage[localSharedCoord.y * SHARED_SIZE_X + localSharedCoord.x];

    // Initialize the destination pixel to black
    imageStore(destImage, globalID, vec4(0.0));

    // If the current pixel is black, no further processing is needed
    if (currentColor == vec4(0.0)) {
        return;
    }

    bool found = false;

    // Search for any black pixel within the neighborhood
    for(int y = -radius; y <= radius && !found; ++y) {
        for(int x = -radius; x <= radius && !found; ++x) {
            ivec2 neighborCoord = localSharedCoord + ivec2(x, y);
            vec4 neighborColor = sharedSrcImage[neighborCoord.y * SHARED_SIZE_X + neighborCoord.x];

            if (neighborColor == vec4(0.0)) {
                found = true;
            }
        }
    }

    // If no black neighbor is found, exit
    if (!found) {
        return;
    }

    // Iterate again to update black neighbors with the current color
    for(int y = -radius; y <= radius; ++y) {
        for(int x = -radius; x <= radius; ++x) {
            ivec2 neighborCoord = localSharedCoord + ivec2(x, y);
            vec4 neighborColor = sharedSrcImage[neighborCoord.y * SHARED_SIZE_X + neighborCoord.x];
            ivec2 globalNeighbor = globalID + ivec2(x, y);

            // Perform bounds checking
            if(all(greaterThanEqual(globalNeighbor, ivec2(0))) && all(lessThan(globalNeighbor, imgSize))) {
                if (neighborColor == vec4(0.0)) {
                    imageStore(destImage, globalNeighbor, currentColor);
                }
            }
        }
    }
}