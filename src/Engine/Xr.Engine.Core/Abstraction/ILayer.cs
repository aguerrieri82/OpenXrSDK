namespace Xr.Engine
{
    public interface ILayer : IObjectChangeListener
    {

        void Attach(LayerManager manager);

        void Detach();

        IEnumerable<ILayerObject> Content { get; }

        bool IsVisible { get; set; }

        ObjectId Id { get; }
    }
}
