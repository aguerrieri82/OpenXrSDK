
using System.Windows;
using System.Windows.Controls;
using XrEngine;


namespace XrEditor.Components
{
    public partial class LogPanelControl : UserControl
    {
        readonly UIProgressLogger _logger;


        public LogPanelControl()
        {
            InitializeComponent();
            _logger = new UIProgressLogger();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _logger.Init(textBox, progressBar);
            Context.Implement<IProgressLogger>(_logger);
            Loaded -= OnLoaded;
        }
    }
}
