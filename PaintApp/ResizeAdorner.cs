using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

//klasa ResizeAdorner jest używana do dodawania uchwytu do zmiany rozmiaru elementu w interfejsie użytkownika.
public class ResizeAdorner : Adorner
{
    private Thumb bottomRight;

    public ResizeAdorner(UIElement adornedElement) : base(adornedElement)
    {
        bottomRight = new Thumb
        {
            Width = 10,
            Height = 10,
            Cursor = Cursors.SizeNWSE,
            Background = Brushes.Gray
        };

        bottomRight.DragDelta += BottomRight_DragDelta;

        AddVisualChild(bottomRight);
    }

    public Thumb BottomRight => bottomRight;

    private void BottomRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (AdornedElement is FrameworkElement element)
        {
            double newWidth = element.Width + e.HorizontalChange;
            double newHeight = element.Height + e.VerticalChange;

            element.Width = Math.Max(20, newWidth);
            element.Height = Math.Max(20, newHeight);
        }
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) => bottomRight;

    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = AdornedElement.RenderSize.Width - bottomRight.Width;
        double y = AdornedElement.RenderSize.Height - bottomRight.Height;
        bottomRight.Arrange(new Rect(x, y, bottomRight.Width, bottomRight.Height));
        return finalSize;
    }
}
