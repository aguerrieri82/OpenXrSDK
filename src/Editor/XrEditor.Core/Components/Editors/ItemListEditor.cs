using CanvasUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class ItemListEditor : BaseEditor<object, object>, IDisposable
    {
        public class ItemListView
        {
            public INode? Node { get; set; }

            public string? DisplayText { get; set; }

            public IPropertyEditor? Editor { get; set; }

            public object? Value { get; set; }
        }

        private readonly ObservableCollection<ItemListView> _items = [];

        private ItemListView? _selectedItem;

        public ItemListEditor()
        {

        }

        protected override void OnEditValueChanged(object newValue)
        {
            base.OnEditValueChanged(newValue);

            _items.Clear();

            var factory = Context.Require<NodeManager>();


            foreach (var value in (IEnumerable)newValue)
            {
                var node = factory.CreateNode(value);

                var item = new ItemListView
                {
                    Value = value,
                    Node = node,
                };

                if (node is IItemView itemView)
                    item.DisplayText = itemView.DisplayName;
                else
                    item.DisplayText = value.ToString();

                _items.Add(item);
            }
        }

        public void Dispose()
        {
            foreach (var item in _items)
            {
                if (item.Editor is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public ItemListView? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value)
                    return;

                _selectedItem = value;

                if (_selectedItem != null)
                {
                    if (_selectedItem.Editor == null)
                    {
                        var manager = Context.Require<PropertyEditorManager>();
                        var editor = manager.CreateEditor(_selectedItem.Value!.GetType(), [], Host);
                        _selectedItem.Editor = editor;
                        if (editor != null)
                            editor!.Value = _selectedItem.Value!;
                    }
                }

                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        public bool CanEdit { get; set; }

        public ObservableCollection<ItemListView> Items => _items;

    }
}
