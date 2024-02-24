namespace Xr.Engine
{
    public interface IObjectChangeListener
    {
        void NotifyChanged(Object3D object3D, ObjectChange change);
    }
}
