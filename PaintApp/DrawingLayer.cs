using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;

namespace PaintApp
{
    internal class DrawingLayer
    {

        public WriteableBitmap Bitmap { get; set; }
        public Image ImageControl { get; set; }  // do wyświetlenia w UI
        public string Name { get; set; }
        public bool IsVisible { get; set; } = true;

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
    }
}
