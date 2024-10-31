using CanvasUI;
using XrEditor.Services;
using XrEngine;


[assembly: Module(typeof(XrEditor.Plot.Module))]

namespace XrEditor.Plot
{
    public class Module : IModule
    {
        public void Load()
        {
            PlotPanel? instance = null;
            Context.Require<PanelManager>().Register(()=>(instance ??= new PlotPanel()), "Plotter");
        }

        public void Shutdown()
        {
            AnimationManager.Instance.Stop();
        }   
    }
}

