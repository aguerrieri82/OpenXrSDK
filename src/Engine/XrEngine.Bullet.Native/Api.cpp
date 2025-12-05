#include "pch.h"
#include "Devpkey.h"


EXPORT IkContext* IkCreate(UINT nodeCount, UINT targetCount)
{
    IkContext* ctx = new IkContext();
    ctx->tree = new Tree();

    ctx->nodes.resize(nodeCount, nullptr);
    ctx->targets.resize(targetCount);
    ctx->jac = nullptr;

    return ctx;
}

EXPORT void IkDelete(IkContext* ctx)
{
    for (auto* n : ctx->nodes)
        delete n;

    delete ctx->tree;
    delete ctx->jac;
    delete ctx;
}


EXPORT void IkCreateNode(
    IkContext* ctx, UINT index,
    const Vector3F attach, const Vector3F v,
    float size, Purpose purpose,
    float minTheta, float maxTheta, float restAngle)
{
    VectorR3 a(attach.X, attach.Y, attach.Z);
    VectorR3 ax(v.X, v.Y, v.Z);

    if (ctx->nodes[index] != nullptr)
        delete ctx->nodes[index];

    ctx->nodes[index] = new Node(a, ax, size, purpose, minTheta, maxTheta, restAngle);
}


void IkInsertRoot(IkContext* ctx, UINT index)
{
    ctx->tree->InsertRoot(ctx->nodes[index]);
}

void IkInsertLeftChild(IkContext* ctx, UINT parentIndex, UINT childIndex)
{
    ctx->tree->InsertLeftChild(ctx->nodes[parentIndex], ctx->nodes[childIndex]);
}

void IkInsertRightSibling(IkContext* ctx, UINT existingNode, UINT newSibling)
{
    ctx->tree->InsertRightSibling(ctx->nodes[existingNode], ctx->nodes[newSibling]);
}

void IkSetTarget(IkContext* ctx, UINT index, Vector3F pos)
{
    ctx->targets[index] = VectorR3(pos.X, pos.Y, pos.Z);
}


void IkUpdate(IkContext* ctx, IkUpdateMethod method, bool updateThetas)
{
    ctx->jac->SetJendActive();

    ctx->jac->ComputeJacobian(&ctx->targets[0]);

    MatrixRmn augMat;

    switch (method)
    {
    case IK_JACOB_TRANS:
        ctx->jac->CalcDeltaThetasTranspose();  // Jacobian transpose method
        break;
    case IK_DLS:
        ctx->jac->CalcDeltaThetasDLS(augMat);  // Damped least squares method
        break;
    case IK_DLS_SVD:
        ctx->jac->CalcDeltaThetasDLSwithSVD();
        break;
    case IK_PURE_PSEUDO:
        ctx->jac->CalcDeltaThetasPseudoinverse();  // Pure pseudoinverse method
        break;
    case IK_SDLS:
        ctx->jac->CalcDeltaThetasSDLS();  // Selectively damped least squares method
        break;
    default:
        ctx->jac->ZeroDeltaThetas();
        break;
        break;
    }

    if (updateThetas) {
        ctx->jac->UpdateThetas();
        ctx->jac->UpdatedSClampValue(&ctx->targets[0]);
    }
}


float IkGetNodeTheta(IkContext* ctx, UINT index)
{
    Node* n = ctx->nodes[index];
    return n->theta;
}


EXPORT void IkInit(IkContext* ctx) 
{
    if (ctx->jac != nullptr)
        delete ctx->jac;
    
    ctx->jac = new Jacobian(ctx->tree);

    IkReset(ctx);
}

EXPORT void IkReset(IkContext* ctx)
{
    ctx->tree->Init();
    ctx->tree->Compute();
    ctx->jac->Reset();
}