namespace XrEditor
{
    public interface INodeChanged : INode
    {
        event EventHandler NodeChanged;
    }
}
