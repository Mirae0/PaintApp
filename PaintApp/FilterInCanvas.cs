using System.Windows.Controls;
using System.Windows.Input.StylusPlugIns;
using PaintApp;
namespace PaintApp;
public class FilterInkCanvas : InkCanvas
{
    private FilterPlugin filter = new FilterPlugin();

    public FilterInkCanvas()
    {
        int dynamicRenderIndex = this.StylusPlugIns.IndexOf(this.DynamicRenderer);
        if (dynamicRenderIndex >= 0)
        {
            this.StylusPlugIns.Insert(dynamicRenderIndex, filter);
        }
        else
        {
            this.StylusPlugIns.Add(filter);
        }

        this.EditingMode = InkCanvasEditingMode.Ink;
        this.Background = System.Windows.Media.Brushes.Transparent;
        this.IsHitTestVisible = false;
    }
}
