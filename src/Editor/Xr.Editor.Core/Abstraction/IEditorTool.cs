using Xr.Engine;

namespace Xr.Editor
{
    public class BaseToolState : IObjectState
    {

    }

    public interface IEditorTool
    {
        void Attach(SceneView view);

        bool IsActive { get; }
    }
}
