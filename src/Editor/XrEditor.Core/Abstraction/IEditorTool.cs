using XrEngine;

namespace XrEditor
{

    public interface IEditorTool
    {
        void Attach(SceneView view);

        void NotifySceneChanged();

        bool IsActive { get; }
    }
}
