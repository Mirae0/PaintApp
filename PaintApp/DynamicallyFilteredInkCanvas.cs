using System.Windows.Controls;
namespace PaintApp;
public class DynamicallyFilteredInkCanvas : InkCanvas
{
    FilterPlugin filter = new FilterPlugin();

    public DynamicallyFilteredInkCanvas()
        : base()
    {
        int dynamicRenderIndex =
            this.StylusPlugIns.IndexOf(this.DynamicRenderer);

        this.StylusPlugIns.Insert(dynamicRenderIndex, filter);
    }
}