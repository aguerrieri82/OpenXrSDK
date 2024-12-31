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
            pm.Register(() => new PropertiesEditor(PropertiesEditorMode.Selection, PropertiesEditor.PROPERTIES), PropertiesEditor.PROPERTIES, "Properties");
            pm.Register(() => new PropertiesEditor(PropertiesEditorMode.Custom, PropertiesEditor.TOOLS), PropertiesEditor.TOOLS, "Tools");
            pm.Register<OutlinePanel>();
            pm.Register<LogPanel>();
            pm.Register<AssetsPanel>();
        }


        public void Shutdown()
        {
        }
    }
}

