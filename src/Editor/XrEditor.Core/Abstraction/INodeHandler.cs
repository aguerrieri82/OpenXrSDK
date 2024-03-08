namespace XrEditor
{
    public interface INodeHandler
    {
        bool CanHandle(object value);

        INode CreateNode(object value);
    }
}
