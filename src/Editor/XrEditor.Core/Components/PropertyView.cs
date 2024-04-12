﻿using System.Reflection;
using UI.Binding;
using XrEngine;

namespace XrEditor
{
    public class PropertyView : BaseView
    {

        public PropertyView()
        {

        }

        public static void CreateProperties(object obj, Type objType, IList<PropertyView> properties)
        {
            foreach (var prop in objType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite || !prop.CanRead)
                    continue;

                var editor = Context.Require<PropertyEditorManager>().CreateEditor(prop.PropertyType, prop.GetCustomAttributes());

                if (editor == null)
                {
                    if (prop.PropertyType.IsClass)
                    {
                        var value = prop.GetValue(obj);
                        if (value != null)
                            CreateProperties(value, value.GetType(), properties);
                    }

                    continue;
                }

                var bindType = typeof(ReflectionProperty<>).MakeGenericType(editor.ValueType);

                editor.Binding = (IProperty)Activator.CreateInstance(bindType, prop, obj)!;

                var propView = new PropertyView
                {
                    Label = prop.Name,
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
