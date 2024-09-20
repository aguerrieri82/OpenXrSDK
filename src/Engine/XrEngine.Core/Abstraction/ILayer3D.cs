namespace XrEngine
{
    public interface ILayer3D : IObjectChangeListener
    {

        void Attach(LayerManager manager);

        void Detach();

        IEnumerable<ILayer3DItem> Content { get; }

        bool IsVisible { get; set; }

        ObjectId Id { get; }

        long Version { get; }

        string Name { get; set; }

        public bool IsEnabled { get; set; } 
    }
}
