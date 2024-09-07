using XrEngine;
using XrEngine.Physics;

namespace XrEditor.Nodes
{
    public class EngineObjectNodeHandler : INodeHandler
    {
        public bool CanHandle(object value)
        {
            return value is EngineObject || value is IComponent;
        }

        public INode CreateNode(object value)
        {
            Type nodeType;

            if (value is EngineObject obj)
            {
                var node = obj.GetProp<INode>("Node");

                if (node == null)
                {
                    if (obj is ImageLight)
                        nodeType = typeof(ImageLightNode);

                    else if (obj is TriangleMesh)
                        nodeType = typeof(TriangleMeshNode);

                    else if (obj is PbrMaterial)
                        nodeType = typeof(PbrMaterialNode);

                    else if (obj is Geometry3D)
                        nodeType = typeof(Geometry3DNode);

                    else if (obj is Texture2D)
                        nodeType = typeof(Texture2DNode);

                    else if (obj is Light)
                        nodeType = typeof(LightNode<>).MakeGenericType(obj.GetType());

                    else if (obj is Camera)
                        nodeType = typeof(CameraNode<>).MakeGenericType(obj.GetType());

                    else if (obj is Material)
                        nodeType = typeof(MaterialNode<>).MakeGenericType(obj.GetType());

                    else if (obj is Group3D)
                        nodeType = typeof(Group3DNode<>).MakeGenericType(obj.GetType());

                    else if (obj is Object3D)
                        nodeType = typeof(Object3DNode<>).MakeGenericType(obj.GetType());

                    else
                        nodeType = typeof(EngineObjectNode<>).MakeGenericType(obj.GetType());

                    node = (INode)Activator.CreateInstance(nodeType, obj)!;

                    if (obj is Object3D obj3d && obj3d.Parent != null)
                        ((IEditableNode)node).SetParent(CreateNode(obj3d.Parent));
                }

                if (node != null)
                {
                    obj.SetProp("Node", node);
                    return node;
                }
            }
            if (value is IComponent comp)
            {
                if (value is RigidBody)
                    nodeType = typeof(RigidBodyNode);
                else
                    nodeType = typeof(ComponentNode<>).MakeGenericType(comp.GetType());

                return (INode)Activator.CreateInstance(nodeType, comp)!;
            }

            throw new NotSupportedException();
        }
    }
}
