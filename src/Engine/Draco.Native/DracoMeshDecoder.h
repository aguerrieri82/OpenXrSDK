#pragma once

enum PontDataType {
	None = 0,
	Position = 0x1,
	Normal = 0x2,
	UV = 0x4,
	Color = 0x8
};

struct PointData {
	float Position[3];
	float Normal[3];
	float UV[2];
};

struct MeshData {
	unsigned int* Indices;
	size_t IndicesSize;
    PointData* Vertices;
	size_t VerticesSize;
	PontDataType Types;
	draco::Mesh* Mesh;
};

extern "C" {

	EXPORT int APIENTRY DecodeBuffer(char* buffer, size_t bufferSize, MeshData* meshData);

	EXPORT void APIENTRY ReadMesh(MeshData* meshData);

}
