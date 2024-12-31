using System.Reflection;
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

        public static void CreateProperties(object obj, Type? objType, IList<PropertyView> result, INotifyPropertyChanged? propertyChanged = null)
        {
            CreateProperties(obj, objType, null, result, propertyChanged);
        }

        public static void CreateProperties(object obj, Type? objType, object? host, IList<PropertyView> result, INotifyPropertyChanged? propertyChanged)
        {
            var binding = BindingFlags.Public | BindingFlags.Instance;

            if (objType == null)
                objType = obj.GetType();
            else
                binding |= BindingFlags.DeclaredOnly;

            foreach (var field in objType.GetFields(binding))
            {
                if (!typeof(IProperty).IsAssignableFrom(field.FieldType))
                    continue;

                var propType = field.FieldType
                    .GetInterfaces()
                    .FirstOrDefault(a => a.IsGenericType && a.GetGenericTypeDefinition() == typeof(IProperty<>));

                if (propType == null)
                    continue;

                var valueType = propType.GetGenericArguments()[0];

                var editor = Context.Require<PropertyEditorManager>().CreateEditor(valueType, field.GetCustomAttributes());

                if (editor == null)
                    continue;

                var fieldProp = (IProperty?)field.GetValue(obj);
                if (fieldProp == null)
                    continue;

                if (fieldProp is INameEdit nameEdit)
                    nameEdit.Name = field.Name;

                editor.Binding = fieldProp;

                if (propertyChanged != null)
                    editor.Binding.Changed += (s, e) => propertyChanged.NotifyPropertyChanged(editor.Binding);

                var propView = new PropertyView
                {
                    Label = field.Name,
                    Category = host != null ? obj.GetType().Name : null,
                    Editor = editor,
                };


                result.Add(propView);
            }


            foreach (var prop in objType.GetProperties(binding))
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
                            CreateProperties(value, null, host ?? obj, result, propertyChanged);
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

                result.Add(propView);
            }
        }

        public string? Label { get; set; }

        public string? Category { get; set; }

        public bool ReadOnly { get; set; }

        public IPropertyEditor? Editor { get; set; }


    }
}
