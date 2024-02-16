#include "Library.h"

using namespace draco;

template<class T> inline T operator| (T a, T b) { return (T)((int)a | (int)b); }

template<class T> inline T& operator|= (T& a, T b) { return (T&)((int&)a |= (int)b); }


void ReadMesh(MeshData* meshData) {

	auto mesh = meshData->Mesh;

	if (mesh == nullptr)
		return;

	if (meshData->Vertices == nullptr || meshData->Indices == nullptr)
		return;

	if (meshData->VerticesSize < mesh->num_points())
		return;

	if (meshData->IndicesSize < mesh->num_faces() * 3)
		return;


	uint32_t idx = 0;
	for (uint32_t i = 0; i < mesh->num_faces(); i++) {
		auto face = mesh->face(FaceIndex(i));
		for (auto j = 0; j < 3; j++)
			meshData->Indices[idx++] = face[j].value();
	}

	meshData->Types = PontDataType::None;

	for (auto i = 0; i < mesh->num_attributes(); i++) {
		auto attr = mesh->attribute(i);
		auto type = attr->attribute_type();
		if (type == GeometryAttribute::Type::POSITION) {

			meshData->Types |= PontDataType::Position;
			for (int j = 0; j < mesh->num_points(); j++)
				attr->GetMappedValue(PointIndex(j), &meshData->Vertices[j].Position);
		}
		if (type == GeometryAttribute::Type::NORMAL) {

			meshData->Types |= PontDataType::Normal;
			for (int j = 0; j < mesh->num_points(); j++)
				attr->GetMappedValue(PointIndex(j), &meshData->Vertices[j].Normal);
		}
		if (type == GeometryAttribute::Type::TEX_COORD) {
			meshData->Types |= PontDataType::UV;
			for (int j = 0; j < mesh->num_points(); j++)
				attr->GetMappedValue(PointIndex(j), &meshData->Vertices[j].UV);
		}

        if (type == GeometryAttribute::Type::COLOR) {
            meshData->Types |= PontDataType::Color;
            for (int j = 0; j < mesh->num_points(); j++)
                attr->GetMappedValue(PointIndex(j), &meshData->Vertices[j].Color);
        }
	}

	delete meshData->Mesh;
	meshData->Mesh = nullptr;
}

int DecodeBuffer(char* buffer, size_t bufferSize, MeshData* meshData) {
	Decoder decoder;
	DecoderBuffer decBuffer;
	
	Mesh* mesh = new Mesh();

	decBuffer.Init(buffer, bufferSize);
	auto type = decoder.GetEncodedGeometryType(&decBuffer);
	if (type.value() != EncodedGeometryType::TRIANGULAR_MESH)
		return -1;

	auto status = decoder.DecodeBufferToGeometry(&decBuffer, mesh);

	if (status.code() != Status::Code::OK)
		return -2;	

	meshData->IndicesSize = mesh->num_faces() * 3;
	meshData->VerticesSize = mesh->num_points();
	meshData->Mesh = mesh;
	return 0;

	
	
	return 1;
}