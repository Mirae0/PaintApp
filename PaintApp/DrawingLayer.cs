using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace PaintApp
{
    public class DrawingLayer
    {

        public WriteableBitmap Bitmap { get; set; }
        public Image ImageControl { get; set; }  // do wyświetlenia w UI
        public string Name { get; set; }
        public bool IsVisible { get; set; } = true;
        public double Opacity { get; set; }
        public UIElement Container { get; set; }



        private Stack<WriteableBitmap> undoStack = new Stack<WriteableBitmap>();
        private Stack<WriteableBitmap> redoStack = new Stack<WriteableBitmap>();
        public bool CanUndo() => undoStack.Count > 0;
        public bool CanRedo() => redoStack.Count > 0;


        public  DrawingLayer(int width, int height)
        {
            Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            ImageControl = new Image
            {
                Source = Bitmap,
                Width = width,
                Height = height
            };

            SaveState();
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

        //skroty klawiszowe, cofanie ponowne
        // undo, redo
        // undoStack - stos do przechowywania stanu przed zmianami
        // redoStack - stos do przechowywania stanu po cofnięciu zmian
        public void SaveState()
        {
            if (Bitmap != null)
            {
                var clone = new WriteableBitmap(Bitmap);
                undoStack.Push(clone);
                redoStack.Clear();
            }
        }
        public void BeginDraw()
        {
            undoStack.Push(new WriteableBitmap(Bitmap));
            redoStack.Clear();
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(new WriteableBitmap(Bitmap));
                Bitmap = undoStack.Pop();
                ImageControl.Source = Bitmap;
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(new WriteableBitmap(Bitmap));
                Bitmap = redoStack.Pop();
                ImageControl.Source = Bitmap;
            }
        }
        public void FillLayer(Color fillColor)
        {
            if (Bitmap == null) return;

            SaveState(); // Zapisz stan do cofania

            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;
            int stride = Bitmap.BackBufferStride;

            byte[] colorData = new byte[4] {
        fillColor.B,
        fillColor.G,
        fillColor.R,
        fillColor.A
    };

            byte[] pixels = new byte[height * stride];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    Array.Copy(colorData, 0, pixels, index, 4);
                }
            }

            Bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }


    }

}
