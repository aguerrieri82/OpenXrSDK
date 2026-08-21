layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUV;

out vec3 vVoxelUv;
flat out int vFace;

uniform mat4 uModelViewProj;

uniform vec3 uOrigin;
uniform float uVoxelSize;
uniform ivec3 uGridSize;

uniform ivec3 uSliceMin;
uniform ivec3 uSliceMax;

int FaceFromNormal(vec3 n)
{
	vec3 a = abs(n);

	if (a.x >= a.y && a.x >= a.z)
		return n.x < 0.0 ? 0 : 1;

	if (a.y >= a.x && a.y >= a.z)
		return n.y < 0.0 ? 2 : 3;

	return n.z < 0.0 ? 4 : 5;
}

#ifdef USE_SLICE

bool IsInsideSlice(ivec3 voxel)
{
	if (uSliceMin.x != 0 && voxel.x < uSliceMin.x)
		return false;

	if (uSliceMax.x != 0 && voxel.x > uSliceMax.x)
		return false;

	if (uSliceMin.y != 0 && voxel.y < uSliceMin.y)
		return false;

	if (uSliceMax.y != 0 && voxel.y > uSliceMax.y)
		return false;

	if (uSliceMin.z != 0 && voxel.z < uSliceMin.z)
		return false;

	if (uSliceMax.z != 0 && voxel.z > uSliceMax.z)
		return false;

	return true;
}

#endif

void main()
{
	int x = gl_InstanceID % uGridSize.x;
	int y = (gl_InstanceID / uGridSize.x) % uGridSize.y;
	int z = gl_InstanceID / (uGridSize.x * uGridSize.y);

	ivec3 voxelIndex = ivec3(x, y, z);
	vec3 voxel = vec3(voxelIndex);

#ifdef USE_SLICE

	if (!IsInsideSlice(voxelIndex))
	{
		gl_Position = vec4(3.0, 3.0, 3.0, 1.0);
		return;
	}

#endif

	int face = FaceFromNormal(aNormal);
	vFace = face;

	vVoxelUv = (voxel + vec3(0.5)) / vec3(uGridSize);

	if (FACE >= 0 && face != FACE)
	{
		gl_Position = vec4(3.0, 3.0, 3.0, 1.0);
		return;
	}

	vec3 local01 = aPosition + vec3(0.5);
	vec3 world = uOrigin + (voxel + local01) * uVoxelSize;

	gl_Position = uModelViewProj * vec4(world, 1.0);
}