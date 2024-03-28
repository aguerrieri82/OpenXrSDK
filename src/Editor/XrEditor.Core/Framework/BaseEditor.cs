#pragma warning disable CS8618 

using Newtonsoft.Json.Linq;
using UI.Binding;

namespace XrEditor
{
    public abstract class BaseEditor<TValue, TEdit> : BaseView, IPropertyEditor
    {
        protected TEdit _editValue;
        protected IProperty<TValue>? _binding;

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
            return (TEdit)Convert.ChangeType(value, typeof(TEdit))!;
        }


        protected virtual TValue EditValueToBind(TEdit value)
        {
            return (TValue)Convert.ChangeType(value, typeof(TValue))!;
        }

        protected virtual void OnEditValueChanged(TEdit newValue)
        {
            if (_binding != null)
                _binding.Value = EditValueToBind(newValue);

            EditValueChanged?.Invoke(this, newValue);
        }

        protected virtual void OnBindValueChanged(TValue newValue)
        {
            EditValue = BindToEditValue(newValue);
        }

        private void OnBindValueChanged(object? sender, EventArgs e)
        {
            OnBindValueChanged(_binding!.Value);
        }

        object IPropertyEditor.Value
        {
            get => EditValue!;
            set => EditValue = (TEdit)value;
        }


        public EventHandler<TEdit>? EditValueChanged;
    }
}
