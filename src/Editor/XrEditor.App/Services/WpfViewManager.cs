using System.Reflection;
using System.Windows;
using XrEngine;

namespace XrEditor.Services
{
    public class WpfViewManager : IViewManager
    {
        public void AddView<T>(string path) where T : IModule
        {
            Assembly assembly = typeof(T).Assembly;
            string? name = assembly.GetName().Name;
            string resId = $"/{name};component/{path}";
            Resources.Add((ResourceDictionary)Application.LoadComponent(new Uri(resId, UriKind.Relative)));
        }

        public IList<ResourceDictionary> Resources { get; } = [];
    }
}
