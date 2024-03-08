namespace Xr.Editor
{
    public interface INodeHandler
    {
        bool CanHandle(object value);

        INode CreateNode(object value);
    }
}
