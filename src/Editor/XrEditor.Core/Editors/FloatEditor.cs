namespace XrEditor
{
    public class FloatEditor : BaseEditor<float>
    {
        private float _min;
        private float _max;

        public FloatEditor()
        {
            _max = 2f;
            _min = -2f;
        }

        protected override void OnValueChanged(float newValue)
        {
            if (newValue > _max)
                Max = newValue;

            if (newValue < _min)
                Min = newValue;

            base.OnValueChanged(newValue);
        }

        public float Min
        {
            get => _min;
            set
            {
                if (_min == value)
                    return;
                _min = value;
                OnPropertyChanged(nameof(Min));
            }
        }

        public float Max
        {
            get => _max;
            set
            {
                if (_max == value)
                    return;
                _max = value;
                OnPropertyChanged(nameof(Max));
            }
        }
    }
}
