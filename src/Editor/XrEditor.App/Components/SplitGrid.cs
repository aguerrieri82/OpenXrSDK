using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace XrEditor
{
    public class SplitGrid : Grid
    {
        private readonly ContentPresenter _left;
        private readonly ContentPresenter _right;
        private readonly GridSplitter _split;

        public class GridLengthConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return new GridLength((float)value, GridUnitType.Pixel);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (float)((GridLength)value).Value;
            }

            public static readonly GridLengthConverter Instance = new GridLengthConverter();
        }

        public SplitGrid()
        {
            _left = new ContentPresenter();
            _right = new ContentPresenter();
            _split = new GridSplitter();

            _left.SetBinding(ContentPresenter.ContentProperty, nameof(SplitView.First));
            _right.SetBinding(ContentPresenter.ContentProperty, nameof(SplitView.Second));

            Children.Clear();
            Children.Add(_left);
            Children.Add(_right);
            Children.Add(_split);

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateSplitters();
        }

        void UpdateSplitters()
        {
            if (DataContext is not SplitView splitter)
                return;

            RowDefinitions.Clear();
            ColumnDefinitions.Clear();

            if (splitter.Mode == SplitViewMode.Vertical)
            {
                ColumnDefinition[] columns =
                [
                    new ColumnDefinition
                    {
                        Width =  new GridLength(1, GridUnitType.Star)
                    },
                    new ColumnDefinition()
                ];

                if (splitter.SizeMode == SplitViewSizeMode.First)
                {
                    ColumnDefinitions.Add(columns[1]);
                    ColumnDefinitions.Add(columns[0]);
                }
                else
                {
                    ColumnDefinitions.Add(columns[0]);
                    ColumnDefinitions.Add(columns[1]);
                }

                columns[1].SetBinding(ColumnDefinition.WidthProperty, new Binding
                {
                    Path = new PropertyPath(nameof(splitter.Size)),
                    Mode = BindingMode.TwoWay,
                    Converter = GridLengthConverter.Instance
                });

                _left.SetValue(ColumnProperty, 0);
                _left.Margin = new Thickness(0, 0, 5, 0);

                _right.SetValue(ColumnProperty, 1);

                _split.Width = 5;
                _split.HorizontalAlignment = HorizontalAlignment.Right;
                _split.VerticalAlignment = VerticalAlignment.Stretch;
                _split.SetValue(ColumnProperty, 0);
            }
            else
            {
                RowDefinition[] rows =
                [
                    new RowDefinition
                    {
                        Height = new GridLength(1, GridUnitType.Star)
                    },
                    new RowDefinition()
                ];

                if (splitter.SizeMode == SplitViewSizeMode.First)
                {
                    RowDefinitions.Add(rows[1]);
                    RowDefinitions.Add(rows[0]);
                }
                else
                {
                    RowDefinitions.Add(rows[0]);
                    RowDefinitions.Add(rows[1]);
                }

                rows[1].SetBinding(RowDefinition.HeightProperty, new Binding
                {
                    Path = new PropertyPath(nameof(splitter.Size)),
                    Mode = BindingMode.TwoWay,
                    Converter = GridLengthConverter.Instance
                });

                _left.SetValue(RowProperty, 0);
                _left.Margin = new Thickness(0, 0, 0, 5);

                _right.SetValue(RowProperty, 1);

                _split.Height = 5;
                _split.HorizontalAlignment = HorizontalAlignment.Stretch;
                _split.VerticalAlignment = VerticalAlignment.Bottom;
                _split.SetValue(RowProperty, 0);
            }
        }
    }
}
