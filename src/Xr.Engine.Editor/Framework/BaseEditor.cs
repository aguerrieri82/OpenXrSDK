using System.Windows;

namespace Xr.Engine.Editor
{
    public abstract class BaseEditor<TValue> : BaseView, IPropertyEditor
    {
        public static readonly DependencyProperty ValueProperty =
               DependencyProperty.Register(
                   name: "Value",
                   propertyType: typeof(TValue),
                   ownerType: typeof(BaseEditor<TValue>),
                   typeMetadata: new FrameworkPropertyMetadata(OnValueChanged));


        static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Equals((TValue)e.NewValue, (TValue)e.OldValue))
                return;

            ((BaseEditor<TValue>)d).OnValueChanged((TValue)e.NewValue);
        }

        protected virtual void OnValueChanged(TValue newValue)
        {
            ValueChanged?.Invoke(this, newValue);
        }

        public TValue Value
        {
            get => (TValue)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        object IPropertyEditor.Value
        {
            get => Value;
            set => Value = (TValue)value;
        }


        public EventHandler<TValue>? ValueChanged;
    }
}
