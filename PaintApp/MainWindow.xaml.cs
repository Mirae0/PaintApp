using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PaintApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LayerList.ItemsSource = Layers;
            AddLayer_Click(null, null);
        }
        ObservableCollection<DrawingLayer> Layers = new ObservableCollection<DrawingLayer>();
        DrawingLayer? ActiveLayer = null;

        private Point? lastPoint = null;
        private bool isDrawing = false;

        const int canvasWidth = 800;
        const int canvasHeight = 600;
        private enum ToolType { Pen, Eraser }
        private ToolType currentTool = ToolType.Pen;
        private Color currentColor = Colors.Black;
        private int brushSize = 1;
        private enum ShapeType { None, Line, Rectangle, Ellipse }
        private ShapeType currentShape = ShapeType.None;
        private enum SelectMode { None, Rectangle, Free }
        private SelectMode currentSelectMode = SelectMode.None;
        private Shape? previewShape = null;
        private Point shapeStart;
        private Rect? selectionRect = null;
        private WriteableBitmap? selectionData = null;



        private void Tool_Line_Click(object sender, RoutedEventArgs e) => currentShape = ShapeType.Line;
        private void Tool_Rectangle_Click(object sender, RoutedEventArgs e) => currentShape = ShapeType.Rectangle;
        private void Tool_Ellipse_Click(object sender, RoutedEventArgs e) => currentShape = ShapeType.Ellipse;




        private void SetToolToPen(object sender, RoutedEventArgs e)
        {
            currentTool = ToolType.Pen;
            currentShape = ShapeType.None;
        }

        private void SetToolToEraser(object sender, RoutedEventArgs e)
        {
            currentTool = ToolType.Eraser;
            currentShape = ShapeType.None;
        }


        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = (ContextMenu)Resources["LayerMenu"];
            var layer = new DrawingLayer(canvasWidth, canvasHeight)
            {
                Name = $"Warstwa {Layers.Count + 1}"
            };
            Layers.Insert(0, layer);
            DrawingCanvas.Children.Insert(0, layer.ImageControl);
            LayerList.SelectedItem = layer;
        }


        /// Zmiana aktywnej warstwy

        private void LayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LayerList.SelectedItem is DrawingLayer selected)
            {
                ActiveLayer = selected;
                OpacitySlider.Value = selected.ImageControl.Opacity;
            }
                
        }

        
        private void MoveLayerUp_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveLayer == null) return;
            int index = Layers.IndexOf(ActiveLayer);
            if (index > 0)
            {
                Layers.Move(index, index - 1);
                RedrawCanvas();
            }
        }

        private void MoveLayerDown_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveLayer == null) return;
            int index = Layers.IndexOf(ActiveLayer);
            if (index < Layers.Count - 1)
            {
                Layers.Move(index, index + 1);
                RedrawCanvas();
            }
        }

       // Otwieranie menu kontekstowego dla warstwy
        private void LayerList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as ListBox)?.SelectedItem as DrawingLayer;
            if (item != null)
            {
                ContextMenu cm = this.FindResource("LayerMenu") as ContextMenu;
                cm.PlacementTarget = sender as Button;
                cm.IsOpen = true;
                ActiveLayer = item;
               
            }   
        }
        // Usuwanie warstwy
        private void RemoveLayer_Click(object sender, RoutedEventArgs e)
        {
            var item = ActiveLayer as DrawingLayer;
            if (MessageBox.Show($"Czy na pewno chcesz usunąć warstwę '{item.Name}'?", "Usuń warstwę",
                                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    DrawingCanvas.Children.Remove(item.ImageControl);
                    Layers.Remove(item);
                    ActiveLayer = null;
                }

        }

        //Duplikowanie warstwy
        private void DuplicateLayer_Click(Object sender, RoutedEventArgs e)
        {
            var item = ActiveLayer as DrawingLayer;
            if (item != null)
            {
                var itemCopy = new DrawingLayer(item);
                itemCopy.Name += "-Copy";
                Layers.Insert(0, itemCopy);
                DrawingCanvas.Children.Insert(0, itemCopy.ImageControl);
                LayerList.SelectedItem = itemCopy;
            }

        }

        //Ukryj / Pokaż warstwę
        private void ShowHideLayer_Click(Object sender, RoutedEventArgs e)
        {
            var item = ActiveLayer as DrawingLayer;
            if(item!=null){ item.IsVisible = !item.IsVisible; }
            RedrawCanvas();
        }


        private void RedrawCanvas()
        {
            DrawingCanvas.Children.Clear();
            foreach (var layer in Layers)
            {
                if (layer.IsVisible)
                {
                    DrawingCanvas.Children.Add(layer.ImageControl);
                }
               
            }
            }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ActiveLayer == null) return;

            shapeStart = e.GetPosition(DrawingCanvas);

            if (currentShape != ShapeType.None)
            {
                previewShape = CreatePreviewShape(currentShape);
                if (previewShape != null)
                {
                    Canvas.SetLeft(previewShape, shapeStart.X);
                    Canvas.SetTop(previewShape, shapeStart.Y);
                    DrawingCanvas.Children.Add(previewShape);
                }
            }
            else
            {
                isDrawing = true;
                lastPoint = shapeStart;
                DrawPoint(lastPoint.Value);
            }
        }


        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (ActiveLayer == null) return;
            Point current = e.GetPosition(DrawingCanvas);
            if (ActiveLayer.IsVisible)
            {
                if (currentShape != ShapeType.None && previewShape != null)
                {
                    UpdatePreviewShape(previewShape, shapeStart, current);
                }
                else if (isDrawing && lastPoint.HasValue)
                {
                    DrawLine(lastPoint.Value, current);
                    lastPoint = current;
                }
            }
        }


        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ActiveLayer == null) return;

            isDrawing = false;
            lastPoint = null;

            if (previewShape != null)
            {
                Point end = e.GetPosition(DrawingCanvas);
                DrawFinalShape(currentShape, shapeStart, end);

                DrawingCanvas.Children.Remove(previewShape);
                previewShape = null;
            }
        }
        private Shape CreatePreviewShape(ShapeType type)
        {
            Shape shape = type switch
            {
                ShapeType.Line => new Line { Stroke = new SolidColorBrush(currentColor), StrokeThickness = brushSize },
                ShapeType.Rectangle => new Rectangle { Stroke = new SolidColorBrush(currentColor), StrokeThickness = brushSize },
                ShapeType.Ellipse => new Ellipse { Stroke = new SolidColorBrush(currentColor), StrokeThickness = brushSize },
                _ => null
            };
            shape.StrokeDashArray = new DoubleCollection { 2, 2 }; // podgląd
            return shape;
        }


        private void UpdatePreviewShape(Shape shape, Point start, Point end)
        {
            if (shape is Line line)
            {
                line.X1 = start.X;
                line.Y1 = start.Y;
                line.X2 = end.X;
                line.Y2 = end.Y;
            }
            else
            {
                double x = Math.Min(start.X, end.X);
                double y = Math.Min(start.Y, end.Y);
                double w = Math.Abs(end.X - start.X);
                double h = Math.Abs(end.Y - start.Y);
                Canvas.SetLeft(shape, x);
                Canvas.SetTop(shape, y);
                shape.Width = w;
                shape.Height = h;
            }
        }



        private void DrawPoint(Point point)
        {
            var wb = ActiveLayer.Bitmap;
            int centerX = (int)point.X;
            int centerY = (int)point.Y;

            if (centerX < 0 || centerY < 0 || centerX >= wb.PixelWidth || centerY >= wb.PixelHeight)
                return;

            byte[] colorData;

            if (currentTool == ToolType.Pen)
            {
                colorData = new byte[]
                {
            currentColor.B,
            currentColor.G,
            currentColor.R,
            currentColor.A
                };
            }
            else // ToolType.Eraser
            {
                colorData = new byte[] { 0, 0, 0, 0 }; // przezroczystość
            }

            wb.Lock();

            int halfSize = brushSize / 2;

            for (int dx = -halfSize; dx <= halfSize; dx++)
            {
                for (int dy = -halfSize; dy <= halfSize; dy++)
                {
                    int px = centerX + dx;
                    int py = centerY + dy;

                    if (px >= 0 && py >= 0 && px < wb.PixelWidth && py < wb.PixelHeight)
                    {
                        Int32Rect rect = new Int32Rect(px, py, 1, 1);
                        wb.WritePixels(rect, colorData, 4, 0);
                    }
                }
            }

            wb.Unlock();
        }



        private void DrawLine(Point from, Point to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            int steps = (int)Math.Max(Math.Abs(dx), Math.Abs(dy));
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double x = from.X + dx * t;
                double y = from.Y + dy * t;
                DrawPoint(new Point(x, y));
            }
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
                currentColor = e.NewValue.Value;
        }
        private void BrushSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            brushSize = (int)e.NewValue;
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(ActiveLayer != null)
            {
                ActiveLayer.ImageControl.Opacity = (double)e.NewValue;
                RedrawCanvas();
            }
            
        }

        private void ZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scale = e.Delta > 0 ? 1.1 : 0.9;
            CanvasScale.ScaleX *= scale;
            CanvasScale.ScaleY *= scale;
        }
        private void Tool_SelectRect_Click(object sender, RoutedEventArgs e)
        {
            currentSelectMode = SelectMode.Rectangle;
            // inicjalizacja zaznaczenia
        }

        private void Tool_SelectFree_Click(object sender, RoutedEventArgs e)
        {
            currentSelectMode = SelectMode.Free;
            // inicjalizacja dowolnego zaznaczenia
        }
        private void SaveImage(string filePath)
        {
            if (ActiveLayer == null) return;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(ActiveLayer.Bitmap));

            using FileStream fs = new FileStream(filePath, FileMode.Create);
            encoder.Save(fs);
        }

        private void SaveAsPng_Click(object sender, RoutedEventArgs e)
        {
            SaveImageToFile("PNG Files|*.png", new PngBitmapEncoder(), "png");
        }

        private void SaveAsJpg_Click(object sender, RoutedEventArgs e)
        {
            SaveImageToFile("JPEG Files|*.jpg;*.jpeg", new JpegBitmapEncoder() { QualityLevel = 90 }, "jpg");
        }

        private void SaveImageToFile(string filter, BitmapEncoder encoder, string ext)
        {
            if (ActiveLayer == null)
            {
                MessageBox.Show("Brak aktywnej warstwy do zapisu.");
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = $".{ext}",
                FileName = $"obraz.{ext}"
            };

            if (dlg.ShowDialog() == true)
            {
                encoder.Frames.Clear();
                encoder.Frames.Add(BitmapFrame.Create(ActiveLayer.Bitmap));
                using (var stream = new FileStream(dlg.FileName, FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }
        }
        private void DrawFinalShape(ShapeType shapeType, Point start, Point end)
        {
            if (ActiveLayer == null) return;

            var wb = ActiveLayer.Bitmap;

            int x1 = (int)Math.Clamp(Math.Min(start.X, end.X), 0, wb.PixelWidth - 1);
            int y1 = (int)Math.Clamp(Math.Min(start.Y, end.Y), 0, wb.PixelHeight - 1);
            int x2 = (int)Math.Clamp(Math.Max(start.X, end.X), 0, wb.PixelWidth - 1);
            int y2 = (int)Math.Clamp(Math.Max(start.Y, end.Y), 0, wb.PixelHeight - 1);

            wb.Lock();

            switch (shapeType)
            {
                case ShapeType.Line:
                    DrawLine(start, end);
                    break;

                case ShapeType.Rectangle:
                    DrawRectangle(wb, x1, y1, x2, y2);
                    break;

                case ShapeType.Ellipse:
                    DrawEllipse(wb, x1, y1, x2, y2);
                    break;

                default:
                    break;
            }

            wb.Unlock();
        }

        private void DrawRectangle(WriteableBitmap wb, int x1, int y1, int x2, int y2)
        {
            for (int x = x1; x <= x2; x++)
            {
                DrawPixel(wb, x, y1);
                DrawPixel(wb, x, y2);
            }
            for (int y = y1; y <= y2; y++)
            {
                DrawPixel(wb, x1, y);
                DrawPixel(wb, x2, y);
            }
        }

        private void DrawEllipse(WriteableBitmap wb, int x1, int y1, int x2, int y2)
        {
            int centerX = (x1 + x2) / 2;
            int centerY = (y1 + y2) / 2;
            int radiusX = (x2 - x1) / 2;
            int radiusY = (y2 - y1) / 2;

            for (double angle = 0; angle < 360; angle += 0.5)
            {
                double rad = angle * Math.PI / 180;
                int x = (int)(centerX + radiusX * Math.Cos(rad));
                int y = (int)(centerY + radiusY * Math.Sin(rad));
                DrawPixel(wb, x, y);
            }
        }
        private void DrawPixel(WriteableBitmap wb, int x, int y)
        {
            if (x < 0 || y < 0 || x >= wb.PixelWidth || y >= wb.PixelHeight)
                return;

            byte[] colorData;

            if (currentTool == ToolType.Pen)
            {
                colorData = new byte[]
                {
                    currentColor.B,
                    currentColor.G,
                    currentColor.R,
                    currentColor.A
                };
            }
            else // Eraser
            {
                colorData = new byte[] { 0, 0, 0, 0 };
            }

            var rect = new Int32Rect(x, y, 1, 1);
            wb.WritePixels(rect, colorData, 4, 0);
        }


        //Usuwanie wszystkich warstw, tworzenie jednej nowej
        private void NewFile_Click(object sender, EventArgs e)
        {
            foreach(var layer in Layers.ToList())
            {
                DrawingCanvas.Children.Remove(layer.ImageControl);
                Layers.Remove(layer);
            }
            AddLayer_Click(null, null);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveAsPng_Click(sender, e); 
        }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Otwórz obraz"
            };

            if (dlg.ShowDialog() == true)
            {
                var bitmap = new BitmapImage(new Uri(dlg.FileName));
                var layer = new DrawingLayer(bitmap.PixelWidth, bitmap.PixelHeight)
                {
                    Name = $"Warstwa {Layers.Count + 1}"
                };

                layer.Bitmap = new WriteableBitmap(bitmap);
                Layers.Insert(0, layer);
                DrawingCanvas.Children.Insert(0, layer.ImageControl);
                LayerList.SelectedItem = layer;
            }
        }
        private void DrawingCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomFactor = 0.1;
            var scale = CanvasScale.ScaleX;

            if (e.Delta > 0)
                scale += zoomFactor;
            else
                scale -= zoomFactor;

            scale = Math.Max(0.2, Math.Min(5.0, scale)); // ograniczenia zoomu

            CanvasScale.ScaleX = scale;
            CanvasScale.ScaleY = scale;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

       
    }
}
