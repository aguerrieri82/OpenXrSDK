using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.Services;

namespace XrEditor
{
    public class MainToolbarView : BaseView
    {
        private bool _isMinimized;

        public MainToolbarView()
        {
            SaveCommand = new Command(Save);
            LoadCommand = new Command(Load);
            IsMinimized = true;
        }

        public async Task Save()
        {
            var container = new JsonStateContainer();   
            EngineApp.Current!.ActiveScene!.GetState(container);
            var json = container.AsJson();
            File.WriteAllText("scene.json", container.AsJson());  

        }

        public async Task Load()
        {
            IsMinimized = true;
        }

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                _isMinimized = value;
                OnPropertyChanged(nameof(IsMinimized));
            }
        }

        public Command SaveCommand { get; }

        public Command LoadCommand { get; }
    }
}
