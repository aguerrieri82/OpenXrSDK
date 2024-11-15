namespace XrEngine
{
    public enum Layer3DChangeType
    {
        Added,
        Removed
    }

    public class Layer3DChange
    {
        public Layer3DChange(Layer3DChangeType type, ILayer3DItem item)
        {
            Type = type;    
            Item = item;
        }

        public readonly Layer3DChangeType Type;

        public readonly ILayer3DItem Item;
    }

    public interface ILayer3D : IObjectChangeListener
    {

        void Attach(LayerManager manager);

        void Detach();

        IEnumerable<ILayer3DItem> Content { get; }

        bool IsVisible { get; set; }

        ObjectId Id { get; }

        long Version { get; }

        string Name { get; set; }

        bool IsEnabled { get; set; }


        event Action<ILayer3D, Layer3DChange>? Changed;
    }
}
