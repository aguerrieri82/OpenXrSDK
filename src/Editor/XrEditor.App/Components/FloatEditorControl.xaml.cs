using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XrEditor.Components
{
    public partial class FloatEditorControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(float), typeof(FloatEditorControl), new PropertyMetadata(0f, OnValueChanged));

        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register("Min", typeof(float), typeof(FloatEditorControl), new PropertyMetadata(0f));

        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register("Max", typeof(float), typeof(FloatEditorControl), new PropertyMetadata(1f));

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register("Step", typeof(float), typeof(FloatEditorControl), new PropertyMetadata(1f));

        public static readonly DependencyProperty SmallStepProperty =
            DependencyProperty.Register("SmallStep", typeof(float), typeof(FloatEditorControl), new PropertyMetadata(0.01f));

        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register("Format", typeof(Func<float, string>), typeof(FloatEditorControl), new PropertyMetadata(null, OnFormatChanged));

        public static readonly DependencyProperty ParseProperty =
            DependencyProperty.Register("Parse", typeof(Func<string?, float>), typeof(FloatEditorControl), new PropertyMetadata(null));

        public static readonly DependencyProperty TextValueProperty =
          DependencyProperty.Register("TextValue", typeof(string), typeof(FloatEditorControl), new PropertyMetadata(""));


        static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FloatEditorControl)d).OnValueChanged();
        }

        static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FloatEditorControl)d).OnFormatChanged();
        }

        bool _editMode;
        private Point _downPos;
        private float _downValue;
        private bool _isMoving;

        public FloatEditorControl()
        {
            InitializeComponent();
            UpdateControls();
            UpdateValue();
        }

        protected virtual void OnFormatChanged()
        {
            UpdateValue();
        }

        protected virtual void OnValueChanged()
        {
            UpdateValue();
        }

        protected void UpdateValue()
        {
            TextValue = Format != null ? Format(Value) : Value.ToString();
        }

        protected void UpdateControls()
        {
            text.Visibility = !_editMode ? Visibility.Visible : Visibility.Hidden;
            textBox.Visibility = _editMode ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnLeftClick(object sender, RoutedEventArgs e)
        {
            Value -= Step;
        }

        private void OnRightClick(object sender, RoutedEventArgs e)
        {
            Value += Step;
        }

        private void OnTextMouseDown(object sender, MouseButtonEventArgs e)
        {
            _downPos = e.GetPosition(this);
            _downValue = Value;
            _isMoving = true;
            text.CaptureMouse();
        }

        private void OnTextMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMoving)
                return;
            var pos = e.GetPosition(this);
            Value = _downValue + (int)(pos.X - _downPos.X) * SmallStep;
        }


        private void OnTextMouseUp(object sender, MouseButtonEventArgs e)
        {
            var upPos = e.GetPosition(this);
            if (_downPos == upPos)
            {
                _editMode = true;
                UpdateControls();
                textBox.Focus();
                textBox.SelectAll();
            }
            text.ReleaseMouseCapture();
            _isMoving = false;
        }
        private void OnTextLostFocus(object sender, RoutedEventArgs e)
        {
            _editMode = false;
            UpdateControls();
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }



        public float Value
        {
            get { return (float)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public float Min
        {
            get { return (float)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        public float Max
        {
            get { return (float)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        public float Step
        {
            get { return (float)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        public float SmallStep
        {
            get { return (float)GetValue(SmallStepProperty); }
            set { SetValue(SmallStepProperty, value); }
        }

        public Func<float, string>? Format
        {
            get { return (Func<float, string>)GetValue(FormatProperty); }
            set { SetValue(FormatProperty, value); }
        }

        public Func<string?, float>? Parse
        {
            get { return (Func<string?, float>)GetValue(ParseProperty); }
            set { SetValue(ParseProperty, value); }
        }

        public string TextValue
        {
            get { return (string)GetValue(TextValueProperty); }
            protected set { SetValue(TextValueProperty, value); }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

    }
}
