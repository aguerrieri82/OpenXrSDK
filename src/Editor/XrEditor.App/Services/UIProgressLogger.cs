using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class UIProgressLogger : BaseUIRetainLogger
    {
        RichTextBox? _textBox;
        ProgressBar? _progressBar;
        DateTime? _lastMessageTime;

        public UIProgressLogger()
        {
            MaxLines = 500;
        }

        public void Init(RichTextBox textBox, ProgressBar progressBar)
        {
            _progressBar = progressBar;
            _textBox = textBox;
            _textBox.Document.Blocks.Clear();
            _textBox.MouseDoubleClick += (sender, args) => _textBox.Document.Blocks.Clear();
        }

        protected override void UpdateMessages()
        {
            if (_textBox == null)
                return;

            if (_textBox.Document.Blocks.Count > MaxLines)
                _textBox.Document.Blocks.Clear();

            //_textBox.Document.Blocks.Remove(_textBox.Document.Blocks.FirstBlock);

            while (_messages.TryDequeue(out var msg))
            {
                var paragraph = new Paragraph
                {
                    Margin = new Thickness(0)
                };

                switch (msg.Level)
                {
                    case LogLevel.Info:
                        break;
                }

                paragraph.Foreground = msg.Level switch
                {
                    LogLevel.Error => Brushes.Red,
                    LogLevel.Success => Brushes.LightGreen,
                    LogLevel.Warning => Brushes.Orange,
                    LogLevel.Debug => Brushes.DarkGray,
                    _ => Brushes.WhiteSmoke,
                };

                paragraph.Inlines.Add(new Run(string.Format("{0:HH:mm:ss.fff} ", msg.Date)) { Foreground = Brushes.Gray });
                paragraph.Inlines.Add(new Run(msg.Text));

                if (_lastMessageTime != null)
                    paragraph.Inlines.Add(new Run(string.Format(" ({0}ms)", (int)(msg.Date - _lastMessageTime.Value).TotalMilliseconds)) { Foreground = Brushes.Gray });

                _textBox.Document.Blocks.Add(paragraph);
                _lastMessageTime = msg.Date;
            }


            _textBox.ScrollToEnd();
        }

        protected override void UpdateProgress(double current, double total, string? message)
        {
            if (_progressBar == null)
                return;

            _progressBar.Maximum = total;
            _progressBar.Minimum = 0;
            _progressBar.Value = current;
            _progressBar.IsIndeterminate = total == 0 && current > 0;

            if (current == 0 && total == 0)
                _progressBar.Maximum = 1;
        }


        public int MaxLines { get; set; }
    }
}
