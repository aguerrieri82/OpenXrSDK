using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XrEngine;

namespace XrEditor.Services
{
    public class WpfViewManager : IViewManager
    {
        public void AddView<T>(string path) where T : IModule
        {
            var assembly = typeof(T).Assembly;  
            var name = assembly.GetName().Name; 
            var resId = $"/{name};component/{path}";
            Resources.Add((ResourceDictionary)Application.LoadComponent(new Uri(resId, UriKind.Relative)));
        }

        public IList<ResourceDictionary> Resources { get; } = []; 
    }
}
