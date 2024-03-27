using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace XrEditor.Components
{
    /// <summary>
    /// Interaction logic for FloatEditor.xaml
    /// </summary>
    public partial class FloatEditor : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(float), typeof(FloatEditor), new PropertyMetadata(0f));

        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register("Min", typeof(float), typeof(FloatEditor), new PropertyMetadata(0f));
        
        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register("Max", typeof(float), typeof(FloatEditor), new PropertyMetadata(1f));

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register("Step", typeof(float), typeof(FloatEditor), new PropertyMetadata(0f));


        bool _editMode;
        private Point _downPos;

        public FloatEditor()
        {
            InitializeComponent();
            UpdateControls();   

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


        private void OnTextLostFocus(object sender, RoutedEventArgs e)
        {
            _editMode = false;
            UpdateControls();
        }
    }
}
