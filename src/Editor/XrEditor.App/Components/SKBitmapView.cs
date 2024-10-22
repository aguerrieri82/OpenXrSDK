using SkiaSharp;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace XrEditor
{
    public class SKBitmapView : System.Windows.Controls.Image
    {
        static readonly object _encodeLock = new object();

        static void OnSKSourceChanged(object obj, DependencyPropertyChangedEventArgs e)
        {
            ((SKBitmapView)obj).OnSKSourceChanged((SKBitmap?)e.NewValue);
        }

        protected void OnSKSourceChanged(SKBitmap? value)
        {
            if (value != null)
            {
                var stream = new MemoryStream();

                lock (_encodeLock)
                    value.Encode(stream, SKEncodedImageFormat.Png, 100);

                stream.Position = 0;

                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.EndInit();

                Source = bitmap;
            }
            else
                Source = null;
        }

        public SKBitmap? SKSource
        {
            get { return (SKBitmap?)GetValue(SKSourceProperty); }
            set { SetValue(SKSourceProperty, value); }
        }


        public static readonly DependencyProperty SKSourceProperty =
            DependencyProperty.Register("SKSource", typeof(SKBitmap), typeof(SKBitmapView), new PropertyMetadata(null, OnSKSourceChanged));


    }
}
