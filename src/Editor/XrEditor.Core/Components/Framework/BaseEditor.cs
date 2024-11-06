#pragma warning disable CS8618 

using UI.Binding;
using XrEngine;

namespace XrEditor
{
    public abstract class BaseEditor<TValue, TEdit> : BaseView, IPropertyEditor
    {
        protected TEdit _editValue;
        protected IProperty<TValue>? _binding;
        protected IDispatcher _dispatcher;
        protected internal int _isLoading;

        public BaseEditor()
        {
            _dispatcher = EngineApp.Current!.Dispatcher;
        }   

        public virtual void NotifyValueChanged()
        {
            OnPropertyChanged(nameof(EditValue));
        }

        public TEdit EditValue
        {
            get => _editValue;
            set
            {
                if (Equals(EditValue, value))
                    return;
                _editValue = value;
                OnPropertyChanged(nameof(EditValue));
                OnEditValueChanged(EditValue);
            }
        }

        public IProperty<TValue>? Binding
        {
            get => _binding;
            set
            {
                if (_binding == value)
                    return;

                if (_binding != null)
                    _binding.Changed -= OnBindValueChanged;

                _binding = value;

                if (_binding != null)
                {
                    _binding.Changed += OnBindValueChanged;
                    OnBindValueChanged(_binding.Value);
                }
            }
        }

        protected virtual TEdit BindToEditValue(TValue value)
        {
            if (value is IConvertible)
                return (TEdit)Convert.ChangeType(value, typeof(TEdit))!;

            return (TEdit)(object)value!;
        }


        protected virtual TValue EditValueToBind(TEdit value)
        {
            if (value is IConvertible)
                return (TValue)Convert.ChangeType(value, typeof(TValue))!;

            return (TValue)(object)value!;
        }

        protected virtual void OnEditValueChanged(TEdit newValue)
        {
            if (_isLoading > 0)
                return;

            if (_binding != null)
            {
                _dispatcher.ExecuteAsync(() =>
                {
                    _binding.Value = EditValueToBind(newValue);
                });
            }

            ValueChanged?.Invoke(this);
        }

        protected virtual void OnBindValueChanged(TValue newValue)
        {
            _isLoading++;
            try
            {
                EditValue = BindToEditValue(newValue);
            }
            finally
            {
                _isLoading--;   
            }
      
        }

        private void OnBindValueChanged(object? sender, EventArgs e)
        {
            OnBindValueChanged(_binding!.Value);
        }

        Type IPropertyEditor.ValueType => typeof(TValue);

        object IPropertyEditor.Value
        {
            get => EditValue!;
            set => EditValue = (TEdit)value;
        }

        IProperty? IPropertyEditor.Binding
        {
            get => Binding;
            set => Binding = (IProperty<TValue>?)value;
        }


        public event Action<IPropertyEditor>? ValueChanged;
    }
}
