#pragma once


struct Vector3F {
	float X;
	float Y;
	float Z;
};

struct IkContext {
	Tree* tree;
	std::vector<Node*> nodes;
	Jacobian* jac;
	std::vector<VectorR3> targets;
};

enum IkUpdateMethod
{
	IK_JACOB_TRANS = 0,
	IK_PURE_PSEUDO,
	IK_DLS,
	IK_SDLS,
	IK_DLS_SVD
};


extern "C" {

	EXPORT IkContext* IkCreate(UINT nodeCount, UINT targetCount);

	EXPORT void IkCreateNode(IkContext* ctx, UINT index, const Vector3F attach, const Vector3F v, float size, Purpose purpose, float minTheta, float maxTheta, float restAngle);

	EXPORT void IkInsertLeftChild(IkContext* ctx, UINT parentIndex, UINT childIndex);

	EXPORT void IkInsertRightSibling(IkContext* ctx, UINT existingNode, UINT childIndex);

	EXPORT void IkInsertRoot(IkContext* ctx, UINT index);

	EXPORT float IkGetNodeTheta(IkContext* ctx, UINT index);

	EXPORT void IkSetTarget(IkContext* ctx, UINT index, Vector3F pos);

	EXPORT void IkUpdate(IkContext* ctx, IkUpdateMethod method, bool updateThetas);

	EXPORT void IkDelete(IkContext* ctx);

	EXPORT void IkInit(IkContext* ctx);

	EXPORT void IkReset(IkContext* ctx);
}