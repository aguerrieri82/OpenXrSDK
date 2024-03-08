using XrEngine;

namespace XrEditor
{
    public class BaseToolState : IObjectState
    {

    }

    public interface IEditorTool
    {
        void Attach(SceneView view);

        void NotifySceneChanged();

        bool IsActive { get; }
    }
}
