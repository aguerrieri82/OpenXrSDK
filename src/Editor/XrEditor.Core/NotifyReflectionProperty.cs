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

        protected override async void OnChanged()
        {
            base.OnChanged();

            if (_host is EngineObject obj)
            {
                await EngineApp.MainThread;

                var change = ChangeType.Property;

                if (obj is Material)
                    change |= ChangeType.Material;

                obj.NotifyChanged(new ObjectChange
                {
                    Type = change,
                    Target = obj,
                    Properties = [Name!]
                });
            }

            if (_host is INotifyPropertyChangedReceiver recv)
            {
                recv.OnPropertyChanged(Name!, Value);
            }
        }
    }
}
