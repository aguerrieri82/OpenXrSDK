﻿using System.Reflection;
using UI.Binding;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{

    public class PropertyView : BaseView
    {

        public PropertyView()
        {

        }

        public static void CreateProperties(object obj, Type objType, IList<PropertyView> properties, INotifyPropertyChanged? propertyChanged = null)
        {
            CreateProperties(obj, objType, null, properties, propertyChanged);
        }

        public static void CreateProperties(object obj, Type objType, object? host, IList<PropertyView> properties, INotifyPropertyChanged? propertyChanged)
        {
            foreach (var prop in objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite || !prop.CanRead)
                    continue;

                var editor = Context.Require<PropertyEditorManager>().CreateEditor(prop.PropertyType, prop.GetCustomAttributes());

                if (editor == null)
                {
                    if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    {
                        var value = prop.GetValue(obj);
                        if (value != null)
                            CreateProperties(value, value.GetType(), host ?? obj, properties, propertyChanged);
                    }

                    continue;
                }

                var bindType = typeof(NotifyReflectionProperty<>).MakeGenericType(editor.ValueType);

                editor.Binding = (IProperty)Activator.CreateInstance(bindType, prop, obj, host)!;

                if (propertyChanged != null)
                    editor.Binding.Changed += (s, e) => propertyChanged.NotifyPropertyChanged(editor.Binding);

                var propView = new PropertyView
                {
                    Label = prop.Name,
                    Category = host != null ? obj.GetType().Name : null,
                    Editor = editor,
                };

                properties.Add(propView);
            }
        }

        public string? Label { get; set; }

        public string? Category { get; set; }

        public bool ReadOnly { get; set; }

        public IPropertyEditor? Editor { get; set; }


    }
}
