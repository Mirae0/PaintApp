using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace PaintApp
{
    /// <summary>
    /// Logika interakcji dla klasy StartWindow.xaml
    /// </summary>
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
            // Ustawienia domyślne
            WidthTextBox.Text = "800";
            HeightTextBox.Text = "600";
            LightThemeRadio.IsChecked = true; 
        }
        public StartWindow(string theme) : this()
        {
            if (theme == "Dark")
            {
                DarkThemeRadio.IsChecked = true;
            }
            else
            {
                LightThemeRadio.IsChecked = true;
            }
        }




        private void StartButton_Click(object sender, RoutedEventArgs e)
        {

            if (!int.TryParse(WidthTextBox.Text, out int width) || width <= 0)
            {
                MessageBox.Show("Podaj poprawną szerokość.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(HeightTextBox.Text, out int height) || height <= 0)
            {
                MessageBox.Show("Podaj poprawną wysokość.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string theme = LightThemeRadio.IsChecked == true ? "Light" : "Dark";


            MainWindow mainWindow = new MainWindow(width, height, theme);
            mainWindow.Show();

            this.Close();
        }
        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            string theme = LightThemeRadio.IsChecked == true ? "Light" : "Dark";
            SetTheme(theme);
        }

        private void SetTheme(string theme)
        {
            if (theme == "Dark")
            {
                Brush darkBg = (Brush)new BrushConverter().ConvertFrom("#222222");
                Brush whiteFg = Brushes.White;

                this.Background = darkBg;
                ApplyThemeRecursively(this, darkBg, whiteFg);
            }
            else
            {
                Brush lightBg = Brushes.White;
                Brush blackFg = Brushes.Black;

                this.Background = lightBg;
                ApplyThemeRecursively(this, lightBg, blackFg);
            }
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

                ApplyThemeRecursively(child, bg, fg); 
            }
        }


    }
}