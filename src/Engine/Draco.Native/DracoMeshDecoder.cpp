#include "Library.h"

using namespace draco;


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

	meshData->IndicesCount = mesh->num_faces() * 3;
	meshData->VerticesCount = mesh->num_points();
	meshData->AttributeCount = mesh->num_attributes();

	for (auto i = 0; i < mesh->num_attributes(); i++) 
		meshData->Attributes[i] = (AttributeType)mesh->attribute(i)->attribute_type();
	
	meshData->Mesh = mesh;

	return 0;
}

void DisposeMesh(draco::Mesh* mesh)
{
	delete mesh;
}

void ReadIndices(draco::Mesh* mesh, uint32_t* buffer, int itemCount)
{
	if (itemCount < mesh->num_faces() * 3)
		return;

	uint32_t idx = 0;
	for (uint32_t i = 0; i < mesh->num_faces(); i++) {
		auto face = mesh->face(FaceIndex(i));
		for (auto j = 0; j < 3; j++)
			buffer[idx++] = face[j].value();
	}
}

void ReadAttribute(draco::Mesh* mesh, uint32_t index, char* buffer, int itemSize, int itemCount)
{
	if (itemCount < mesh->num_points())
		return;

	auto attr = mesh->attribute(index);
	for (int j = 0; j < mesh->num_points(); j++)
		attr->GetMappedValue(PointIndex(j), buffer + (j * itemSize));
}
