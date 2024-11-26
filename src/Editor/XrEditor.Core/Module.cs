using System.Reflection;
using XrEditor.Services;
using XrEngine;


[assembly: Module(typeof(XrEditor.Module))]

namespace XrEditor
{
    public class Module : IModule
    {
        public void Load()
        {
            var pm = Context.Require<PanelManager>();
            pm.Register(() => new PropertiesEditor(PropertiesEditorMode.Selection, PropertiesEditor.PROPERTIES), PropertiesEditor.PROPERTIES);
            pm.Register(() => new PropertiesEditor(PropertiesEditorMode.Custom, PropertiesEditor.TOOLS), PropertiesEditor.TOOLS);
            pm.Register<OutlinePanel>();
            pm.Register<LogPanel>();
        }


        public void Shutdown()
        {
        }
    }
}

