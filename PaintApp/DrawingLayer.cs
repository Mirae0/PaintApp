using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace PaintApp
{
    internal class DrawingLayer
    {

        public WriteableBitmap Bitmap { get; set; }
        public Image ImageControl { get; set; }  // do wyświetlenia w UI
        public string Name { get; set; }
        public bool IsVisible { get; set; } = true;
        public double Opacity { get; set; }

        public DrawingLayer(int width, int height)
        {
            Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            ImageControl = new Image
            {
                Source = Bitmap,
                Width = width,
                Height = height
            };
        }

        public DrawingLayer(DrawingLayer layer)
        {
            Bitmap = layer.Bitmap.Clone();

            ImageControl = new Image
            {
                Source = layer.ImageControl.Source.Clone(),
                Width = layer.ImageControl.Width,
                Height = layer.ImageControl.Height
            };
            Name = layer.Name;
            IsVisible = layer.IsVisible;
        }
    }
}
