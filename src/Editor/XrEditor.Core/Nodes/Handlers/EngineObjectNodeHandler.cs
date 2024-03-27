using XrEngine;

namespace XrEditor.Nodes
{
    public class EngineObjectNodeHandler : INodeHandler
    {
        public bool CanHandle(object value)
        {
            return value is EngineObject;
        }

        public INode CreateNode(object value)
        {
            if (value is EngineObject obj)
            {
                var node = obj.GetProp<INode>("Node");;
                Type nodeType;

                if (node == null)
                {
                    if (obj is Group3D)
                        nodeType = typeof(Group3DNode);

                    else if (obj is Light)
                        nodeType = typeof(LightNode<>).MakeGenericType(obj.GetType());

                    else if (obj is Camera)
                        nodeType = typeof(CameraNode<>).MakeGenericType(obj.GetType());

                    else if (obj is TriangleMesh)
                        nodeType = typeof(TriangleMeshNode);

                    else
                        nodeType = typeof(EngineObjectNode<>).MakeGenericType(obj.GetType());

                    node = (INode)Activator.CreateInstance(nodeType, obj)!;

                }

                if (node != null)
                {
                    obj.SetProp("Node", node);
                    return node;
                }
            }


            throw new NotSupportedException();
        }
    }
}
