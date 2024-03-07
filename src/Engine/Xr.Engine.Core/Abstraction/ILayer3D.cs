namespace Xr.Engine
{
    public interface ILayer3D : IObjectChangeListener
    {

        void Attach(LayerManager manager);

        void Detach();

        IEnumerable<ILayer3DObject> Content { get; }

        bool IsVisible { get; set; }

        ObjectId Id { get; }
    }
}
