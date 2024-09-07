using System.Reflection;
using UI.Binding;
using XrEngine;

namespace XrEditor
{
    public class NotifyReflectionProperty<T> : ReflectionProperty<T>
    {
        public NotifyReflectionProperty(PropertyInfo property, object obj) 
            : base(property, obj)
        {
        }

        protected override void OnChanged()
        {
            base.OnChanged();

            if (_object is EngineObject obj)
                obj.NotifyChanged(new ObjectChange
                {
                    Type = ObjectChangeType.Property,
                    Target = obj,
                    Properties = [Name!]
                });

        }
    }
}
