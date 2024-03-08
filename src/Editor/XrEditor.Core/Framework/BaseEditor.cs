#pragma warning disable CS8618 

namespace XrEditor
{
    public abstract class BaseEditor<TValue> : BaseView, IPropertyEditor
    {
        private TValue _value;

        protected virtual void OnValueChanged(TValue newValue)
        {
            ValueChanged?.Invoke(this, newValue);
        }

        public virtual void NotifyValueChanged()
        {
            OnPropertyChanged(nameof(Value));
        }

        public TValue Value
        {
            get => _value;
            set
            {
                if (Equals(Value, value))
                    return;
                _value = value;
                OnPropertyChanged(nameof(Value));
                OnValueChanged(Value);
            }
        }

        object IPropertyEditor.Value
        {
            get => Value!;
            set => Value = (TValue)value;
        }


        public EventHandler<TValue>? ValueChanged;

    }
}
