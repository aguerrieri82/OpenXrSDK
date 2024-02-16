#pragma once

enum AttributeType : uint8_t {
	Position = 0,
	Normal = 1,
	Color = 2,
	UV = 3,
	Other = 4
};


constexpr auto MAX_ATTRIBUTES = 16;

struct MeshData {
	uint32_t IndicesCount;
	uint32_t VerticesCount;
	uint32_t AttributeCount;
	AttributeType Attributes[MAX_ATTRIBUTES];
	draco::Mesh* Mesh;
};


extern "C" {

	EXPORT int APIENTRY DecodeBuffer(char* buffer, size_t bufferSize, MeshData* meshData);

	EXPORT void APIENTRY DisposeMesh(draco::Mesh* Mesh);

	EXPORT void APIENTRY ReadIndices(draco::Mesh*, uint32_t* buffer, int itemCount);

	EXPORT void APIENTRY ReadAttribute(draco::Mesh*, uint32_t index, char* buffer, int itemSize, int itemCount);
}
