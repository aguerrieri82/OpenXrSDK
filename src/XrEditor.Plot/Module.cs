using XrEditor.Services;
using XrEngine;


[assembly: Module(typeof(XrEditor.Plot.Module))]

namespace XrEditor.Plot
{
    public class Module : IModule
    {
        public void Load()
        {
            var panel = new PlotPanel();

            Context.Require<PanelManager>().Register(() => panel, panel.PanelId);
        }
    }
}

