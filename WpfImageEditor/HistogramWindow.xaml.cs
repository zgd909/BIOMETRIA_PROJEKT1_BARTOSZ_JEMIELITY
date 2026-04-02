using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfImageEditor
{
    public partial class HistogramWindow : Window
    {
        private int[]? _r, _g, _b, _gray;

        private const double ML = 52;
        private const double MB = 30;
        private const double MT = 14;
        private const double MR = 12;

        private static readonly Color ClrGray  = Color.FromRgb( 90,  90,  90);
        private static readonly Color ClrRed   = Color.FromRgb(180,  40,  40);
        private static readonly Color ClrGreen = Color.FromRgb( 40, 140,  40);
        private static readonly Color ClrBlue  = Color.FromRgb( 40,  80, 190);

        public HistogramWindow(BitmapSource src)
        {
            InitializeComponent();
            (_r, _g, _b, _gray) = ImageProcessor.ComputeHistogram(src);
            Loaded      += (_, _) => DrawHistogram();
            SizeChanged += (_, _) => DrawHistogram();
        }

        private void Channel_Checked(object sender, RoutedEventArgs e)
        {
            if (_gray == null) return;
            DrawHistogram();
        }

        private void DrawHistogram()
        {
            if (HistCanvas == null || _gray == null) return;
            if (HistCanvas.ActualWidth <= 0 || HistCanvas.ActualHeight <= 0) return;

            HistCanvas.Children.Clear();

            int[] data;
            Color barColor;
            if      (RbRed.IsChecked   == true) { data = _r!;   barColor = ClrRed;   }
            else if (RbGreen.IsChecked == true) { data = _g!;   barColor = ClrGreen; }
            else if (RbBlue.IsChecked  == true) { data = _b!;   barColor = ClrBlue;  }
            else                                { data = _gray; barColor = ClrGray;  }

            double cw = HistCanvas.ActualWidth;
            double ch = HistCanvas.ActualHeight;
            double x0 = ML, x1 = cw - MR;
            double y0 = MT, y1 = ch - MB;
            double pw = x1 - x0;
            double ph = y1 - y0;

            int maxVal = 1;
            foreach (var v in data) if (v > maxVal) maxVal = v;

            for (int pct = 0; pct <= 100; pct += 25)
            {
                double y = y1 - (pct / 100.0) * ph;
                bool isBase = pct == 0;
                HistCanvas.Children.Add(new Line
                {
                    X1 = x0, Y1 = y, X2 = x1, Y2 = y,
                    Stroke = new SolidColorBrush(isBase ? Color.FromRgb(140, 140, 140) : Color.FromRgb(210, 210, 210)),
                    StrokeThickness = isBase ? 1.5 : 0.8
                });
                DrawText($"{pct}%", x0 - 6, y, 10, "#444444", TextAlignment.Right);
            }

            foreach (int val in new[] { 0, 64, 128, 192, 255 })
            {
                double x = x0 + (val / 255.0) * pw;
                bool isEdge = val == 0 || val == 255;
                HistCanvas.Children.Add(new Line
                {
                    X1 = x, Y1 = y0, X2 = x, Y2 = y1,
                    Stroke = new SolidColorBrush(isEdge ? Color.FromRgb(85, 85, 85) : Color.FromRgb(50, 50, 52)),
                    StrokeThickness = isEdge ? 1.5 : 0.8
                });
                DrawText(val.ToString(), x, y1 + 6, 10, "#444444", TextAlignment.Center);
            }

            double barW   = pw / 256.0;
            var    fill   = new SolidColorBrush(Color.FromArgb(200, barColor.R, barColor.G, barColor.B));
            var    fillHi = new SolidColorBrush(Color.FromArgb(240, barColor.R, barColor.G, barColor.B));

            for (int i = 0; i < 256; i++)
            {
                double barH = (data[i] / (double)maxVal) * ph;
                if (barH < 0.5) continue;

                var rect = new Rectangle
                {
                    Width  = Math.Max(barW, 1),
                    Height = barH,
                    Fill   = data[i] == maxVal ? fillHi : fill
                };
                Canvas.SetLeft(rect, x0 + i * barW);
                Canvas.SetTop(rect, y1 - barH);
                HistCanvas.Children.Add(rect);
            }

            HistCanvas.Children.Add(new Rectangle
            {
                Width           = pw,
                Height          = ph,
                Stroke          = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 1,
                Fill            = Brushes.Transparent
            });
            Canvas.SetLeft(HistCanvas.Children[^1], x0);
            Canvas.SetTop(HistCanvas.Children[^1], y0);

            ComputeAndShowStats(data);
        }

        private void ComputeAndShowStats(int[] data)
        {
            long total = 0, wsum = 0;
            int minVal = 255, maxVal = 0, modeVal = 0, modeCount = 0;

            for (int i = 0; i < 256; i++)
            {
                if (data[i] == 0) continue;
                total += data[i];
                wsum  += (long)i * data[i];
                if (i < minVal) minVal = i;
                if (i > maxVal) maxVal = i;
                if (data[i] > modeCount) { modeCount = data[i]; modeVal = i; }
            }

            double mean = total > 0 ? wsum / (double)total : 0;

            TxtMean.Text   = $"Średnia:  {mean:F1}";
            TxtMode.Text   = $"Dominanta:  {modeVal}";
            TxtMin.Text    = $"Min:  {(total > 0 ? minVal : 0)}";
            TxtMax.Text    = $"Max:  {(total > 0 ? maxVal : 0)}";
            TxtPixels.Text = $"Piksele:  {total:N0}";
        }

        private void DrawText(string text, double cx, double cy, double fontSize, string hex, TextAlignment align)
        {
            var tb = new TextBlock
            {
                Text          = text,
                Foreground    = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                FontSize      = fontSize,
                TextAlignment = align
            };
            tb.Measure(new Size(200, 40));
            double w = tb.DesiredSize.Width;
            double h = tb.DesiredSize.Height;
            double left = align switch
            {
                TextAlignment.Right  => cx - w,
                TextAlignment.Center => cx - w / 2,
                _                    => cx
            };
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, cy - h / 2);
            HistCanvas.Children.Add(tb);
        }
    }
}
