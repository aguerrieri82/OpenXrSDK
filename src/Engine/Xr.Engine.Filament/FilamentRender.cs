namespace Xr.Engine.Filament
{
    public class FilamentRender : IRenderEngine
    {
        public Rect2I View => throw new NotImplementedException();

        public FilamentRender()
        {
            var options = new FilamentLib.InitializeOptions();
            FilamentLib.Initialize(ref options);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Texture2D? GetDepth()
        {
            throw new NotImplementedException();
        }

        public void Render(Scene scene, Camera camera, Rect2I view)
        {
            throw new NotImplementedException();
        }
    }
}
