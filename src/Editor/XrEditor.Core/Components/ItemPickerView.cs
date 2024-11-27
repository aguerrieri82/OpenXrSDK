using CanvasUI;
using CefSharp.DevTools.Accessibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrMath;

namespace XrEditor
{
    public class ItemPickerView : BaseView
    {
        private string? _query;
        private ItemView? _selectedItem;
        private IPopup? _popup;
        private int _isRefreshing;

        public ItemView? SelectedItem
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

        public string? Query
        {
            get => _query;
            set
            {
                if (_query == value)
                    return;
                _query = value;

                OnPropertyChanged(nameof(Query));

                _ = RefreshAsync();
            }
        }

        public void Select()
        {
            _popup?.Close(); 
        }

        public async Task RefreshAsync()
        {
            if (_isRefreshing >0)
                return;

            _isRefreshing++;

            try
            {
                var objs = await Task.Run(() => ItemsSource?.Filter(_query) ?? []);

                Items = objs.Select(a => new ItemView
                {
                    Text = ItemsSource!.GetText(a),
                    Value = a
                }).OrderBy(a => a.Text).ToArray();

                OnPropertyChanged(nameof(Items));
            }
            finally
            {
                _isRefreshing--;
            }
        }

        public IList<ItemView>? Items { get; set; }
        

        public IItemsSource? ItemsSource { get; set; }   

        public async Task<object?> ShowAsync(string title)
        {
            var manager = Context.Require<IWindowManager>();

            var content = new ContentView()
            {
                Title = title,
                Content = this,
                Actions = [

                    new ActionView
                    {
                        DisplayName = "Cancel",
                    },
                    new ActionView
                    {
                        DisplayName = "Select",
                    },
                ]
            };

            _popup = manager.CreatePopup(content, new Size2I(300, 400));

            await RefreshAsync();

            var result = await _popup.ShowAsync();

            _popup = null;

            if (result?.DisplayName == "Select")
                return SelectedItem?.Value;

            return null;
        }

    }
}
