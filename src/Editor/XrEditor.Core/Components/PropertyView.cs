using System.Collections;
using System.ComponentModel;
using System.Reflection;
using UI.Binding;
using XrEditor.Services;
using XrEngine;
using INotifyPropertyChanged = UI.Binding.INotifyPropertyChanged;

namespace XrEditor
{

    public class PropertyView : BaseView, IDisposable
    {

        public PropertyView()
        {

        }

        public static void CreateProperties(object obj, Type? objType, IList<PropertyView> result, INotifyPropertyChanged? propertyChanged = null)
        {
            CreateProperties(obj, objType, null, result, propertyChanged);
        }

        public static void CreateProperties(object obj, Type? objType, object? host, IList<PropertyView> result, INotifyPropertyChanged? propertyChanged, string? category = null)
        {
            var binding = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

            if (objType == null)
                objType = obj.GetType();
            else
                binding |= BindingFlags.DeclaredOnly;

            var manager = Context.Require<PropertyEditorManager>();

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

                var editor = manager.CreateEditor(valueType, field.GetCustomAttributes(), obj);

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

                var curCategory = category;

                if (string.IsNullOrWhiteSpace(curCategory))
                {
                    var catAttr = field.GetCustomAttribute<CategoryAttribute>();
                    if (catAttr != null)
                        curCategory = catAttr.Category;
                    else
                        curCategory = host != null ? obj.GetType().Name : null;
                }

                var propView = new PropertyView
                {
                    Label = field.Name,
                    Category = curCategory,
                    Editor = editor,
                };

                result.Add(propView);
            }

            foreach (var prop in objType.GetProperties(binding))
            {
                if (!prop.CanRead)
                    continue;

                var editableAttr = prop.GetCustomAttribute<EditableAttribute>();

                if (!prop.CanWrite && editableAttr == null)
                    continue;

                var editor = manager.CreateEditor(prop.PropertyType, prop.GetCustomAttributes(), obj);

                if (editor == null)
                {
                    if ((prop.PropertyType.IsClass || prop.PropertyType.IsInterface) && prop.PropertyType != typeof(string))
                    {
                        var value = prop.GetValue(obj);

                        if (value == null && editableAttr != null && editableAttr.AllowCreate)
                            value = Activator.CreateInstance(prop.PropertyType);

                        if (value != null)
                            CreateProperties(value, null, host ?? obj, result, propertyChanged, prop.Name);
                    }

                    continue;
                }

                var bindType = typeof(NotifyReflectionProperty<>).MakeGenericType(editor.ValueType);

                editor.Binding = (IProperty)Activator.CreateInstance(bindType, prop, obj, host)!;

                if (propertyChanged != null)
                    editor.Binding.Changed += (s, e) => propertyChanged.NotifyPropertyChanged(editor.Binding);

                var curCategory = category;

                if (string.IsNullOrWhiteSpace(curCategory))
                {
                    var catAttr = prop.GetCustomAttribute<CategoryAttribute>();
                    if (catAttr != null)
                        curCategory = catAttr.Category;
                    else
                        curCategory = host != null ? obj.GetType().Name : null;
                }

                var propView = new PropertyView
                {
                    Label = prop.Name,
                    Category = curCategory,
                    Editor = editor,
                };

                result.Add(propView);
            }
        }

        public void Dispose()
        {
            if (Editor is IDisposable disposable)
                disposable.Dispose();

            GC.SuppressFinalize(this);
        }

        public string? Label { get; set; }

        public string? Category { get; set; }

        public bool ReadOnly { get; set; }

        public IPropertyEditor? Editor { get; set; }

    }
}
