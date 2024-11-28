using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using XrEngine;

namespace XrEditor
{


    [Panel("40f708c2-5c26-4b81-bc06-38b890cc9e5a")]
    [DisplayName("Assets")]
    public class AssetsPanel : BasePanel
    {
        private readonly ObservableCollection<NavItemView> _navItems = [];
        private readonly ObservableCollection<FolderItemView> _folderItems = [];
        private IAssetStore _store;
        private string _curPath;
        private FolderItemView? _selectedItem;

        public class NavItemView : BaseView
        {
            readonly AssetsPanel _host;

            public NavItemView(AssetsPanel host, string path)
            {
                Name = Path.GetFileName(path);
                if (string.IsNullOrWhiteSpace(Name))
                    Name = "Root";
                FullPath = path;
                SelectCommand = new Command(Select);
                _host = host;
            }

            public void Select()
            {
                _host.OpenPath(FullPath);
            }

            public Command SelectCommand { get; }

            public string Name { get; }

            public string FullPath { get; }
        }

        public class FolderItemView : BaseView
        {
            readonly AssetsPanel _host;

            private string _name;

            public FolderItemView(AssetsPanel host, string path, string icon, string color)
            {
                _host = host;
                _name = Path.GetFileName(path);
                Icon = icon;
                Color = color;
                FullPath = path;
            }


            public string? Color { get; set; }

            public string? Icon { get; set; }

            public string FullPath { get; }

            public string Name
            {
                get => _name;
                set
                {
                    if (_name == value)
                        return;
                    _name = value;
                    OnPropertyChanged(nameof(Name));    
                }
            }
        }

        public AssetsPanel()
        {
            _store = Context.Require<IAssetStore>();
            ToolBar = new ToolbarView();
            ToolBar.AddButton("icon_refresh", RefreshAsync);
            OpenPath("");
        }

        protected void UpdateNav()
        {
            var parts = _curPath.Replace('\\', '/')
                .Split('/')
                .Select(a => a.Trim())
                .Where(a=> a != "")
                .ToArray();

            var curPath = "";

            _navItems.Clear();
            _navItems.Add(new NavItemView(this, curPath));

            foreach (var part in parts)
            {
                curPath = Path.Join(curPath, part);
                _navItems.Add(new NavItemView(this, curPath));
            }
        }

        public override void OnActivate()
        {
            _ = RefreshAsync();
            base.OnActivate();
        }

        public void OpenSelected()
        {
            if (_selectedItem == null)
                return;
            OpenPath(_selectedItem.FullPath);
        }


        [MemberNotNull(nameof(_curPath))]
        public void OpenPath(string path)
        {
            _curPath = path;
            UpdateNav();
            _ = RefreshAsync();
            SelectedItem = null;
        }

        protected Task RefreshAsync()
        {
            _folderItems.Clear();

            foreach (var dirName in _store.ListDirectories(_curPath))
                _folderItems.Add(new FolderItemView(this, dirName, "icon_folder", "#ffff00"));

            return Task.CompletedTask;
        }

        public FolderItemView? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value)
                    return;
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        public ObservableCollection<FolderItemView> FolderItems => _folderItems;

        public ObservableCollection<NavItemView> NavItems => _navItems;

        public override string? Title => "Assets";
    }
}
