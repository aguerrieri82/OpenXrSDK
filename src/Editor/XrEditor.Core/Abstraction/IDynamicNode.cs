namespace XrEditor
{
    public delegate void ChildNodeDelegate(INode sender, INode child);

    public interface IDynamicNode : INode
    {

        event ChildNodeDelegate ChildAdded;

        event ChildNodeDelegate ChildRemoved;
    }
}
