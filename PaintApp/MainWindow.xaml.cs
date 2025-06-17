using System;
using System.Collections.ObjectModel;
using System.IO;
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
using System.Windows.Input.StylusPlugIns;
using Xceed.Wpf.Themes.FluentDesign.Common;
using System.DirectoryServices;
using System.Diagnostics;


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
            DrawingCanvas.StylusDown += DrawingCanvas_StylusDown;
            DrawingCanvas.StylusMove += DrawingCanvas_StylusMove;
            DrawingCanvas.StylusUp += DrawingCanvas_StylusUp;
            UndoCommand = new RelayCommand(_ => Undo_Click(null, null), _ => CanUndo());
            RedoCommand = new RelayCommand(_ => Redo_Click(null, null), _ => CanRedo());

            this.DataContext = this;
            this.InputBindings.Add(new KeyBinding(RedoCommand, Key.Y, ModifierKeys.Control));



        }
        public MainWindow(int canvasWidth, int canvasHeight, string theme) : this()
        {
            DrawingCanvas.Width = canvasWidth;
            DrawingCanvas.Height = canvasHeight;
            SetTheme(theme);
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, SaveCommand_Executed));
            this.InputBindings.Add(new KeyBinding(ApplicationCommands.Save, Key.S, ModifierKeys.Control));



        }


        ObservableCollection<DrawingLayer> Layers = new ObservableCollection<DrawingLayer>();
        DrawingLayer? ActiveLayer = null;

        private Point? lastPoint = null;
        private bool isDrawing = false;

        const int canvasWidth = 800;
        const int canvasHeight = 600;
        private enum ToolType { Pen, Eraser, Select, Bucket }
        private ToolType currentTool = ToolType.Pen;
        private Color currentColor = Colors.Black;
        private int brushSize = 1;
        private enum ShapeType { None, Line, Rectangle, Ellipse }
        private ShapeType currentShape = ShapeType.None;
        private enum SelectMode { None, Rectangle, Free }
        private SelectMode currentSelectMode = SelectMode.None;
        private bool invertedSelect;
        private Shape? previewShape = null;
        private Shape? selectedShape = null;
        private Point shapeStart;
        private Point? selectedStart=null;
        private Point? selectedEnd=null;
        private Rect selectionRect = new Rect(new Point(0,0),new Point(canvasWidth,canvasHeight));
        private WriteableBitmap? selectionData = null;
        public ICommand UndoCommand { get; set; }
        public ICommand RedoCommand { get; set; }
        private enum InputMode { None, Mouse, Stylus }
        private InputMode currentMode = InputMode.None;
        private bool isSaved = false;


        private bool CanUndo() => ActiveLayer != null && ActiveLayer.CanUndo();
        private bool CanRedo() => ActiveLayer != null && ActiveLayer.CanRedo();
        private Stack<LayerAction> undoLayerStack = new Stack<LayerAction>();
        private Stack<LayerAction> redoLayerStack = new Stack<LayerAction>();
        private bool isFillModeEnabled = false;




        private void Tool_Line_Click(object sender, RoutedEventArgs e)
        {
            currentShape = ShapeType.Line;
            Cursor = Cursors.Pen; 
        }
        private void Tool_Rectangle_Click(object sender, RoutedEventArgs e)
        {
            currentShape = ShapeType.Rectangle;
            Cursor = Cursors.Pen;

        } 
        private void Tool_Ellipse_Click(object sender, RoutedEventArgs e) 
        {
            currentShape = ShapeType.Ellipse;
            Cursor = Cursors.Pen;
        }

        private void SetToolToPen(object sender, RoutedEventArgs e)
        {
            currentTool = ToolType.Pen;
            currentShape = ShapeType.None;
            Cursor = Cursors.Pen; 
        }

        private void SetToolToEraser(object sender, RoutedEventArgs e)
        {
            currentTool = ToolType.Eraser;
            currentShape = ShapeType.None;
            Cursor = Cursors.Cross; 
        }
        //ciemny jasny motyw
        private void SetTheme(string theme)
        {
            Brush bg, fg;

            if (theme == "Dark")
            {
                bg = (Brush)new BrushConverter().ConvertFrom("#222222");
                fg = Brushes.White;

                Resources["WindowBackgroundBrush"] = bg;
                Resources["BackgroundBrush"] = bg;
                Resources["WindowForegroundBrush"] = fg;
                Resources["ButtonBackgroundBrush"] = bg;
                Resources["ButtonForegroundBrush"] = fg;
                Resources["LabelForegroundBrush"] = fg;
                Resources["MenuBackgroundBrush"] = bg;
                Resources["MenuForegroundBrush"] = fg;
                Resources["ColorPickerBackgroundBrush"] = bg;
                Resources["ColorPickerForegroundBrush"] = fg;
                Resources["ContextMenuBackground"] = bg;
                Resources["ContextMenuForeground"] = fg;
          
            }
            else
            {
                bg = Brushes.White;
                fg = Brushes.Black;

                Resources["WindowBackgroundBrush"] = bg;
                Resources["BackgroundBrush"] = bg;
                Resources["WindowForegroundBrush"] = fg;
                Resources["ButtonBackgroundBrush"] = bg;
                Resources["ButtonForegroundBrush"] = fg;
                Resources["LabelForegroundBrush"] = fg;
                Resources["MenuBackgroundBrush"] = bg;
                Resources["MenuForegroundBrush"] = fg;
                Resources["ColorPickerBackgroundBrush"] = bg;
                Resources["ColorPickerForegroundBrush"] = fg;
                Resources["ContextMenuBackground"] = bg;
                Resources["ContextMenuForeground"] = fg;
             
            }

            this.Background = bg;
            ApplyThemeRecursively(this, bg, fg);
        }

        private void ApplyThemeRecursively(DependencyObject parent, Brush bg, Brush fg)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                switch (child)
                {
                    case TextBlock tb:
                        tb.Foreground = fg;
                        break;
                    case Label lbl:
                        lbl.Foreground = fg;
                        lbl.Background = bg;
                        break;
                    case TextBox tbx:
                        tbx.Foreground = fg;
                        tbx.Background = Brushes.White;
                        break;
                    case RadioButton rb:
                        rb.Foreground = fg;
                        rb.Background = bg;
                        break;
                    case Slider slider:
                        slider.Foreground = fg;
                        slider.Background = Brushes.Transparent;
                        break;
                    case ListBoxItem lbi:
                        lbi.Foreground = fg;
                        lbi.Background = bg;
                        break;
                    case ScrollViewer sv:
                        sv.Background = bg;
                        break;
                    case Panel panel:
                        panel.Background = bg;
                        break;
                    case Control ctrl:
                        ctrl.Foreground = fg;
                        ctrl.Background = bg;
                        break;
                }

                ApplyThemeRecursively(child, bg, fg); // rekurencja
            }
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
            //aktualizacja o cofanie i ponow
            ActiveLayer = layer;
            undoLayerStack.Push(new LayerAction
            {
                Type = LayerAction.ActionType.Add,
                Layer = layer,
                Index = 0
            });
            redoLayerStack.Clear();
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
                undoLayerStack.Push(new LayerAction { Type = LayerAction.ActionType.MoveUp, Layer = ActiveLayer, Index = index });
                redoLayerStack.Clear();
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
                undoLayerStack.Push(new LayerAction { Type = LayerAction.ActionType.MoveDown, Layer = ActiveLayer, Index = index });
                redoLayerStack.Clear();
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
                int index = Layers.IndexOf(item);
                Layers.Remove(item);
                DrawingCanvas.Children.Remove(item.ImageControl);
                undoLayerStack.Push(new LayerAction { Type = LayerAction.ActionType.Remove, Layer = item, Index = index });
                redoLayerStack.Clear();

                ActiveLayer = Layers.FirstOrDefault();
                LayerList.SelectedItem = ActiveLayer;
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
                ActiveLayer = itemCopy;

                undoLayerStack.Push(new LayerAction { Type = LayerAction.ActionType.Duplicate, Layer = itemCopy, Index = 0 });
                redoLayerStack.Clear();
            }

        }

        //Ukryj / Pokaż warstwę
        private void ShowHideLayer_Click(Object sender, RoutedEventArgs e)
        {
            var item = ActiveLayer as DrawingLayer;
            if (item != null) { item.IsVisible = !item.IsVisible; }
            RedrawCanvas();
        }


        #region DrawingShapes


        private void DrawPoint(Point point,int size)
        {
            var wb = ActiveLayer.Bitmap;
            int centerX = (int)point.X;
            int centerY = (int)point.Y;

            if (centerX < 0 || centerY < 0 || centerX >= wb.PixelWidth || centerY >= wb.PixelHeight)
                return;

            byte[] colorData;

            if (currentTool == ToolType.Pen || currentTool == ToolType.Bucket)
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

            int halfSize = size / 2;

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
                DrawPoint(new Point(x, y),brushSize);
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



        private void DrawRectangle(WriteableBitmap wb, int x1, int y1, int x2, int y2)
        {
            for (int x = x1; x <= x2; x++)
            {
             
                DrawPoint(new Point(x, y1), brushSize);
                DrawPoint(new Point(x, y2), brushSize);
            }
            for (int y = y1; y <= y2; y++)
            {
            
                DrawPoint(new Point(x1, y), brushSize);
                DrawPoint(new Point(x2, y), brushSize);
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
                DrawPoint(new Point(x,y), brushSize);
            }
        }
    

        private void Tool_SelectRect_Click(object sender, RoutedEventArgs e)
        {
            currentSelectMode = SelectMode.Rectangle;
            currentTool = ToolType.Select;
            currentShape = ShapeType.Rectangle;
            Cursor = Cursors.Cross;
            // inicjalizacja zaznaczenia

        }

        private void Tool_SelectFree_Click(object sender, RoutedEventArgs e)
        {
            currentSelectMode = SelectMode.Free;
            Cursor = Cursors.Cross;
            // inicjalizacja dowolnego zaznaczenia
        }

        private void Tool_SelectBucket_Click(object sender, RoutedEventArgs e)
        {
            currentShape = ShapeType.None;
            currentTool = ToolType.Bucket;
            Cursor = Cursors.Cross;
        }

        private void DrawFinalSelectShape(Shape shape,Point start, Point end)
        {
            if(selectedShape != null)
            {
                DrawingCanvas.Children.Remove(selectedShape);
            }
            selectedShape = shape;
            shape.StrokeDashArray = new DoubleCollection { 2, 2 };
            DrawFinalShape(currentShape,start,end);
            if(currentSelectMode == SelectMode.Rectangle)
            {
                selectionRect = new Rect(start,end);
            }
            selectedEnd= end;
            selectedStart= start;
            //Tu sa punkty start i koniec. Testować czy współrzędne między nimi żeby można było rysować (Przynajmniej dla rect)

        }

        private void FillShape(Point start)
        {
            System.Drawing.Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource) ActiveLayer.Bitmap));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }


            Stack<Point> points = new Stack<Point>();
            System.Drawing.Color colTemp =bmp.GetPixel((int)start.X, (int)start.Y);
            Color targetColor = Color.FromArgb(colTemp.A,colTemp.R, colTemp.G, colTemp.B);
            System.Drawing.Color colNew = System.Drawing.Color.FromArgb(currentColor.A,currentColor.R,currentColor.G,currentColor.B);

            if (!colTemp.Equals(colNew))
            {
                points.Push(start);

                while (points.Count > 0)
                {
                    Point temp = points.Pop();
                    if (temp.X < bmp.Width && temp.X > 0 && temp.Y < bmp.Height && temp.Y > 0)
                    {
                        if (bmp.GetPixel((int)temp.X, (int)temp.Y) == colTemp)
                        {
                            DrawPoint(temp,10);
                            //bmp.SetPixel((int)temp.X, (int)temp.Y, colNew);
                            Point newP = new Point(temp.X+1, temp.Y);
                            if (colTemp.Equals(bmp.GetPixel((int)newP.X, (int)newP.Y)) && newP.X < bmp.Width && newP.X > 0 && newP.Y < bmp.Height && newP.Y > 0)
                            {
                                points.Push(newP);
                            }
                            newP = new Point((int)temp.X - 1, (int)temp.Y);
                            if (colTemp.Equals(bmp.GetPixel((int)newP.X, (int)newP.Y)) && newP.X < bmp.Width && newP.X > 0 && newP.Y < bmp.Height && newP.Y > 0)
                            {
                                points.Push(newP);
                            }
                             newP = new Point((int)temp.X, (int)temp.Y + 1);
                            if (colTemp.Equals(bmp.GetPixel((int)newP.X, (int)newP.Y)) && newP.X < bmp.Width && newP.X > 0 && newP.Y < bmp.Height && newP.Y > 0)
                            {
                                points.Push(newP);
                            }
                             newP = new Point((int)temp.X, (int)temp.Y - 1);
                            if (colTemp.Equals(bmp.GetPixel((int)newP.X, (int)newP.Y)) && newP.X < bmp.Width && newP.X > 0 && newP.Y < bmp.Height && newP.Y > 0)
                            {
                                points.Push(newP);
                            }

                            Trace.WriteLine(points.Pop());
                        }
                    }
                }
            }

          
           BitmapSource bitmapSource =  System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
           WriteableBitmap wbtm = new WriteableBitmap(bitmapSource);

            ActiveLayer.Bitmap = wbtm;
        }

        #endregion


        private void remove_Select(object sender, RoutedEventArgs e)
        {
            selectionRect = new Rect(new Point(0, 0), new Point(canvasWidth, canvasHeight));
            if (selectedShape != null)
            {
                DrawingCanvas.Children.Remove(selectedShape);
            }
            invertedSelect = false;
        }

        private void invert_Select(object sender, RoutedEventArgs e)
        {
                invertedSelect = !invertedSelect; 
            
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
            if (e.StylusDevice != null && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus)
            {
                StatusLabel.Content = "Tryb: Rysik";
                currentMode = InputMode.Stylus;
            }
            else
            {
                StatusLabel.Content = "Tryb: Mysz";
                currentMode = InputMode.Mouse;
            }


            if (isFillModeEnabled)
            {
                FillEntireLayerWithColor();
                isFillModeEnabled = false;
                Cursor = Cursors.Arrow;
                return;
            }


            ActiveLayer.BeginDraw();
            isDrawing = true;


            if (currentTool == ToolType.Bucket)
            {
                FillShape(e.GetPosition(DrawingCanvas));
            }


            shapeStart = e.GetPosition(DrawingCanvas);
            if (!invertedSelect)
            {
                if (currentTool == ToolType.Select || selectionRect.Contains(e.GetPosition(DrawingCanvas))) //Punkt wewnątrz zaznaczenia
                {
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
                        DrawPoint(lastPoint.Value, brushSize);
                    }
                }
            }
            else
            {
                if (currentTool == ToolType.Select || !selectionRect.Contains(e.GetPosition(DrawingCanvas))) //Punkt  na zewnątrz zaznaczenia
                {
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
                        DrawPoint(lastPoint.Value, brushSize);
                    }
                }
            }
           
        }


        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (ActiveLayer == null) return;
            Point current = e.GetPosition(DrawingCanvas);
            if (ActiveLayer.IsVisible)
            {
                if (!invertedSelect)
                {
                    if (currentTool == ToolType.Select || selectionRect.Contains(e.GetPosition(DrawingCanvas))) //Punkt wewnątrz zaznaczenia
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
                else
                {
                    if (currentTool == ToolType.Select || !selectionRect.Contains(e.GetPosition(DrawingCanvas))) //Punkt  na zewnątrz zaznaczenia
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
                
            }
        }




        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ActiveLayer == null) return;

            isDrawing = false;
            lastPoint = null;

            if (previewShape != null)
            {
                if (!invertedSelect)
                {
                    if (currentTool == ToolType.Select || selectionRect.Contains(e.GetPosition(DrawingCanvas))) //Punkt wewnątrz zaznaczenia
                        {
                            if (currentTool != ToolType.Select)
                            {
                                Point end = e.GetPosition(DrawingCanvas);
                                DrawFinalShape(currentShape, shapeStart, end);

                                DrawingCanvas.Children.Remove(previewShape);
                                previewShape = null;
                            }
                            else
                            {
                                Point end = e.GetPosition(DrawingCanvas);
                                //Rysuj finalny select
                                DrawFinalSelectShape(previewShape, shapeStart, e.GetPosition(DrawingCanvas));
                                previewShape = null;
                                DrawingCanvas.Children.Remove(previewShape);
                            }
                        }
                }
                else
                {
                    if (currentTool == ToolType.Select || !selectionRect.Contains(e.GetPosition(DrawingCanvas))) //Punkt  na zewnątrz zaznaczenia
                    {
                        if (currentTool != ToolType.Select)
                        {
                            Point end = e.GetPosition(DrawingCanvas);
                            DrawFinalShape(currentShape, shapeStart, end);

                            DrawingCanvas.Children.Remove(previewShape);
                            previewShape = null;
                        }
                        else
                        {
                            Point end = e.GetPosition(DrawingCanvas);
                            //Rysuj finalny select
                            DrawFinalSelectShape(previewShape, shapeStart, e.GetPosition(DrawingCanvas));
                            previewShape = null;
                            DrawingCanvas.Children.Remove(previewShape);
                        }
                    }
                }
                
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
            if (ActiveLayer != null)
            {
                ActiveLayer.ImageControl.Opacity = (double)e.NewValue;
                RedrawCanvas();
            }

        }
        private void Tool_FillLayer_Click(object sender, RoutedEventArgs e)
        {
            isFillModeEnabled = true;
            Cursor = Cursors.Pen;
        }
        private void FillEntireLayerWithColor()
        {
            if (ActiveLayer == null) return;

            var wb = ActiveLayer.Bitmap;
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;

            byte[] colorData = new byte[] {
        currentColor.B,
        currentColor.G,
        currentColor.R,
        currentColor.A
    };

            byte[] fullBuffer = new byte[width * height * 4];

            for (int i = 0; i < fullBuffer.Length; i += 4)
            {
                fullBuffer[i] = colorData[0];
                fullBuffer[i + 1] = colorData[1];
                fullBuffer[i + 2] = colorData[2];
                fullBuffer[i + 3] = colorData[3];
            }

            wb.Lock();
            wb.WritePixels(new Int32Rect(0, 0, width, height), fullBuffer, width * 4, 0);
            wb.Unlock();
        }




        private void ZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scale = e.Delta > 0 ? 1.1 : 0.9;
            CanvasScale.ScaleX *= scale;
            CanvasScale.ScaleY *= scale;
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
            if (Layers == null || Layers.Count == 0)
            {
                MessageBox.Show("Brak warstw do zapisania.");
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

                int width = (int)DrawingCanvas.Width;
                int height = (int)DrawingCanvas.Height;

                var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                var drawingVisual = new DrawingVisual();

                using (DrawingContext context = drawingVisual.RenderOpen())
                {
                    foreach (var layer in Layers)
                    {
                        if (layer.ImageControl.Visibility == Visibility.Visible)
                        {
                            var image = layer.ImageControl;
                            var topLeft = new Point(Canvas.GetLeft(image), Canvas.GetTop(image));
                            if (double.IsNaN(topLeft.X)) topLeft.X = 0;
                            if (double.IsNaN(topLeft.Y)) topLeft.Y = 0;

                            context.DrawImage(image.Source, new Rect(topLeft, new Size(image.Source.Width, image.Source.Height)));
                        }
                    }
                }

                renderTarget.Render(drawingVisual);


                encoder.Frames.Clear();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

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


        //Usuwanie wszystkich warstw, tworzenie jednej nowej
        private void NewFile_Click(object sender, EventArgs e)
        {
            foreach (var layer in Layers.ToList())
            {
                DrawingCanvas.Children.Remove(layer.ImageControl);
                Layers.Remove(layer);
            }
            AddLayer_Click(null, null);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            //zapis stanu pracy w pamięci
            isSaved = true;

            
            StatusLabel.Content = "Zapisano.";
            
        }
        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Save_Click(sender, e);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Otwórz obraz",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(dlg.FileName));

                // warstwa z obrazem
                var layer = new DrawingLayer(bitmap.PixelWidth, bitmap.PixelHeight)
                {
                    Name = $"Warstwa {Layers.Count + 1}"
                };

                layer.Bitmap = new WriteableBitmap(bitmap);
                layer.ImageControl.Source = layer.Bitmap;

                // kontener dla obrazu z obramowaniem (border)
                Border imageContainer = new Border
                {
                    Child = layer.ImageControl,
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Gray
                };

                // dodanie kontener na Canvas
                Canvas.SetLeft(imageContainer, 0);
                Canvas.SetTop(imageContainer, 0);
                DrawingCanvas.Children.Insert(0, imageContainer);

                Layers.Insert(0, layer);
                layer.ImageControl.Tag = imageContainer;
                LayerList.SelectedItem = layer;

                // przesuwanie
                Point? dragStart = null;
                imageContainer.MouseLeftButtonDown += (s, ev) =>
                {
                    dragStart = ev.GetPosition(DrawingCanvas);
                    imageContainer.CaptureMouse();
                };

                imageContainer.MouseLeftButtonUp += (s, ev) =>
                {
                    dragStart = null;
                    imageContainer.ReleaseMouseCapture();
                };

                imageContainer.MouseMove += (s, ev) =>
                {
                    if (dragStart.HasValue)
                    {
                        Point current = ev.GetPosition(DrawingCanvas);
                        double offsetX = current.X - dragStart.Value.X;
                        double offsetY = current.Y - dragStart.Value.Y;

                        double left = Canvas.GetLeft(imageContainer);
                        double top = Canvas.GetTop(imageContainer);

                        Canvas.SetLeft(imageContainer, left + offsetX);
                        Canvas.SetTop(imageContainer, top + offsetY);

                        dragStart = current;
                    }
                };

                // ResizeAdorner do skalowania
                var adornerLayer = AdornerLayer.GetAdornerLayer(layer.ImageControl);
                var resizeAdorner = new ResizeAdorner(layer.ImageControl);

                // po puszceniu thumb (tego co skaluje) konczy sie edycja i usuwa adorner
                resizeAdorner.BottomRight.DragCompleted += (s, ev) =>
                {
                    adornerLayer.Remove(resizeAdorner);
                    imageContainer.BorderThickness = new Thickness(0);
                };

                // dodaje adorner i pokauje obramowanie
                adornerLayer.Add(resizeAdorner);
                imageContainer.BorderThickness = new Thickness(1);
                imageContainer.BorderBrush = Brushes.Gray;
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
        //rysik
        private void DrawingCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            if (ActiveLayer == null) return;
            currentMode = InputMode.Stylus;
            StatusLabel.Content = "Tryb: Rysik";
            shapeStart = e.GetPosition(DrawingCanvas);
            lastPoint = shapeStart;
            isDrawing = true;
            DrawStylusPoint(e);
            e.Handled = true;
        }
        private void DrawingCanvas_StylusMove(object sender, StylusEventArgs e)
        {
            if (ActiveLayer == null || !isDrawing) return;
            Point current = e.GetPosition(DrawingCanvas);
            if (currentShape != ShapeType.None && previewShape != null)
            {
                UpdatePreviewShape(previewShape, shapeStart, current);
            }
            else if (lastPoint.HasValue)
            {
                DrawLine(lastPoint.Value, current);
                lastPoint = current;
            }
            e.Handled = true;
        }
        private void DrawingCanvas_StylusUp(object sender, StylusEventArgs e)
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
            e.Handled = true;
        }
        //nacisk i kąt rysika
        private void DrawStylusPoint(StylusEventArgs e)
        {
            StylusPointCollection points = e.GetStylusPoints(DrawingCanvas);
            foreach (StylusPoint p in points)
            {
                float pressure = p.PressureFactor; // 0.0 - 1.0
                Vector tilt = GetStylusTilt(p);

                int dynamicBrushSize = Math.Max(1, (int)(brushSize * pressure));
                Color dynamicColor = currentColor;

                DrawPoint(new Point(p.X, p.Y), dynamicBrushSize);
            }
        }

        private Vector GetStylusTilt(StylusPoint point)
        {
            if (point.HasProperty(StylusPointProperties.XTiltOrientation) &&
                point.HasProperty(StylusPointProperties.YTiltOrientation))
            {
                double xtilt = point.GetPropertyValue(StylusPointProperties.XTiltOrientation);
                double ytilt = point.GetPropertyValue(StylusPointProperties.YTiltOrientation);
                return new Vector(xtilt, ytilt);
            }
            return new Vector(0, 0); // fallback
        }
        //cofanie ostatniej aktywnosci i ponawianie zmian
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveLayer?.CanUndo() == true)
            {
                ActiveLayer.Undo();

               
                if (IsLayerEmpty(ActiveLayer) && undoLayerStack.All(a => a.Layer != ActiveLayer))
                {
                    
                    int index = Layers.IndexOf(ActiveLayer);
                    var removedLayer = ActiveLayer;

                    Layers.Remove(removedLayer);
                    DrawingCanvas.Children.Remove(removedLayer.ImageControl);

                    undoLayerStack.Push(new LayerAction
                    {
                        Type = LayerAction.ActionType.Remove,
                        Layer = removedLayer,
                        Index = index
                    });

                    ActiveLayer = Layers.FirstOrDefault();
                    LayerList.SelectedItem = ActiveLayer;
                }
                return;
            }



            if (undoLayerStack.Count > 0)
            {
                var action = undoLayerStack.Pop();
                switch (action.Type)
                {
                    case LayerAction.ActionType.Add:
                        Layers.Remove(action.Layer);
                        DrawingCanvas.Children.Remove(action.Layer.ImageControl);
                        break;

                    case LayerAction.ActionType.Remove:
                        Layers.Insert(action.Index, action.Layer);
                        DrawingCanvas.Children.Insert(action.Index, action.Layer.ImageControl);
                        break;

                    case LayerAction.ActionType.Duplicate:
                        Layers.Remove(action.Layer);
                        DrawingCanvas.Children.Remove(action.Layer.ImageControl);
                        break;

                    case LayerAction.ActionType.MoveUp:
                        int indexUp = Layers.IndexOf(action.Layer);
                        if (indexUp < Layers.Count - 1)
                            Layers.Move(indexUp, indexUp + 1);
                        RedrawCanvas();
                        break;

                    case LayerAction.ActionType.MoveDown:
                        int indexDown = Layers.IndexOf(action.Layer);
                        if (indexDown > 0)
                            Layers.Move(indexDown, indexDown - 1);
                        RedrawCanvas();
                        break;
                }

                redoLayerStack.Push(action);
                LayerList.SelectedItem = ActiveLayer = action.Layer;
            }
        }


        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveLayer?.CanRedo() == true)
            {
                ActiveLayer.Redo();
                return;
            }

            if (redoLayerStack.Count > 0)
            {
                var action = redoLayerStack.Pop();
                switch (action.Type)
                {
                    case LayerAction.ActionType.Add:
                        Layers.Insert(action.Index, action.Layer);
                        DrawingCanvas.Children.Insert(action.Index, action.Layer.ImageControl);
                        break;

                    case LayerAction.ActionType.Remove:
                        Layers.Remove(action.Layer);
                        DrawingCanvas.Children.Remove(action.Layer.ImageControl);
                        break;

                    case LayerAction.ActionType.Duplicate:
                        Layers.Insert(action.Index, action.Layer);
                        DrawingCanvas.Children.Insert(action.Index, action.Layer.ImageControl);
                        break;

                    case LayerAction.ActionType.MoveUp:
                        int indexUp = Layers.IndexOf(action.Layer);
                        if (indexUp > 0)
                            Layers.Move(indexUp, indexUp - 1);
                        RedrawCanvas();
                        break;

                    case LayerAction.ActionType.MoveDown:
                        int indexDown = Layers.IndexOf(action.Layer);
                        if (indexDown < Layers.Count - 1)
                            Layers.Move(indexDown, indexDown + 1);
                        RedrawCanvas();
                        break;
                }

                undoLayerStack.Push(action);
                LayerList.SelectedItem = ActiveLayer = action.Layer;
            }

            CommandManager.InvalidateRequerySuggested();
        }


        //wartstwa
        public class LayerAction
        {
            public enum ActionType
            {
                Add, Remove, Activate, MoveUp, MoveDown, Duplicate
            }

            public ActionType Type { get; set; }

           
            public required DrawingLayer Layer { get; set; }

            public int Index { get; set; }
        }

        private bool IsLayerEmpty(DrawingLayer layer)
        {
            if (layer?.Bitmap == null) return true;

            var buffer = new byte[layer.Bitmap.PixelHeight * layer.Bitmap.BackBufferStride];
            layer.Bitmap.CopyPixels(buffer, layer.Bitmap.BackBufferStride, 0);

            return buffer.All(b => b == 0); // przezroczystość = 0
        }


    }
}
