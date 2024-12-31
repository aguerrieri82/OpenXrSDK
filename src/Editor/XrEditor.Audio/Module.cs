using CanvasUI;
using System.Reflection;
using XrEditor.Services;
using XrEngine;


[assembly: Module(typeof(XrEditor.Audio.Module))]

namespace XrEditor.Audio
{
    public class Module : IModule
    {
        public void Load()
        {
            AddPanel<LoopEditorPanel>("Views/LoopEditorPanel.xaml");
        }

        protected void AddPanel<T>(string viewPath) where T : class, IPanel, new()
        {
            Context.Require<PanelManager>().Register<T>();
            Context.Require<IViewManager>().AddView<Module>(viewPath);
        }

        public void Shutdown()
        {
            AnimationManager.Instance.Stop();
        }
    }
}

