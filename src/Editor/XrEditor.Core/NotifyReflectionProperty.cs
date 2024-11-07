using System.Reflection;
using UI.Binding;
using XrEngine;

namespace XrEditor
{
    public class NotifyReflectionProperty<T> : ReflectionProperty<T>
    {
        readonly object? _host;

        public NotifyReflectionProperty(PropertyInfo property, object obj, object? host = null)
            : base(property, obj)
        {
            _host = host ?? obj;
        }

        protected override void OnChanged()
        {
            base.OnChanged();

            if (_host is EngineObject obj)
                obj.NotifyChanged(new ObjectChange
                {
                    Type = ObjectChangeType.Property,
                    Target = obj,
                    Properties = [Name!]
                });

        }
    }
}
