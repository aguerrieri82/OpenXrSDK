using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XrEditor
{
    public sealed class ImagePointerMoveBehavior : Behavior<Image>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ImagePointerMoveBehavior));


        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseMove += OnMouseMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseMove -= OnMouseMove;
            base.OnDetaching();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {

            var p = e.GetPosition(AssociatedObject);

            if (AssociatedObject.ActualWidth <= 0 || AssociatedObject.ActualHeight <= 0)
                return;

            var x = (float)(p.X / AssociatedObject.ActualWidth);
            var y = (float)(p.Y / AssociatedObject.ActualHeight);

            if (AssociatedObject.RenderTransform is System.Windows.Media.ScaleTransform st && st.ScaleY < 0)
                y = 1f - y;

            if (AssociatedObject.DataContext is IPointerEvents pEvents)
                pEvents.OnMouseMove(x, y);

        }
    }
}
