// A StylusPlugin that restricts the input area.
using System.Windows.Input.StylusPlugIns;
using System.Windows.Input;
using System;
using System.Windows.Media;
using System.Windows;

using System.Windows.Ink;
namespace PaintApp;

public class FilterPlugin : StylusPlugIn
{
    protected override void OnStylusDown(RawStylusInput rawStylusInput)
    {
        // Call the base class before modifying the data.
        base.OnStylusDown(rawStylusInput);

        // Restrict the stylus input.
        Filter(rawStylusInput);
    }

    protected override void OnStylusMove(RawStylusInput rawStylusInput)
    {
        // Call the base class before modifying the data.
        base.OnStylusMove(rawStylusInput);

        // Restrict the stylus input.
        Filter(rawStylusInput);
    }

    protected override void OnStylusUp(RawStylusInput rawStylusInput)
    {
        // Call the base class before modifying the data.
        base.OnStylusUp(rawStylusInput);

        // Restrict the stylus input
        Filter(rawStylusInput);
    }

    private void Filter(RawStylusInput rawStylusInput)
    {
        // Get the StylusPoints that have come in.
        StylusPointCollection stylusPoints = rawStylusInput.GetStylusPoints();

        // Modify the (X,Y) data to move the points
        // inside the acceptable input area, if necessary.
        for (int i = 0; i < stylusPoints.Count; i++)
        {
            StylusPoint sp = stylusPoints[i];
            if (sp.X < 50) sp.X = 50;
            if (sp.X > 250) sp.X = 250;
            if (sp.Y < 50) sp.Y = 50;
            if (sp.Y > 250) sp.Y = 250;
            stylusPoints[i] = sp;
        }

        // Copy the modified StylusPoints back to the RawStylusInput.
        rawStylusInput.SetStylusPoints(stylusPoints);
    }
}